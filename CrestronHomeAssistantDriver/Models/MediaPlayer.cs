using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crestron.HomeAssistant.Models
{
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

    }
}
