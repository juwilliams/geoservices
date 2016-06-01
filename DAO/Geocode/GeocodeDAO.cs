using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.DAO.Geocode
{
    public class GeocodeDAO
    {
        public string Address { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }

        private GeocodeDAO()
        {

        }

        public static GeocodeDAO Create(GeoRecord record)
        {
            GeocodeDAO geocodeDao = new GeocodeDAO();

            foreach (GeoField field in record.fields)
            {
                switch (field.Name.ToLower())
                {
                    case GeocodeConstants.Fields.Address:
                        {
                            geocodeDao.Address = field.Value;

                            break;
                        }
                    case GeocodeConstants.Fields.State:
                        {
                            geocodeDao.State = field.Value;

                            break;
                        }
                    case GeocodeConstants.Fields.Zip:
                        {
                            geocodeDao.Zip = field.Value;

                            break;
                        }
                }
            }

            return geocodeDao;
        }
    }

    
}
