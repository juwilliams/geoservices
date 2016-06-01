using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.DAO.Trafficwise
{
    [JsonObject]
    public class TrafficWiseMessage
    {
        public List<TrafficWiseFeature> Features { get; set; }
        public string Timestamp { get; set; }
        public string MessageType { get; set; }
        public string Version { get; set; }

        public TrafficWiseMessage()
        {
            this.Features = new List<TrafficWiseFeature>();
        }

        public static TrafficWiseMessage Parse(JObject jObject)
        {
            TrafficWiseMessage message = new TrafficWiseMessage();
            
            JArray features = JArray.Parse(jObject.SelectToken("features").ToString());

            for (int i = 0, featureLen = features.Count; i < featureLen; i++)
            {
                TrafficWiseFeature feature = TrafficWiseFeature.Create(features[i]);
                
                message.Features.Add(feature);
            }

            return message;
        }
    }
}
