using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronWebSocketClient;

using Crestron.HomeAssistant.Events;

namespace Crestron.HomeAssistant.Transports
{

    public class ConnectEventArgs : EventArgs
    {
        public int State;
    }

    public class WebSocketLocation
    {
        public string Host;
        public uint Port;
        public string Path;
        public bool Secure;

        public string Url
        {
            get
            {
                string prefix = Secure ? "ws" : "wss";
                return string.Format("{0}://{1}:{2}{3}", prefix, Host, Port, Path);
            }
        }
    }

    class BasicWebSocket
    {
        public event EventHandler<DataEventArgs<string>> DataReceived;
        public event EventHandler<ConnectEventArgs> ConnectionStateChange;

        private WebSocketClient _socket;
        private Encoding _encoding;
        private ulong _rxBufSize;
        private WebSocketLocation _location;
        private string _originAddress =>
            CrestronEthernetHelper.GetEthernetParameter(
                CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, 0);

        private bool EnableLogging { get; set; }
        public bool Connected { get; private set; }
        private Action<string> Log { get; set; }
        public BasicWebSocket(ulong rxBufSize, Action<string> log)
        {
            _rxBufSize = rxBufSize;
            _encoding = new UTF8Encoding();
            _socket = new WebSocketClient();
            Log = log;
            EnableLogging = true;
        }

        public bool Setup(WebSocketLocation location)
        {
            if (location == null) { return false; }
            _location = location;
            _socket.KeepAlive = true;
            _socket.AddOnHeader = string.Format("origin:{0}", _originAddress);
            _socket.Host = location.Host;
            _socket.URL = location.Url;
            _socket.Port = location.Port;
            _socket.KeepAlive = true;
            _socket.ConnectionCallBack = ConnectCallback;
            _socket.ReceiveCallBack = ReceiveCallback;
            _socket.SendCallBack = SendCallback;
            _socket.DisconnectCallBack = DisconnectCallback;

            if (_socket.Connected)
            {
                Connected = true;
                return true;
            }

            return false;
        }

        public void SendMethod(string message)
        {
            try
            {
                var buf = Encoding.ASCII.GetBytes(message);

                if (_socket == null)
                {
                    _socket = new WebSocketClient();
                    Setup(_location);
                    _socket.ConnectAsyncEx();
                    if (EnableLogging)
                    {
                        Log(string.Format("WebSocketTransport : Websocket starting"));
                    }
                }

                if (_socket.Connected)
                {
                    _socket.SendAsync(buf, (uint)buf.Length,
                    WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__TEXT_FRAME);
                }
                else
                {
                    ConnectClient();
                    if (EnableLogging)
                    {
                        Log("WebSocketTransport - Unable to send data, client is not connected");
                    }
                }
            }
            catch (Exception e)
            {
                if (EnableLogging)
                {
                    Log(string.Format("WebSocketTransport : Send Method Exception: {0}", e.Message));
                }
            }
        }

        private int ConnectCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            UpdateConnectionStatus(error, "ConnectCallback");
            if (!Connected)
            {
                if (EnableLogging)
                {
                    Log(string.Format("WebSocketTransport : Unable to connect - {0}", error));
                }

                // Handle reconnect
                if (error != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_ALREADY_CONNECTED ||
                    error != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_PENDING ||
                     error != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
                {
                    ConnectClient();
                }
            }

            return (int)error;
        }


        private int DisconnectCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error, object obj)
        {
            if (obj is bool &&
                ((bool)obj))
            {
                // This means we called disconnect internally,
                // report this as being disconnected to everyone else
                UpdateConnectionStatus(WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_ERROR, "DisconnectCallback");
            }
            else
            {
                UpdateConnectionStatus(error, "DisconnectCallback");
            }
            try
            {
                if (EnableLogging)
                {
                    Log(string.Format("WebSocketTransport : Disconnected - {0}", error));
                }
                ConnectClient();
            }
            catch (Exception e)
            {
                if (EnableLogging)
                {
                    Log(string.Format("WebSocketTransport : Disconnect Callback Exception: {0}", e.Message));
                }
            }
            return (int)error;
        }

        private int SendCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            UpdateConnectionStatus(error, "SendCallback");
            try
            {
                if (error == WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
                {
                    _socket.ReceiveAsync();
                }

                // Handle reconnect
                if (error != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_ALREADY_CONNECTED ||
                    error != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_PENDING ||
                    error != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
                {
                    ConnectClient();
                }
            }
            catch (Exception e)
            {
                Log(string.Format("WebSocketTransport : Send Callback Exception: {0}", e.Message));
            }
            return (int)error;
        }

        public int ReceiveCallback(
            byte[] data, 
            uint datalen, 
            WebSocketClient.WEBSOCKET_PACKET_TYPES opcode,
            WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            try
            {
                if (opcode == WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__CLOSE)
                {
                    // Pass "true" as callback to ensure the callback
                    // will report disconnected instead of success
                    _socket.DisconnectAsync(true);
                    return (int)error;
                }

                string dataReceived = Encoding.Default.GetString(data, 0, (int)datalen);
                DataEventArgs<string> args = new DataEventArgs<string>(dataReceived);
                DataReceived.Invoke(this, args);

                Log(string.Format("RX: {0}", dataReceived));
                

                // Handle reconnect
                if (error != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_ALREADY_CONNECTED ||
                    error != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_PENDING ||
                    error != WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
                {
                    ConnectClient();
                }

            }
            catch (Exception e)
            {
                if (EnableLogging)
                {
                    Log(string.Format("WebSocketTransport : Receive Callback Exception: {0}", e.Message));
                }
            }
            finally
            {
                _socket.ReceiveAsync();
            }
            return (int)error;
        }

        public void ConnectClient()
        {
            try
            {
                _socket.ConnectAsyncEx();
            }
            catch (Exception e)
            {
                if (EnableLogging)
                {
                    Log(string.Format("WebSocketTransport - Exception occured while connecting client: {0}", e.Message));
                }
            }
        }


        private void UpdateConnectionStatus(WebSocketClient.WEBSOCKET_RESULT_CODES code, string callbackOrigin)
        {
            if (EnableLogging)
            {
                Log(string.Format("WebSocketTransport.UpdateConnectionStatus: {0} from {1}",
                    code, callbackOrigin));
            }

            switch (code)
            {
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_ALREADY_CONNECTED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS:
                    Connected = true;
                    ConnectionStateChange(this, new ConnectEventArgs() { State = (int)code });
                    break;
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_PENDING:
                    Connected = true;
                    break;
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_ASYNC_READ_CB_ALREADY_SET:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_ASYNC_WRITE_BUSY_SENDING:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_ERROR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_HOSTNAME_LOOKUP_BY_IPADDR_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_HTTP_HANDSHAKE_RESPONSE_ERROR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_HTTP_HANDSHAKE_SECURITY_KEY_ERROR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_HTTP_HANDSHAKE_TOKEN_ERROR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_INSUFFICIENT_BUFFER:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_INVALID_HANDLE:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_INVALID_HOSTNAME:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_INVALID_HOSTNAME_AND_IPADDR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_INVALID_IPADDR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_INVALID_PATH:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_INVALID_POINTER:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_INVALID_PROXY_IPADDR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_INVALID_URL:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_IPADDR_FAMILY_NOT_SUPPORTED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_IPADDR_LOOKUP_BY_HOSTNAME_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_LINKLIST_INSERT_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_LINKLIST_OBTAIN_HANDLE_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_MEMORY_ALLOC_ERROR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_PROXY_HOSTNAME_LOOKUP_BY_IPADDR_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_PROXY_IPADDR_LOOKUP_BY_HOSTNAME_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_READ_BUFFER_SIZE_INVALID:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SERVER_CERTIFICATE_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SOCKET_CONNECTION_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SOCKET_CREATION_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SOCKET_RECEIVE_ERROR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SOCKET_SELECT_ERROR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SSL_CONNECTION_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SSL_CONTEXT_ALLOC_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SSL_LIBRARY_INIT_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SSL_OPTION_SETTING_FAILED:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SSL_RECEIVE_ERROR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SSL_SOCKET_SEND_ERROR:
                case WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_TCP_SOCKET_SEND_ERROR:
                    Connected = false;
                    break;
            }
        }
    }
}
