using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using gbc.DAO;
using gbc.Configuration;

namespace gbc.Util
{
    public class SDEUtil
    {
        #region Properties and Fields

        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private Int64 wkid { get; set; }

        #endregion


        #region Ctor

        public SDEUtil()
        {

        }
        public SDEUtil(Int64 wkid)
        {
            this.wkid = wkid;
        }

        #endregion


        #region Feature Lifecycle

        public IFeatureClass CreateFeatureClass(IFeatureWorkspace featureWorkspace,
            string sdeObject, IFields fields, string featureType, string keyword)
        {
            //	create a featureclass description
            IFeatureClassDescription _featureClassDescription = new FeatureClassDescriptionClass();
            //	cast to an object description
            IObjectClassDescription _objectClassDescription = (IObjectClassDescription)_featureClassDescription;
            //	create the featureclass from the featureworkspace
            IFeatureClass _featureClass = featureWorkspace.CreateFeatureClass(
                sdeObject,
                fields,
                _objectClassDescription.InstanceCLSID,
                _objectClassDescription.ClassExtensionCLSID,
                GetFeatureType(featureType),
                "SHAPE",
                keyword
            );
            
            return _featureClass;
        }

        public bool InsertFeature(IFeatureClass featureClass, IFeatureCursor insertFeatureCursor, List<GeoField> fields, string geometry, string uniqueKey)
        {
            try
            {
                IFeatureBuffer featureBuffer = featureClass.CreateFeatureBuffer();

                featureBuffer.Shape = GetShape(fields, geometry);

                if (featureBuffer.Shape.IsEmpty)
                {
                    throw new Exception();
                }

                foreach (var f in fields)
                {
                    if (f.GetName().ToLower() == "coordinates")
                    {
                        continue;
                    }

                    featureBuffer.set_Value(featureBuffer.Fields.FindField(f.GetName()), GetTypeSafeValue(f));
                }

                insertFeatureCursor.InsertFeature(featureBuffer);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool UpdateFeature(IFeatureClass featureClass, List<GeoField> fields, string geometry, int objectId)
        {
            IQueryFilter2 query = new QueryFilterClass();
            query.WhereClause = "OBJECTID = '" + objectId + "'";

            IFeatureCursor updateFeatureCursor = featureClass.Update(query, false);
            IFeature feature = null;

            try
            {
                while ((feature = updateFeatureCursor.NextFeature()) != null)
                {
                    //  set feature values;
                    foreach (var f in fields)
                    {
                        if (f.GetName().ToLower() != "coordinates")
                        {
                            feature.set_Value(feature.Fields.FindField(f.GetName()), GetTypeSafeValue(f));
                        }
                    }

                    updateFeatureCursor.UpdateFeature(feature);
                }

                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);

                return false;
            }
        }

        public int InsertFeature(IFeatureClass featureClass, List<GeoField> fields, string geometry, string uniqueKey)
		{
			IFeature _feature = featureClass.CreateFeature();

            try
            {
                _feature.Shape = GetShape(fields, geometry);

                if (_feature.Shape == null || _feature.Shape.IsEmpty)
                {
                    return -1;
                }

                //	set the field values avoiding coordinates field as that should never be included
                foreach (var f in fields)
                {
                    if (f.GetName().ToLower() != "coordinates")
                    {
                        _feature.set_Value(_feature.Fields.FindField(f.GetName()), GetTypeSafeValue(f));
                    }
                }

                _feature.set_Value(_feature.Fields.FindField("uid"), uniqueKey);

                _feature.Store();

                return _feature.OID;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);

                _feature.Delete();

                return -1;
            }
		}

        public bool DeleteFeature(IFeatureClass featureClass, string keyValue, string keyField)
        {
            try
            {
                if (featureClass.Fields.FindField(keyField) > -1)
                {
                    IQueryFilter2 _query = new QueryFilterClass();
                    _query.WhereClause = keyField + " = '" + keyValue + "'";
                    IFeatureCursor _fCursor = featureClass.Update(_query, false);
                    IFeature _feature = null;

                    while ((_feature = _fCursor.NextFeature()) != null)
                    {
                        _fCursor.DeleteFeature();
                    }

                    return true;
                }
            }
            catch (Exception ex) // TODO: log this exception
            {
                Console.WriteLine(ex.Message);

                return false;
            }

            return false;
        }

        #endregion


        #region Table/Row Lifecycle

        public ITable CreateTable(IFeatureWorkspace featureWorksapce,
            string sdeObject, IFields fields, string keyword)
        {
            //	create a featureclass description
            IFeatureClassDescription _featureClassDescription = new FeatureClassDescriptionClass();
            //	cast to an object description
            IObjectClassDescription _objectClassDescription = (IObjectClassDescription)_featureClassDescription;
            //  create the table
            ITable _table = featureWorksapce.CreateTable(
                sdeObject,
                fields,
                _objectClassDescription.InstanceCLSID,
                _objectClassDescription.ClassExtensionCLSID,
                keyword
            );

            return _table;
        }

        public int InsertRow(ITable table, List<GeoField> fields, string uniqueKey)
        {
            //  check for existance of all fields adding missing fields before adding a new row
            AddMissingFields(fields, table);

            IRow _row = table.CreateRow();

            try
            {
                foreach (var f in fields)
                {
                    if (f.GetName().ToLower() != "coordinates")
                    {
                        _row.set_Value(_row.Fields.FindField(f.GetName()), GetTypeSafeValue(f));
                    }
                }

                _row.set_Value(_row.Fields.FindField("uid"), uniqueKey);
            }
            catch (Exception ex) // TODO: log this error
            {
                _row.Delete();

                return -1;
            }

            //  store the row
            _row.Store();
            
            return _row.OID;
        }

        public bool UpdateRow(ITable table, List<GeoField> fields, int objectId)
        {
            IQueryFilter2 query = new QueryFilterClass();
            query.WhereClause = "OBJECTID = '" + objectId + "'";
            
            ICursor updateRowCursor = table.Search(query, false);
            IRow row = null;

            try
            {
                while ((row = updateRowCursor.NextRow()) != null)
                {
                    //  set feature values;
                    foreach (var f in fields)
                    {
                        if (f.GetName().ToLower() != "coordinates")
                        {
                            row.set_Value(table.FindField(f.GetName()), GetTypeSafeValue(f));
                        }
                    }

                    updateRowCursor.UpdateRow(row);
                }

                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);

                return false;
            }
        }

        public bool DeleteRow(ITable table, string keyValue, string keyField)
        {
            try
            {
                //  locate the RECORD_ID
                if (table.Fields.FindField(keyField) > -1)
                {
                    IQueryFilter _query = new QueryFilterClass();
                    _query.WhereClause = keyField + " = '" + keyValue + "'";
                    
                    table.DeleteSearchedRows(_query);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return false;
            }

            return false;
        }

        public void DeleteNullFeatures(IFeatureClass featureClass)
        {
            if (featureClass.Fields.FindField("RECORD_ID") > -1)
            {
                IQueryFilter query = new QueryFilterClass();
                query.WhereClause = "RECORD_ID IS NULL";
                IFeatureCursor fCursor = featureClass.Update(query, false);
                IFeature feature = null;

                while ((feature = fCursor.NextFeature()) != null)
                {
                    fCursor.DeleteFeature();
                }
            }
        }

        public void DeleteNullRows(ITable table, string sdeObject)
        {
            if (table.Fields.FindField("RECORD_ID") > -1)
            {
                IQueryFilter _query = new QueryFilterClass();
                _query.WhereClause = "RECORD_ID IS NULL";
                ICursor _rowCursor = table.Search(_query, false);
                table.DeleteSearchedRows(_query);
            }
        }

        #endregion


        #region Geometry Helpers

        private GeoRecord GetGeoRecordFromIRow(IRow row, string geometry)
        {
            var m_row = (Row)row;

            //  right here i need to match up something from the config that says pull these fields from each record and create a new georecord from
            //  those fields

            return new GeoRecord(geometry);
        }

        private IGeometry GetShape(List<GeoField> fields, string geometry)
        {
            switch (geometry)
            {
                default:
                case "point":
                    {
                        return GetPoint(fields);
                    }
                case "kml_point":
                    {
                        return GetKmlPoint(fields);
                    }
                case ("kml_polygon"):
                    {
                        return GetKmlPoly(fields);
                    }
                case "polygon":
                    {
                        return GetPolygon(fields);
                    }
                case ("kml_polyline"):
                    {
                        return GetKmlPolyline(fields);
                    }
                case "georss_point":
                    {
                        return GetGeorssPoint(fields);
                    }
                // need additional cases for polygon and line
            }
        }

        private IGeometry GetPolygon(List<GeoField> fields)
        {
            IPolygon poly = new PolygonClass();
            IPointCollection ptCollection = poly as IPointCollection;

            //  a missing point used to add the point to the end of the path
            object missing = Type.Missing;

            //  create the points
            GeoField coordsField = fields.FirstOrDefault(p => p.GetName().ToLower() == "coordinates");
            
            if (coordsField == null)
            {
                throw new Exception("Coordinates field missing");
            }

            string coordinates = coordsField.Value;

            coordinates = coordinates.Replace("\n", " ");
            coordinates = coordinates.Replace("\t", "");

            FillPoly('|', poly, ptCollection, ref missing, coordinates);

            return poly;
        }

        private IGeometry GetGeorssPoint(List<GeoField> fields)
        {
            IPoint pt = new PointClass();

            var coordinates = (from p in fields
                               where p.GetName().ToLower() == "coordinates"
                               select p.Value).FirstOrDefault();

            if (coordinates != null)
            {
                var xy = coordinates.Split(' ');

                double x, y;
                x = double.TryParse(xy[1].Trim(), out x) == false ? 0 : x;
                y = double.TryParse(xy[0].Trim(), out y) == false ? 0 : y;

                //  push the coords
                pt.PutCoords(x, y);
                pt.SpatialReference = GetSpatialReference();
            }

            return pt;
        }

        private static IGeometry GetKmlPolyline(List<GeoField> fields)
        {
            object _missing = Type.Missing;
            IGeometryCollection polyLine = new PolylineClass();
            IPointCollection ptCollection = new PathClass();
            IPoint pt;
            object missing = Type.Missing;

            //  create the polyline point path
            var coordinates = (from p in fields
                               where p.GetName().ToLower() == "linestring"
                               select p.Value).FirstOrDefault();

            if (coordinates != null)
            {
                var coordArray = coordinates.Split(' ');

                foreach (string coordPair in coordArray)
                {
                    //  split the coordinates from the string input i.e. 0,0,0 => [0,0,0]
                    var xyz = coordPair.Split(',');
                    pt = new PointClass();

                    double x, y;
                    x = double.TryParse(xyz[0], out x) == false ? 0 : x;
                    y = double.TryParse(xyz[1], out y) == false ? 0 : y;

                    //  push the coords
                    pt.PutCoords(x, y);

                    //  push the point
                    ptCollection.AddPoint(pt, ref missing, ref missing);
                }

                polyLine.AddGeometry(ptCollection as IGeometry, ref _missing, ref _missing);
            }

            return polyLine as IGeometry;
        }

        private static IGeometry GetKmlPoly(List<GeoField> fields, char coordPairSplitChar = ' ')
        {
            IPolygon poly = new PolygonClass();
            IPointCollection ptCollection = poly as IPointCollection;
            
            //  a missing point used to add the point to the end of the path
            object missing = Type.Missing;

            //  create the points
            string coordinates = (from p in fields
                                  where p.GetName().ToLower() == "coordinates"
                                  select p.Value).FirstOrDefault();

            coordinates = coordinates.Replace("\n", " ");
            coordinates = coordinates.Replace("\t", "");

            FillPoly(coordPairSplitChar, poly, ptCollection, ref missing, coordinates);
            
            return poly;
        }

        private static void FillPoly(char coordPairSplitChar, IPolygon poly, IPointCollection ptCollection, ref object missing, string coordinates)
        {
            IPoint pt;

            if (coordinates != null && coordinates.Any())
            {
                var coordArray = coordinates.Split(coordPairSplitChar);

                foreach (string coordPair in coordArray)
                {
                    //  split the coordinates from the string input i.e. 0,0,0 => [0,0,0]
                    var xyz = coordPair.Split(',');
                    if (xyz.Length < 2)
                    {
                        continue;
                    }

                    pt = new PointClass();

                    double x, y, z;

                    x = double.TryParse(xyz[0], out x) == false ? 0 : x;
                    y = double.TryParse(xyz[1], out y) == false ? 0 : y;

                    pt.PutCoords(x, y);

                    ptCollection.AddPoint(pt, ref missing, ref missing);
                }

                poly.Close();
            }
        }

        private IGeometry GetKmlPoint(List<GeoField> fields)
        {
            IPoint pt = new PointClass();

            var coordinates = (from p in fields
                               where p.GetName().ToLower() == "coordinates"
                               select p.Value).FirstOrDefault();

            if (coordinates != null)
            {
                var xyz = coordinates.Split(',');

                double x, y, z;
                x = double.TryParse(xyz[0].Trim(), out x) == false ? 0 : x;
                y = double.TryParse(xyz[1].Trim(), out y) == false ? 0 : y;

                //  push the coords
                pt.PutCoords(x, y);
                pt.SpatialReference = GetSpatialReference();
            }

            return pt;
        }

        private IGeometry GetPoint(List<GeoField> fields)
        {
            IPoint point = new PointClass();
            Double d;
            Double d_0;

            try
            {
                string lat = (from p in fields
                              where p.GetName().ToLower() == "latitude"
                              select p.Value).FirstOrDefault();
                if (lat == null)
                {
                    lat = (from p in fields
                           where p.GetName().ToLower() == "planey"
                           select p.Value).FirstOrDefault();
                }

                string lng = (from p in fields
                              where p.GetName().ToLower() == "longitude"
                              select p.Value).FirstOrDefault();
                if (lng == null)
                {
                    lng = (from p in fields
                           where p.GetName().ToLower() == "planex"
                           select p.Value).FirstOrDefault();
                }

                double latitude = double.TryParse(lat, out d) ? d : default(double);
                double longitude = double.TryParse(lng, out d_0) ? d_0 : default(double);

                point.PutCoords(longitude, latitude);
                point.SpatialReference = GetSpatialReference();
            }
            catch(Exception ex)
            {
                _log.Warn("Error creating point");
                _log.Error(ex.Message, ex);
            }

            return point;
        }

        //	Creates and returns a spatial reference
        private ISpatialReference GetSpatialReference()
        {
            ISpatialReferenceFactory2 spatialRefFactory = new SpatialReferenceEnvironmentClass();
            ISpatialReference spatialRef;

            if (ApplicationConstants.Wkids.GeographicCoordinateSystemWkids.Contains(wkid))
            {
                spatialRef = spatialRefFactory.CreateGeographicCoordinateSystem((int)wkid);
            }
            else
            {
                spatialRef = spatialRefFactory.CreateProjectedCoordinateSystem((int)wkid);
            }

            IControlPrecision2 controlPrecision = spatialRef as IControlPrecision2;
            controlPrecision.IsHighPrecision = true;
            ISpatialReferenceResolution spatialRefRes = spatialRef as ISpatialReferenceResolution;
            spatialRefRes.ConstructFromHorizon();
            spatialRefRes.SetDefaultXYResolution();
            ISpatialReferenceTolerance spatialRefTolerance = spatialRefRes as ISpatialReferenceTolerance;
            spatialRefTolerance.SetDefaultXYTolerance();
            double xmin, xmax, ymin, ymax;
            spatialRef.GetDomain(out xmin, out xmax, out ymin, out ymax);

            return spatialRef;
        }

        //	creates the required fields necessary for featureclass creation (OID and SHAPE)
        public IFields CreateRequiredFields(string geometry)
        {
            IField2 _field;
            IFieldEdit2 _fieldEdit;
            IFields _fields = new FieldsClass();
            IFieldsEdit _fieldsEdit = (IFieldsEdit)_fields;
            //  specifies how many fields will be created            
            _fieldsEdit.FieldCount_2 = geometry.ToLower() == "table" ? 2 : 3;

            //  create the OID field
            _field = new FieldClass();
            _fieldEdit = (IFieldEdit2)_field;
            _fieldEdit.Name_2 = "ObjectID";
            _fieldEdit.AliasName_2 = "FID";
            _fieldEdit.Type_2 = esriFieldType.esriFieldTypeOID;
            _fieldsEdit.set_Field(0, _field);

            //  create the Unique Key Field
            _field = new FieldClass();
            _fieldEdit = (IFieldEdit2)_field;
            _fieldEdit.Name_2 = "uid";
            _fieldEdit.AliasName_2 = "Unique Key";
            _fieldEdit.Type_2 = esriFieldType.esriFieldTypeString;
            _fieldsEdit.set_Field(1, _field);

            if (geometry.ToLower() != ApplicationConstants.SDEGeometry.Table)
            {
                //  create the shape field
                _field = new FieldClass();
                _fieldEdit = (IFieldEdit2)_field;
                _fieldEdit.Name_2 = "SHAPE";
                _fieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
                _fieldEdit.GeometryDef_2 = GetGeometryDef(geometry);
                _fieldEdit.Required_2 = true;
                _fieldsEdit.set_Field(2, _field);
            }

            return _fields;
        }

        //	creates and returns a geometry definition
        private IGeometryDef GetGeometryDef(string geometry)
        {
            IGeometryDef geoDef = new GeometryDefClass();
            IGeometryDefEdit geoDefEdit = (IGeometryDefEdit)geoDef;

            geoDefEdit.GeometryType_2 = GetGeometryType(geometry);
            geoDefEdit.GridCount_2 = 1;
            geoDefEdit.set_GridSize(0, 0);
            geoDefEdit.HasM_2 = false;
            geoDefEdit.HasZ_2 = false;
            geoDefEdit.SpatialReference_2 = GetSpatialReference();

            return geoDef;
        }

        //	creates and returns a geometry type
        private esriGeometryType GetGeometryType(string geometry)
        {
            switch (geometry.ToLower())
            {
                default:
                case ApplicationConstants.SDEGeometry.Kml_Point:
                case ApplicationConstants.SDEGeometry.Point:
                    {
                        return esriGeometryType.esriGeometryPoint;
                    }
                case ApplicationConstants.SDEGeometry.Line:
                    {
                        return esriGeometryType.esriGeometryLine;
                    }
                case ApplicationConstants.SDEGeometry.Kml_PolyLine:
                case ApplicationConstants.SDEGeometry.PolyLine:
                    {
                        return esriGeometryType.esriGeometryPolyline;
                    }
                case ApplicationConstants.SDEGeometry.Polygon:
                case ApplicationConstants.SDEGeometry.Kml_Polygon:
                    {
                        return esriGeometryType.esriGeometryPolygon;
                    }
            }
        }

        private esriFeatureType GetFeatureType(string fType)
        {
            switch (fType)
            {
                default:
                case ApplicationConstants.SDEGeometry.Point:
                case ApplicationConstants.SDEGeometry.Polygon:
                case ApplicationConstants.SDEGeometry.Line:
                    {
                        return esriFeatureType.esriFTSimple;
                    }
            }
        }

        #endregion


        #region Field Helpers

        //	creates all missing fields prior to attempting to input data on featureclass
        public void AddMissingFields(List<GeoField> fields, IFeatureClass featureClass)
        {
            if (fields == null ||
                    featureClass == null)
            {
                return;
            }

            foreach (var field in fields)
            {
                if (featureClass.FindField(field.GetName()) < 0 && field.GetName() != ApplicationConstants.ReservedFieldNames.Coordinates)
                {
                    featureClass.AddField(CreateIField(field));
                }
            }
        }

        //  creates all missing fields prior to attempting to input data on table
        public void AddMissingFields(List<GeoField> fields, ITable table)
        {
            foreach (var field in fields)
            {
                //  truncate field names larger than 19 characters
                field.Name = field.GetName().Length > 19 ? field.GetName().Substring(0, 18) : field.GetName();

                if (table.FindField(field.GetName()) < 0 && field.GetName() != ApplicationConstants.ReservedFieldNames.Coordinates)
                {
                    table.AddField(CreateIField(field));
                }
            }
        }

        //	creates and returns a new IField from the input gb Field
        private IField CreateIField(GeoField field)
        {
            IField2 _f = new FieldClass();
            IFieldEdit2 fieldEdit = (IFieldEdit2)_f;
            fieldEdit.Name_2 = field.GetName();

            //  set field length or precision depending on type and prevent null values
            if (GetFieldType(field.DataType) == esriFieldType.esriFieldTypeString)
            {
                field.Length = CheckLength(field);
                fieldEdit.Length_2 = System.Convert.ToInt32(field.Length);
            }
            else if (GetFieldType(field.DataType) == esriFieldType.esriFieldTypeDouble)
            {
                field.Length = CheckLength(field);
                fieldEdit.Precision_2 = System.Convert.ToInt32(field.Length);
                fieldEdit.Scale_2 = 7;
            }
            else
            {
                field.Length = CheckLength(field);
                fieldEdit.Precision_2 = System.Convert.ToInt32(field.Length);
            }

            fieldEdit.IsNullable_2 = true;
            fieldEdit.Type_2 = GetFieldType(field.DataType);

            return _f;
        }

        //	Checks an input field for length and modifies the field.length parameter if one is not present.
        private string CheckLength(GeoField f)
        {
            if (GetFieldType(f.DataType) == esriFieldType.esriFieldTypeString)
            {
                if (f.Length == String.Empty || f.Length == null)
                    f.Length = "255";
            }
            else
            {
                if (f.Length == String.Empty || f.Length == null || System.Convert.ToInt32(f.Length) < 25)
                    f.Length = "25";
            }
            return f.Length;
        }

        //	Returns an esriFieldType object based on the input type of the geoboards field
        private esriFieldType GetFieldType(string inputType)
        {
            switch (inputType.ToLower())
            {
                case "string":
                    {
                        return esriFieldType.esriFieldTypeString;
                    }
                case "long":
                    {
                        return esriFieldType.esriFieldTypeInteger;
                    }
                case "short":
                    {
                        return esriFieldType.esriFieldTypeSmallInteger;
                    }
                case "double":
                    {
                        return esriFieldType.esriFieldTypeDouble;
                    }
                case "date":
                    {
                        return esriFieldType.esriFieldTypeDate;
                    }
                case "float":
                    {
                        return esriFieldType.esriFieldTypeSingle;
                    }
                default:
                    {
                        return esriFieldType.esriFieldTypeString;
                    }
            }
        }

        /// <summary>
        /// Converts the string input value to its ESRI data type value
        /// </summary>
        /// <param name="f">The input field</param>
        /// <returns>esriFieldType value</returns>
        private object GetTypeSafeValue(GeoField f)
        {
            switch (GetFieldType(f.DataType))
            {
                case esriFieldType.esriFieldTypeString:
                default:
                    {
                        int maxLength;

                        if (int.TryParse(f.Length, out maxLength) &&
                                f.Value.Length > maxLength)
                        {
                            return f.Value.Substring(0, maxLength - 1);
                        }

                        return f.Value;
                    }
                case esriFieldType.esriFieldTypeSingle:
                    {
                        float z;
                        return float.TryParse(f.Value, out z) ? z : default(float);
                    }
                case esriFieldType.esriFieldTypeDouble:
                    {
                        double z;
                        return double.TryParse(f.Value, out z) ? z : default(double);
                    }
                case esriFieldType.esriFieldTypeInteger:
                    {
                        int z;
                        return int.TryParse(f.Value, out z) ? z : default(int);
                    }
                case esriFieldType.esriFieldTypeSmallInteger:
                    {
                        short z;
                        return short.TryParse(f.Value, out z) ? z : default(short);
                    }
            }
        }

        #endregion
    }
}
