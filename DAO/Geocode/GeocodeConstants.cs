using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.DAO.Geocode
{
    public static class GeocodeConstants
    {
        public static class Status
        {
            public const string OK = "OK";
            public const string ZERO_RESULTS = "ZERO_RESULTS";
            public const string OVER_QUERY_LIMIT = "OVER_QUERY_LIMIT";
            public const string REQUEST_DENIED = "REQUEST_DENIED";
            public const string INVALID_REQUEST = "INVALID_REQUEST";
            public const string UNKNOWN_ERROR = "UNKNOWN_ERROR";
        }

        public static class Fields
        {
            public const string Address = "address";
            public const string State = "state";
            public const string Zip = "zip";
            public const string Latitude = "latitude";
            public const string Longitude = "longitude";
        }

        public const string GeocodeUrl_Addr_Zip = "http://gisq.in.gov/arcgis/rest/services/Indiana_Composite_Locator/GeocodeServer/findAddressCandidates?Street={0}&Zip={1}&outFields=&outSR=4326&f=pjson";
    }
}
