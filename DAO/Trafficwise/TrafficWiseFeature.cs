using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.DAO.Trafficwise
{
    public class TrafficWiseFeature
    {
        public TrafficWiseGeometry Geometry { get; set; }
        public TrafficWiseFeatureProperties Properties { get; set; }
        public string FeatureType { get; set; }

        internal static TrafficWiseFeature Create(dynamic jObject)
        {
            return new TrafficWiseFeature()
            {
                Geometry = TrafficWiseGeometry.Create(jObject.SelectToken("geometry")),
                Properties = TrafficWiseFeatureProperties.Create(jObject.SelectToken("properties")),
                FeatureType = (string)jObject.type
            };
        }
    }
}
