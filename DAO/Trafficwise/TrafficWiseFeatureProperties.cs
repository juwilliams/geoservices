using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace gbc.DAO.Trafficwise
{
    public class TrafficWiseFeatureProperties
    {
        public string AssetType { get; set; }
        public string Icon { get; set; }
        public string Message { get; set; }
        public string Mile { get; set; }
        public string Road { get; set; }

        internal static TrafficWiseFeatureProperties Create(dynamic jToken)
        {
            return new TrafficWiseFeatureProperties()
            {
                AssetType = (string)jToken.assettype,
                Icon = (string)jToken.tcon,
                Message = (string)jToken.message,
                Mile = (string)jToken.mile,
                Road = (string)jToken.road
            };
        }
    }
}
