using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gbc.DAO.Geocode;
using gbc.DAO.DA;
using gbc.DAO;
using log4net;

namespace gbc.Util
{
    public class GeocodeUtil
    {
        public static void GeocodeGeoRecords(IEnumerable<GeoRecord> geoRecords, ILog log)
        {
            foreach (GeoRecord record in geoRecords)
            {
                GeocodeDAO geocodeDao = GeocodeDAO.Create(record);

                if (string.IsNullOrEmpty(geocodeDao.Address))
                {
                    continue;
                }

                string url =
                    string.Format(GeocodeConstants.GeocodeUrl_Addr_Zip,
                                    geocodeDao.Address,
                                    geocodeDao.Zip);

                GeocodeResult geocodeResult = DataUtil.GetAndDeserializeJsonFromWeb<GeocodeResult>(url, log);

                if (geocodeResult == null ||
                        geocodeResult.candidates == null ||
                            !geocodeResult.candidates.Any())
                {
                    continue;
                }

                geocodeDao.Latitude = geocodeResult.candidates[0].location.y.ToString();
                geocodeDao.Longitude = geocodeResult.candidates[0].location.x.ToString();

                GeoRecord.SetGeocodeFields(record, geocodeDao);
            }
        }
    }
}
