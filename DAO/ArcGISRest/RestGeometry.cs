using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gbc.DAO.ArcGISRest
{
    public sealed class RestGeometry
    {
        public string X { get; set; }
        public string Y { get; set; }

        public static RestGeometry Create(dynamic jToken)
        {
            RestGeometry geometry = new RestGeometry();
            geometry.X = (string)jToken.x;
            geometry.Y = (string)jToken.y;

            return geometry;
        }
    }
}
