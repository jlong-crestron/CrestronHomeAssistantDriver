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

}