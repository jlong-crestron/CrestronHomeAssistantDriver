using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Crestron.HomeAssistant.Models 
{
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
}