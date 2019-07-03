using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects
{
    public class RabbitMQMessageObj
    {
        [JsonProperty("request_url")]
        public String requestUrl { get; set; }
        [JsonProperty("response_url")]
        public String responseUrl { get; set; }
        [JsonProperty("event_id")]
        public String eventId { get; set; }
        [JsonProperty("guid")]
        public String guid { get; set; }
        [JsonProperty("event_type")]
        public String eventType { get; set; }
        [JsonProperty("event_dtm")]
        public String eventDateTime { get; set; }
        [JsonProperty("verb")]
        public String verb { get; set; }
        [JsonProperty("payload")]
        public JRaw payload { get; set; }
    }
}
