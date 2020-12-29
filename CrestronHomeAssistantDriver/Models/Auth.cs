using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crestron.HomeAssistant.Models 
{
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
}