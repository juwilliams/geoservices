using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gbc.DAO.ArcGISRest
{
    public class RestFeature
    {
        public RestGeometry Geometry { get; set; }
        public List<RestAttribute> Attributes { get; set; }

        public RestFeature ()
        {
            this.Attributes = new List<RestAttribute>();
        }

        internal static RestFeature Create(dynamic jObject)
        {
            return new RestFeature()
            {
                Geometry = RestGeometry.Create(jObject.SelectToken("geometry")),
                Attributes = RestAttribute.CreateList(jObject.SelectToken("attributes"))
            };
        }
    }
}
