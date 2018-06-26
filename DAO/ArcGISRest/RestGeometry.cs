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
        public string Ring { get; set; }

        public static RestGeometry Create(dynamic jToken)
        {
            RestGeometry geometry = new RestGeometry();

            if (jToken.rings != null)
            {
                geometry.Ring = jToken.rings.Count > 0 ? string.Join("|", jToken.rings[0]) : "";
                geometry.Ring = geometry.Ring.Replace("[", "");
                geometry.Ring = geometry.Ring.Replace("]", "");
            }
            else
            {
                geometry.X = (string)jToken.x;
                geometry.Y = (string)jToken.y;
            }

            return geometry;
        }
    }
}
