using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.Constants
{
    public static class SDESqlConstants
    {
        public static class QueryReturns
        {
            public const string NEXT_OBJECTID = "NObjectID";
        }
        public static class QueryFormat
        {
            public const string SELECT_OBJECTIDS_FROM_TABLE = "SELECT UKEY FROM dbo.{0}";
            public const string SELECT_NEXT_OBJECTID_FOR_TABLE = "DECLARE @id as integer EXEC dbo.next_rowid '{0}', '{1}', @id OUTPUT; SELECT @id '" + QueryReturns.NEXT_OBJECTID + "';";
        }
        public static class STGeometryFormat
        {
            /// <summary>
            /// A single point (x y) and wkid
            /// </summary>
            public const string POINT = "geography::STPointFromText('POINT({0} {1})', {2})";
            /// <summary>
            /// A comma delimited line segment (x y, x y, x y) and wkid
            /// </summary>
            public const string LINE = "geography::STLineFromText('LINESTRING({0})', {1});";
            /// <summary>
            /// A comma delimited collection of closing x y coordinates (x1 y1, x2 y2, x1 y1) and wkid 
            /// </summary>
            public const string POLYGON = "geography::STPolyFromText('POLYGON(({0}', 4326);";
        }
        public static class DataCaptureTypes
        {
            public const string BINARY = "binary";
            public const string VARBINARY = "varbinary";
            public const string COORDS_POLY = "coords_poly";
        }
    }
}
