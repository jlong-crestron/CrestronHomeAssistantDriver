using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Crestron.SimplSharp.CrestronWebSocketClient;

using Crestron.HomeAssistant.Events;
using Crestron.HomeAssistant.Transports;
using Crestron.HomeAssistant.Models;

namespace Crestron.HomeAssistant.Controllers
{
    // TODO: Create base Controller object for higher ordeance classes to derive basic behaviors from.
    public class HomeAssistantWebSocketController
    {
        private BasicWebSocket _socket;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly Action<string> _sendJson;
        private readonly Action<string> _log;
        private ulong _commandId;
        private readonly WebSocketLocation _loc;

        // TODO: Prevent unhandled responses from building up, and use that to retry
        // Note: In testing, it seems you are not allowed to send a second
        // command before the first is responded to. So we may not need a full
        // dictionary here, just compare the previous command ID to what we get
        // back.
        private readonly Dictionary<ulong, Action<CommandResponseObject>> _responseActions;
        // buffer size for response payloads for the websocket transport.
        // Probably shouldnt be residing here .... 
        private const ulong _rxBufSize = 16384;

        // When we get a state change event or poll. State may actually be the same
        // but this is always fired regardless
        public event EventHandler<DataEventArgs<StateObject>> EntityStateChanged;

        public string AccessToken { protected get; set; }

        public HomeAssistantWebSocketController(WebSocketLocation location, Action<string> log, string accessToken)
        {
            _log = log;
            _loc = location;
            AccessToken = accessToken;

            _socket = new BasicWebSocket(_rxBufSize, _log);
            _sendJson = _socket.SendMethod;
            _responseActions = new Dictionary<ulong, Action<CommandResponseObject>>();
            _jsonSettings = new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            _socket.ConnectionStateChange += OnConnected;
            _socket.DataReceived += OnDataReceived;
            _socket.Setup(_loc);
            _socket.ConnectClient();
        }

        public void Dispose()
        {
            _socket.ConnectionStateChange -= OnConnected;
            _socket.DataReceived -= OnDataReceived;
        }

        private void SendCommandWithoutId(BaseCommandObject cmd)
        {
            var data = JsonConvert.SerializeObject(cmd, Formatting.None, _jsonSettings);
            Log("TX: {0}", data);
            _sendJson(data);
        }

        public void SendCommand(CommandObject cmd, Action<CommandResponseObject> responseCallback)
        {
            // Assign unique ID
            cmd.Id = ++_commandId;

            // Add ID to callback dict
            if (responseCallback != null)
            {
                _responseActions[cmd.Id] = responseCallback;
            
            }

            SendCommandWithoutId(cmd);
        }

        private void Log(string s, params object[] args)
        {
            _log?.Invoke(string.Format(s, args));
        }

        public void OnDataReceived(object sender, DataEventArgs<string> args)
        {
            Log("RX: {0}", args.Data);

            try
            {
                ProcessResponse(args.Data);
            }
            catch (Exception ex)
            {
                Log("Error occurred processing response: {0}", ex.ToString());
            }
        }

        public void OnConnected(object sender, ConnectEventArgs args)
        {
            Log(String.Format("Connection state is now: {0}", args.State));
            if (args.State == (int)WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
            {
                Log("Connected To Socket Transport");
                Log(String.Format("Connection established Starting HA Auth."));
                StartAuth();
            }
        }

        private void ProcessResponse(string response)
        {
            // Determine what kind of response it is
            var commandObject = JsonConvert.DeserializeObject<BaseCommandObject>(response);
            switch (commandObject.CommandType)
            {
                // Sent when connection is first opened
                case AuthenticationCommandObject.TypeAuthRequired:
                    Log("Received auth required - starting auth");
                    StartAuth();
                    break;

                case AuthenticationCommandObject.TypeAuthOk:
                    OnAuthenticated(true, null);
                    break;

                case AuthenticationCommandObject.TypeAuthInvalid:
                {
                    // Get reason why it failed
                    var obj = JsonConvert.DeserializeObject<AuthenticationCommandObject>(response, _jsonSettings);
                    OnAuthenticated(false, obj.Message);
                    break;
                }

                case GetCommandObject.TypePingResponse:
                {
                    // Ping is special and just has ID, so use CommandObject
                    var obj = JsonConvert.DeserializeObject<CommandObject>(response, _jsonSettings);
                    OnPingResponse(obj.Id);
                    break;
                }

                case CommandResponseObject.TypeResult:
                {
                    // Generic "result" sent back for many commands
                    var obj = JsonConvert.DeserializeObject<CommandResponseObject>(response, _jsonSettings);
                    if (obj.Success)
                    {
                        // Call callback, if it exists
                        Action<CommandResponseObject> callback;
                        if (_responseActions.TryGetValue(obj.Id, out callback))
                        {
                            
                            _responseActions.Remove(obj.Id);
                            
                            callback(obj);
                        }
                    }
                    else
                    {
                        // Log error here, don't call callback since this is a simple
                        // proof-of-concept. A real solution probably notifies the
                        // calling code that their command failed.
                        var code = obj.Error == null ? "<null error>" : obj.Error.Code.ToString();
                        var errmsg = obj.Error == null || obj.Error.Message == null ? "<no error message>" : obj.Error.Message;
                        Log("Result id:{0} returned error {1}: {2}", obj.Id, code, errmsg);
                    }
                    

                    break;
                }

                case EventObject.TypeEvent:
                {
                    // Process events
                    var obj = JsonConvert.DeserializeObject<EventObject>(response, _jsonSettings);
                    OnHAEvent(obj);
                    break;
                }

                default:
                    Log("Unknown response CommandType: {0}\r\n{1}", commandObject.CommandType, response);
                    break;
            }
        }

        #region Authentication

        public void StartAuth()
        {
            var cmd = new AuthenticationCommandObject(AccessToken);
            SendCommandWithoutId(cmd);
        }

        private void OnAuthenticated(bool success, string message)
        {
            if (success)
            {
                Log("Authentication successful");
                SubscribeToChanges();
            }
            else
            {
                Log("Authentication failed: {0}", message);
                StartAuth();
            }
        }

        #endregion

        #region Commands and Responses

        public void Ping(Action callback)
        {
            SendCommand(new GetCommandObject()
            {
                CommandType = GetCommandObject.TypePing
            }, null);
        }

        private void OnPingResponse(ulong id) //CommandResponseObject resp)
        {

        }

        public void PollStates()
        {
            var cmd = new CommandObject()
            {
                CommandType = GetCommandObject.TypeGetStates
            };
            SendCommand(cmd, GetStatesResponse);
        }

        private void SubscribeToChanges()
        {
            SendCommand(new ServiceSubscribeObject(), OnSubscribed);
        }

        private void OnSubscribed(CommandResponseObject rep)
        {
            PollStates();
        }

        public void GetStatesResponse(CommandResponseObject resp)
        {
            var result = resp.Result as JArray;
            if (result == null)
            {
                Log("GetStatesResponse: result was null");
                return;
            }

            foreach (var obj in result)
            {
                var state = obj.ToObject<StateObject>();
                EntityStateChanged?.Invoke(this, new DataEventArgs<StateObject>(state));
            }
        }

        private void OnHAEvent(EventObject ev)
        {
            StateObject state = null;

            try
            {
                state = ev.Event.Data.NewState;
            }
            catch (NullReferenceException)
            {
                // Will handle below
            }

            if (ev == null)
            {
                Log("OnHAEvent: some data was null");
            }
            else
            {
                EntityStateChanged?.Invoke(this, new DataEventArgs<StateObject>(state));
            }
        }

        #endregion
    }
}
