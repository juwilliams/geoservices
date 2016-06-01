using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.DAO.Trafficwise
{
    public class TrafficWiseGeometry
    {
        public string[] Coordinates { get; set; }
        public string GeometryType { get; set; }

        internal static TrafficWiseGeometry Create(dynamic jToken)
        {
            return new TrafficWiseGeometry()
            {
                Coordinates = ((JArray)jToken.coordinates).Select(p => (string)p).ToArray(),
                GeometryType = (string)jToken.type
            };
        }
    }
}
