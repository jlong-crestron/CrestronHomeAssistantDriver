using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crestron.HomeAssistant.Models
{
    public class BaseCommandObject
    {
        [JsonProperty(PropertyName = "type")]
        public string CommandType { get; set; }
    }

    // This handles both commands and responses
    public class AuthenticationCommandObject : BaseCommandObject
    {
        public const string TypeAuthRequired = "auth_required";
        public const string TypeAuthSendToken = "auth";
        public const string TypeAuthOk = "auth_ok";
        public const string TypeAuthInvalid = "auth_invalid";

        public AuthenticationCommandObject()
        {
        }

        public AuthenticationCommandObject(string accessToken)
        {
            CommandType = TypeAuthSendToken;
            AccessToken = accessToken;
        }

        // Set by us when we use AuthSendToken
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        // Set by HA when authentication fails
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }

    public class CommandObject : BaseCommandObject
    {
        // ID should be always increasing with each command sent.
        // Not sure if there's a rollover point or we just have to restart the connection
        [JsonProperty(PropertyName = "id")]
        public ulong Id { get; set; }
    }

    public class GetCommandObject : CommandObject
    {
        public const string TypePing = "ping";
        public const string TypePingResponse = "pong";
        public const string TypeGetStates = "get_states";
        public const string TypeGetConfig = "get_config";
        public const string TypeGetServices = "get_services";
        public const string TypeGetPanels = "get_panels";
    }

    public class CommandResponseObject : CommandObject
    {
        public static string TypeCommandResponse = "result";

        public class ErrorObject
        {
            // This is an int in the docs, but in practice, apparently it's a string
#if false
            public enum Code
            {
                Unknown = 0, // Not valid from HA
                NonIncreasingId = 1,
                IncorrectFormat = 2,
                ItemNotFound = 3,
                NumberOfCodes,
            }

            public Code ErrorCode
            {
                get
                {
                    if (Enum.IsDefined(typeof(Code), code))
                    {
                        return (Code) code;
                    }

                    return Code.Unknown;
                }
            }
#endif

            // int in docs, string in reality
            [JsonProperty(PropertyName = "code")]
            public string Code { get; set; }

            [JsonProperty(PropertyName = "message")]
            public string Message { get; set; }
        }

        public const string TypeResult = "result";

        [JsonProperty(PropertyName = "success")]
        public bool Success { get; set; }

        // What this is depends on the CommandType of command. Many commands it is null
        // get_state, get_panels, etc use this
        // So far I've seen JArray, JObject, or null
        [JsonProperty(PropertyName = "result")]
        public object Result { get; set; }

        // Not specified if there is no error
        [JsonProperty(PropertyName = "error")]
        public ErrorObject Error { get; set; }
    }

    public class ServiceSubscribeObject : CommandObject
    {
        public const string TypeSubscribeEvents = "subscribe_events";

        // There are probably more events, but we just use this one
        public const string EventTypeStateChanged = "state_changed";

        // Set to null to get all events
        [JsonProperty(PropertyName = "event_type")]
        public string EventType { get; set; }

        public ServiceSubscribeObject()
        {
            CommandType = TypeSubscribeEvents;
            EventType = EventTypeStateChanged;
        }
    }

    public class ServiceUnsubscribeObject : CommandObject
    {
        public const string TypeUnsubscribeEvents = "unsubscribe_events";

        // Must match the id of the command you send to subscribe
        [JsonProperty(PropertyName = "subscription")]
        public ulong Subscription { get; set; }

        public ServiceUnsubscribeObject()
        {
            CommandType = TypeUnsubscribeEvents;
        }
    }

    public class EventObject : CommandObject
    {
        public const string TypeEvent = "event";

        public class InnerObject
        {
            public class EventData
            {
                [JsonProperty(PropertyName = "entity_id")]
                public string EntityId { get; set; }

                [JsonProperty(PropertyName = "event_type")]
                public string EventType { get; set; }

                [JsonProperty(PropertyName = "new_state")]
                public StateObject NewState { get; set; }

                [JsonProperty(PropertyName = "old_state")]
                public StateObject OldState { get; set; }

                // Time format: 2016-11-26T01:37:10.466994+00:00
                [JsonProperty(PropertyName = "time_fired")]
                public DateTime TimeFired { get; set; }

                [JsonProperty(PropertyName = "origin")]
                public string Origin { get; set; }
            }

            [JsonProperty(PropertyName = "data")]
            public EventData Data { get; set; }
        }

        [JsonProperty(PropertyName = "event")]
        public InnerObject Event { get; set; }
    }

    public class ServiceCommandObject : CommandObject
    {
        public const string TypeCallService = "call_service";

        [JsonProperty(PropertyName = "domain")]
        public string Domain { get; set; }

        [JsonProperty(PropertyName = "service")]
        public string Service { get; set; }

        [JsonProperty(PropertyName = "service_data")]
        public ServiceData Data { get; set; }

        public ServiceCommandObject()
        {
            CommandType = TypeCallService;
        }
    }

    public class ServiceData
    {
        // Set to null to control all entities
        [JsonProperty(PropertyName = "entity_id")]
        public string EntityId { get; set; }
    }

    // I took this from the media_player spec. I have not tested this to see
    // if it works on roku
    public class ThumbnailCommandObject : CommandObject
    {
        public const string TypeThumbnail = "media_player_thumbnail";

        [JsonProperty(PropertyName = "entity_id")]
        public string EntityId { get; set; }

        public ThumbnailCommandObject()
        {
            CommandType = TypeThumbnail;
        }
    }

    public class ThumbnailCommandResult
    {
        // mime CommandType of image, like "image/jpeg"
        [JsonProperty(PropertyName = "content_type")]
        public string ContentType { get; set; }

        // base64 encoded image
        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }
    }

    // https://www.home-assistant.io/integrations/media_player/
    public static class MediaPlayerServiceData
    {
        public const string DomainName = "media_player";

        // https://www.home-assistant.io/integrations/media_player/#service-media_playervolume_mute
        public class VolumeMute : ServiceData
        {
            public const string ServiceName = "volume_mute";
            [JsonProperty(PropertyName = "is_volume_muted")]
            public bool IsVolumeMuted { get; set; }
        }

        // https://www.home-assistant.io/integrations/media_player/#service-media_playervolume_set
        public class VolumeSet : ServiceData
        {
            public const string ServiceName = "volume_set";
            // 0 - 1 range
            [JsonProperty(PropertyName = "volume_level")]
            public double VolumeLevel { get; set; }
        }

        // https://www.home-assistant.io/integrations/media_player/#service-media_playerselect_source
        public class SelectSource : ServiceData
        {
            public const string ServiceName = "select_source";
            [JsonProperty(PropertyName = "source")]
            public string Source { get; set; }
        }

        // https://www.home-assistant.io/integrations/media_player/#service-media_playerselect_sound_mode
        // Only supported on Denon and Songpal per docs. Denon uses a string (from testing)
        public class SelectSoundMode : ServiceData
        {
            public const string ServiceName = "select_sound_mode";

            [JsonProperty(PropertyName = "sound_mode")]
            public string SoundMode { get; set; }
        }

        // https://www.home-assistant.io/integrations/media_player/#service-media_playermedia_seek
        // media_player.media_seek has seek_position but format is platform dependent
        // https://www.home-assistant.io/integrations/media_player/#service-media_playerplay_media
        // https://www.home-assistant.io/integrations/media_player/#service-media_playershuffle_set
        // https://www.home-assistant.io/integrations/media_player/#service-media_playerrepeat_set
    }

    // https://www.home-assistant.io/integrations/remote
    // Services not implemented below: turn_on, turn_off, toggle
    public static class RemoteServiceData
    {
        public static class Commands
        {
            public const string Back = "back";
            public const string Backspace = "backspace";
            public const string ChannelDown = "channel_down";
            public const string ChannelUp = "channel_up";
            public const string Down = "down";
            public const string Enter = "enter";
            public const string FindRemote = "find_remote";
            public const string Forward = "forward";
            public const string Home = "home";
            public const string Info = "info";
            public const string InputAv1 = "input_av1";
            public const string InputHdmi1 = "input_hdmi1";
            public const string InputHdmi2 = "input_hdmi2";
            public const string InputHdmi3 = "input_hdmi3";
            public const string InputHdmi4 = "input_hdmi4";
            public const string InputTuner = "input_tuner";
            public const string Left = "left";
            public const string Literal = "literal";
            public const string Play = "play";
            public const string Power = "power";
            public const string Replay = "replay";
            public const string Reverse = "reverse";
            public const string Right = "right";
            public const string Search = "search";
            public const string Select = "select";
            public const string Up = "up";
            public const string VolumeDown = "volume_down";
            public const string VolumeMute = "volume_mute";
            public const string VolumeUp = "volume_up";
        }

        public const string DomainName = "remote";

        public class SendCommand : ServiceData
        {
            public const string ServiceName = "send_command";

            // This may be a list or array of commands
            [JsonProperty(PropertyName = "command")]
            public string Command { get; set; }
        }
    }

    public class StateObject
    {
        [JsonProperty(PropertyName = "entity_id")]
        public string EntityId { get; set; }

        [JsonProperty(PropertyName = "last_changed")]
        public DateTime LastChanged { get; set; }

        [JsonProperty(PropertyName = "last_updated")]
        public DateTime LastUpdated { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "attributes")]
        public AttributesObject Attributes { get; set; }
    }

    public class AttributesObject
    {
        // Most things seem to have this
        [JsonProperty(PropertyName = "friendly_name")]
        public string FriendlyName { get; set; }

        // Media players
        [JsonProperty(PropertyName = "volume_level")]
        public double? VolumeLevel { get; set; }

        [JsonProperty(PropertyName = "is_volume_muted")]
        public bool? IsVolumeMuted { get; set; }

        // Media source
        [JsonProperty(PropertyName = "source")]
        public string Source { get; set; }

        [JsonProperty(PropertyName = "source_list")]
        public List<string> SourceList { get; set; }

        [JsonProperty(PropertyName = "sound_mode")]
        public string SoundMode { get; set; }

        [JsonProperty(PropertyName = "sound_mode_list")]
        public List<string> SoundModeList { get; set; }

        // is "app" for roku, was "channel" at some point for an AVR
        [JsonProperty(PropertyName = "media_content_type")]
        public string MediaContentType { get; set; }

        // Common (lights have this too) but means different things for different types
        // See SUPPORTS_ in stuff like
        // https://github.com/home-assistant/core/blob/dev/homeassistant/components/media_player/const.py
        [JsonProperty(PropertyName = "supports_features")]
        public ulong? SupportsFeatures { get; set; }

        // Image for active media source on roku
        [JsonProperty(PropertyName = "entity_picture")]
        public string EntityPictureUrl { get; set; }

        // Stuff that may be very specific that we likely don't care about
#if false
        // Might be denon-specific
        [JsonProperty(PropertyName = "sound_mode_raw")]
        public string SoundModeRaw { get; set; }

        // From roku, maybe others? Same as source though.
        [JsonProperty(PropertyName = "app_name")]
        public string AppName { get; set; }

        // From roku, gives app id int code
        [JsonProperty(PropertyName = "app_id")]
        public int AppId{ get; set; }

        // "receiver" on 4k roku box (media_player)
        [JsonProperty(PropertyName = "device_class")]
        public string DeviceClass { get; set; }
#endif

    }
}
