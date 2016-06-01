using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.Configuration
{
    public static class ApplicationConstants
    {
        public const string GeoRecordRootXmlElementName = "georecords";
        public const string GeoRecordXmlElementName = "georecord";
        public const string GeoFieldXmlElementName = "geofield";
        public const string GeoRecordIdXmlElementName = "id";

        public const string ExtractedGeoFieldsConfigSectionName = "geofields";
        public const string RestApiConfigSectionName = "restapi";

        public const string ResourceLockExtension = ".lock";
        public const string ResourceExtension = ".res";

        public class SDEGeometry
        {
            public const string Table = "table";
            public const string Point = "point";
            public const string Line = "line";
            public const string PolyLine = "polyline";
            public const string Polygon = "polygon";

            public const string Kml_Point = "kml_point";
            public const string Kml_Line = "kml_line";
            public const string Kml_PolyLine = "kml_polyline";
            public const string Kml_Polygon = "kml_polygon";
        }

        public class ReservedFieldNames
        {
            public const string Coordinates = "coordinates";
        }

        public class Wkids
        {
            public static List<Int64> ProjectedCoordinateSystemWkids = new List<Int64>() { 3159 };
            public static List<Int64> GeographicCoordinateSystemWkids = new List<Int64>() { 4326 };
        }
    }
}
