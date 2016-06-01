using System;
using System.Collections.Generic;
using gbc.DAO.Geocode;
using gbc.Interfaces;
using Newtonsoft.Json;

namespace gbc.DAO
{
    public class GeoRecord : IGeoRecord
    {
        public string id { get; set; }
        public int dataid { get; set; }
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
