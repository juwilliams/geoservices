using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gbc.DAO.ArcGISRest
{
    public sealed class RestAttribute
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public static RestAttribute Create(JProperty property)
        {
            RestAttribute attribute = new RestAttribute();
            attribute.Key = property.Name;
            attribute.Value = (string)property.Value;

            return attribute;
        }

        public static List<RestAttribute> CreateList(JObject jObject)
        {
            List<RestAttribute> attributes = new List<RestAttribute>();

            foreach (JProperty property in (JToken)jObject)
            {
                attributes.Add(RestAttribute.Create(property));
            }

            return attributes;
        }
    }
}
