using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gbc.DAO.ArcGISRest
{
    public sealed class RestMessage
    {
        public List<RestFeature> Features { get; set; }

        public RestMessage()
        {
            this.Features = new List<RestFeature>();
        }

        public static RestMessage Parse(JObject jObject)
        {
            RestMessage message = new RestMessage();

            JArray features = JArray.Parse(jObject.SelectToken("features").ToString());

            for (int i = 0, featureLen = features.Count; i < featureLen; i++)
            {
                RestFeature feature = RestFeature.Create(features[i]);

                message.Features.Add(feature);
            }

            return message;
        }
    }
}
