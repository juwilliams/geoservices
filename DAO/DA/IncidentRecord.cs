using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gbc.DAO.Geocode;

namespace gbc.DAO.DA
{
    public class IncidentRecord
    {
        public string IncidentId { get; set; }
        public int RecordId { get; set; }
        public string RecordStatus { get; set; }
        public string County { get; set; }
        public string RecordType { get; set; }
        public string Insured { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string AddedOn { get; set; }

        //  Geocode Location
        public string Latitude { get; set; }
        public string Longitude { get; set; }

        public IncidentRecord()
        {
            
        }

        public static IncidentRecord Parse(string record)
        {
            if (string.IsNullOrEmpty(record))
            {
                return null;
            }

            int firstSpace = record.IndexOf(" ");
            string incidentId = record.Substring(0, firstSpace);

            string payload = record.Substring(firstSpace, record.Length - firstSpace);
            string rawAttributes = payload.Replace("(", "");
            rawAttributes = rawAttributes.Replace(")", "");

            string[] attributes = rawAttributes.Split(new string[] { " - " }, StringSplitOptions.None);

            return new IncidentRecord()
            {
                IncidentId = incidentId,
                RecordId = int.Parse(attributes[0]),
                RecordStatus = attributes[1],
                County = attributes[2],
                RecordType = attributes[3],
                Insured = attributes[4],
                Address = attributes[5],
                City = attributes[7],
                State = attributes[8],
                Zip = attributes[9],
                Latitude = string.Empty,
                Longitude = string.Empty,
                AddedOn = attributes[10]
            };
        }
    }
}
