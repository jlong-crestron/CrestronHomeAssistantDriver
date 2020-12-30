using Newtonsoft.Json;

namespace Crestron.HomeAssistant.Models 
{
    public class Configuration {
        [JsonProperty(PropertyName = "accessToken")]
        public string DefaultAccessToken { get; set; }

        [JsonProperty(PropertyName = "host")]
        public string Host { get; set; }

        [JsonProperty(PropertyName = "path")]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "port")]
        public uint Port { get; set; }

        [JsonProperty(PropertyName = "debug")]
        public bool Debug { get; set; }

        [JsonProperty(PropertyName = "secure")]
        public bool Secure { get; set; }
    }
}