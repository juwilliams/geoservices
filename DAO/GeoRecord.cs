using System;
using System.Collections.Generic;
using gbc.DAO.Geocode;
using gbc.Interfaces;
using Newtonsoft.Json;
using System.Linq;

namespace gbc.DAO
{
    public class GeoRecord : IGeoRecord
    {
        public string id { get; set; }
        public int dataid { get; set; }
        public int objectid { get; set; }
        public List<GeoField> fields { get; set; }
        public string geometry { get; set; }
        public string uid { get; set; }

        [JsonIgnore]
        public bool was_update { get; set; }

        public GeoRecord(string geometryType)
        {
            this.geometry = geometryType;
            this.fields = new List<GeoField>();
        }

        public int GetKeyFieldValue(string containerKey)
        {
            int key = -1;

            if (fields == null || !fields.Any(p => p.Name.ToLower() == containerKey.ToLower()))
            {
                return key;
            }

            int.TryParse(fields.First(p => p.Name.ToLower() == containerKey.ToLower()).Value, out key);

            return key;
        }

        public static void SetGeocodeFields(GeoRecord record, GeocodeDAO geocodeDao)
        {
            foreach (GeoField field in record.fields)
            {
                switch (field.Name.ToLower())
                {
                    case GeocodeConstants.Fields.Latitude:
                        {
                            field.Value = geocodeDao.Latitude;

                            break;
                        }
                    case GeocodeConstants.Fields.Longitude:
                        {
                            field.Value = geocodeDao.Longitude;

                            break;
                        }
                }
            }
        }
    }
}
