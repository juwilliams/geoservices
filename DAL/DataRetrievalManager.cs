using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using gbc.Configuration;
using gbc.Constants;
using gbc.DAO;
using gbc.DAO.ArcGISRest;
using gbc.DAO.Cars;
using gbc.DAO.DA;
using gbc.DAO.Trafficwise;
using gbc.Util;
using gbc.WebEOC7;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpatialConnect.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

using Container = SpatialConnect.Entity.Container;
using SCFields = SpatialConnect.Entity.Fields;

namespace gbc.DAL
{
    public class DataRetrievalManager
    {
        #region Properties and Fields

        private static readonly log4net.ILog _log = LogManager.GetLogger(typeof(DataRetrievalManager));
        private Container _Container { get; set; }
        private DataRetriever _Retriever { get; set; }

        public delegate void DataErrorEventHandler(object sender, Exception ex);
        public delegate void DataSuccessEventHandler(object sender, List<GeoRecord> records);

        public event DataErrorEventHandler OnDataRetrievalError;
        public event DataSuccessEventHandler OnDataRetrievalSuccess;

        #endregion


        #region Ctor

        public DataRetrievalManager(Container container)
        {
            this._Container = container;
        }

        private void InitializeDataRetriever()
        {
            this._Retriever = new DataRetriever();

            switch (this._Container.format.ToLower())
            {
                case DataResponseFormatConstants.XML:
                    {
                        _Retriever.OnGetDataAsXmlSuccess += new DAL.DataRetriever.XDocumentHandler(DataRetriever_OnGetDataAsXmlSuccess);
                        _Retriever.GetResponseAsXml(this._Container.source);
                        
                        break;
                    }
                case DataResponseFormatConstants.SECURE_XML:
                    {
                        _Retriever.OnGetDataAsXmlSuccess += new DAL.DataRetriever.XDocumentHandler(DataRetriever_OnGetDataAsXmlSuccess);
                        _Retriever.GetPasswordProtectedResponseAsXml(this._Container.source_username, this._Container.source_password, this._Container.source);
                        
                        break;
                    }
                case DataResponseFormatConstants.STRING:
                    {
                        _Retriever.OnGetDataAsStringSuccess += new DAL.DataRetriever.StringHandler(DataRetriever_OnGetDataAsStringSuccess);
                        _Retriever.GetResponseAsString(this._Container.source);
                        
                        break;
                    }
                case DataResponseFormatConstants.TRAFFICWISE_JSON:
                case DataResponseFormatConstants.CARS_JSON:
                    {
                        _Retriever.OnGetDataAsStringSuccess += new DAL.DataRetriever.StringHandler(DataRetriever_OnGetDataAsJsonStringSuccess);
                        _Retriever.GetResponseAsString(this._Container.source);

                        break;
                    }
                case DataResponseFormatConstants.FILE:
                    {
                        _Retriever.OnGetDataAsFileSuccess += new DAL.DataRetriever.FileHandler(DataRetriever_OnGetDataAsFileSuccess);
                        _Retriever.GetResponseAsFile(this._Container.source, this._Container.output_file);
                        break;
                    }
                case DataResponseFormatConstants.SECURE_FILE:
                    {
                        _Retriever.OnGetDataAsFileSuccess += new DAL.DataRetriever.FileHandler(DataRetriever_OnGetDataAsFileSuccess);
                        _Retriever.GetPasswordProtectedResponseAsFile(GetNetworkCredential(),
                                                                         this._Container.source,
                                                                         this._Container.output_file);
                        break;
                    }
                case DataResponseFormatConstants.WEBEOC:
                    {
                        _Retriever.OnGetDataAsStringSuccess += new DAL.DataRetriever.StringHandler(DataRetriever_OnGetDataAsStringSuccess);
                        _Retriever.GetWebEOCBoardDataAsString(GetWebEOCCredentials(), this._Container.board, this._Container.view);
                        
                        break;
                    }
                case DataResponseFormatConstants.ARCGIS:
                    {
                        _Retriever.OnSdeGetDataAsIEnumerableSuccess += new DAL.DataRetriever.ICursorHandler(DataRetriever_OnSdeGetCursorSuccess);
                        _Retriever.GetICursorFromSde(this._Container);
                        
                        break;
                    }
                case DataResponseFormatConstants.DAMAGE_ASSESSMENT:
                    {
                        _Retriever.OnGetDamageAssessmentSuccess += new DAL.DataRetriever.DamageAssessmentHandler(DataRetriever_OnGetDamageAssessmentSuccess);
                        _Retriever.GetDamageAssessment();
                        break;
                    }
                case DataResponseFormatConstants.HOSTED_SHAPEFILE:
                    {
                        _Retriever.OnGetDataAsFileSuccess += new DAL.DataRetriever.FileHandler(DataRetriever_OnGetHostedShapefileSuccess);
                        _Retriever.GetResponseAsFile(this._Container.source, this._Container.output_file);

                        break;
                    }
                case DataResponseFormatConstants.HOSTED_ZIPPED_SHAPEFILE:
                    {
                        _Retriever.OnGetDataAsFileSuccess += new DAL.DataRetriever.FileHandler(DataRetriever_OnGetHostedZippedShapeFile);
                        _Retriever.GetResponseAsFile(this._Container.source, this._Container.output_file);

                        break;
                    }
                case DataResponseFormatConstants.HOSTED_GZIPPED_TAR_SHAPEFILE:
                    {
                        _Retriever.OnGetDataAsFileSuccess += new DAL.DataRetriever.FileHandler(DataRetriever_OnGetGzippedTarShapeFile);
                        _Retriever.GetGzippedTarFile(this._Container.source, this._Container.Config.app_path + "\\" + this._Container.name + "\\temp\\" + this._Container.output_file);

                        break;
                    }
                case DataResponseFormatConstants.REST_XML:
                    {
                        _Retriever.OnGetDataAsXmlSuccess += new DAL.DataRetriever.XDocumentHandler(DataRetriever_OnGetDataAsXmlSuccess);
                        _Retriever.GetRestResponseAsXml(this._Container.source);

                        break;
                    }
                case DataResponseFormatConstants.ARCGIS_REST:
                    {
                        _Retriever.OnGetDataAsStringSuccess += new DAL.DataRetriever.StringHandler(DataRetriever_OnGetDataAsJsonStringSuccess);
                        _Retriever.GetArcGISRestResponseAsString(this._Container.source, this._Container.where_clause, this._Container.Fields);

                        break;
                    }
            }
        }

        void DataRetriever_OnGetDataFailure(object sender, string message)
        {
            _log.Error(message);
        }

        #endregion


        #region Data Getters

        public void GetData()
        {
            try
            {
                InitializeDataRetriever();
            }
            catch (FaultException soapFault)
            {
                _log.Error(soapFault.Message, soapFault);
            }
            catch (Exception ex)
            {
                OnDataRetrievalError?.Invoke(this, ex);

                _log.Error("error encountered while attempting to retrieve data from source: ", ex);
            }
        }

        private void DataRetriever_OnGetDataAsFileSuccess(object sender, string path)
        {
            throw new NotImplementedException();
        }

        private void DataRetriever_OnGetDataAsJsonStringSuccess(object sender, string data)
        {
            switch (this._Container.format.ToLower())
            {
                case DataResponseFormatConstants.CARS_JSON:
                    {
                        dynamic json = JsonConvert.DeserializeObject(data);

                        JArray jObjects = JArray.Parse(json.ToString());

                        List<CarsReport> reports = CarsReport.Parse(jObjects);

                        GetSpatialRecords(reports);

                        break;
                    }
                case DataResponseFormatConstants.TRAFFICWISE_JSON:
                    {
                        dynamic json = JsonConvert.DeserializeObject(data);

                        JObject jObject = JObject.Parse(json.ToString());

                        TrafficWiseMessage trafficWiseMessage = TrafficWiseMessage.Parse(jObject);

                        GetSpatialRecords(trafficWiseMessage);

                        break;
                    }
                case DataResponseFormatConstants.ARCGIS_REST:
                    {
                        dynamic json = JsonConvert.DeserializeObject(data);

                        JObject jObject = JObject.Parse(json.ToString());

                        RestMessage restMessage = RestMessage.Parse(jObject);

                        GetSpatialRecords(restMessage);

                        break;
                    }
            }
        }

        private void DataRetriever_OnGetDataAsStringSuccess(object sender, string data)
        {
            XDocument doc;

            if (this._Container.transform)
            {
                doc = TransformData(data);
            }
            else
            {
                doc = XDocument.Parse(data);
            }

            GetSpatialRecords(doc);
        }

        private void DataRetriever_OnGetDataAsXmlSuccess(object sender, XDocument doc)
        {
            if (this._Container.transform)
            {
                doc = TransformData(doc.ToString());
            }
            else
            {
                doc = XDocument.Parse(doc.ToString());
            }

            GetSpatialRecords(doc);
        }

        private void DataRetriever_OnGetDataAsXmlListSuccess(object sender, List<XDocument> docs)
        {
            List<XDocument> outputDocs = new List<XDocument>();
            XDocument tempDoc;

            foreach (XDocument doc in docs)
            {
                if (this._Container.transform)
                {
                    tempDoc = TransformData(doc.ToString());
                }
                else
                {
                    tempDoc = doc;
                }

                outputDocs.Add(tempDoc);
            }

            GetSpatialRecords(outputDocs);
        }

        private void DataRetriever_OnSdeGetCursorSuccess(object sender, ICursor cursor)
        {
            GetGeoRecordsFromICursor(cursor);
        }

        private void DataRetriever_OnGetDamageAssessmentSuccess(object sender, string[] counties)
        {
            List<IncidentRecord> incidentRecords = new List<IncidentRecord>();

            for (int i = 0; i < counties.Length; i++)
            {
                incidentRecords.Add(IncidentRecord.Parse(counties[i]));
            }

            GetSpatialRecords(incidentRecords);
        }

        private void DataRetriever_OnGetHostedShapefileSuccess(object sender, string downloadedFileName)
        {
            if (string.IsNullOrEmpty(downloadedFileName))
            {
                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, new Exception(ExceptionConstants.Messages.Configuration.OUTPUT_FILE_NOT_SET));
                }

                return;
            }

            SDE.BindLicense(this._Container.license_type);

            IFeatureCursor shapeFeatureCursor = SDEManager.GetFeatureCursorFromShapefile(this._Container.file_in_archive, this._Container.temp_dir, this._Container.where_clause);

            if (shapeFeatureCursor == null)
            {
                throw new Exception("failed to retrieve cursor object from input shapefile");
            }

            GetSpatialRecords(shapeFeatureCursor);

            SDE.CheckInLicense();
        }

        private void DataRetriever_OnGetHostedZippedShapeFile(object sender, string downloadedFileName)
        {
            if (string.IsNullOrEmpty(this._Container.file_in_archive))
            {
                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, new Exception(ExceptionConstants.Messages.Configuration.FILE_IN_ARCHIVE_NOT_SET));
                }

                return;
            }

            if (string.IsNullOrEmpty(downloadedFileName))
            {
                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, new Exception(ExceptionConstants.Messages.Configuration.OUTPUT_FILE_NOT_SET));
                }

                return;
            }

            SDE.BindLicense(this._Container.license_type);

            DataUtil.ExtractZip(downloadedFileName, this._Container.temp_dir);

            IFeatureCursor shapeFeatureCursor = SDEManager.GetFeatureCursorFromShapefile(this._Container.file_in_archive, this._Container.temp_dir, this._Container.where_clause);

            if (shapeFeatureCursor == null)
            {
                throw new Exception("failed to retrieve cursor object from input shapefile");
            }

            GetSpatialRecords(shapeFeatureCursor);

            SDE.CheckInLicense();
        }

        private void DataRetriever_OnGetGzippedTarShapeFile(object sender, string downloadedFilePath)
        {
            if (string.IsNullOrEmpty(this._Container.file_in_archive))
            {
                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, new Exception(ExceptionConstants.Messages.Configuration.FILE_IN_ARCHIVE_NOT_SET));
                }

                return;
            }
            
            if (string.IsNullOrEmpty(downloadedFilePath))
            {
                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, new Exception(ExceptionConstants.Messages.Configuration.OUTPUT_FILE_NOT_SET));
                }

                return;
            }

            SDE.BindLicense(this._Container.license_type);

            //DataUtil.DecompressTar(downloadedFilePath, this._Container.Config.app_path + "\\" + this._Container.name + "\\temp\\");
            //DataUtil.ExtractTar(downloadedFilePath, this._Container.Config.app_path + "\\" + this._Container.name + "\\temp\\");
            DataUtil.DecompressAndExtractTGZ(downloadedFilePath, this._Container.Config.app_path + "\\" + this._Container.name + "\\temp\\");

            IFeatureCursor shapeFeatureCursor = SDEManager.GetFeatureCursorFromShapefile(this._Container.file_in_archive, this._Container.Config.app_path + "\\" + this._Container.name + "\\temp\\", this._Container.where_clause);

            if (shapeFeatureCursor == null)
            {
                throw new Exception("failed to retrieve cursor object from input shapefile");
            }

            GetSpatialRecords(shapeFeatureCursor);

            SDE.CheckInLicense();
        }

        #endregion


        #region GeoRecords

        private void GetSpatialRecords(RestMessage restMessage)
        {
            try
            {
                List<GeoRecord> spatialRecords = new List<GeoRecord>();

                foreach (RestFeature feature in restMessage.Features)
                {
                    GeoRecord spatialRecord = CreateRecord(feature, this._Container.Fields);

                    spatialRecords.Add(spatialRecord);
                }

                if (OnDataRetrievalSuccess != null)
                {
                    OnDataRetrievalSuccess(this, spatialRecords);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);

                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, ex);
                }
            }
        }

        private void GetSpatialRecords(TrafficWiseMessage trafficWiseMessage)
        {
            try
            {
                List<GeoRecord> spatialRecords = new List<GeoRecord>();

                foreach (TrafficWiseFeature feature in trafficWiseMessage.Features)
                {
                    GeoRecord spatialRecord = CreateRecord(feature, this._Container.Fields);

                    spatialRecords.Add(spatialRecord);
                }

                if (OnDataRetrievalSuccess != null)
                {
                    OnDataRetrievalSuccess(this, spatialRecords);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);
                
                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, ex);
                }
            }
        }

        private void GetSpatialRecords(IFeatureCursor cursor)
        {
            try
            {
                var spatialRecords = new List<GeoRecord>();
                IFeature feature;
                
                while ((feature = cursor.NextFeature()) != null)
                {
                    spatialRecords.Add(CreateRecord(feature, this._Container.Fields));
                }

                if (OnDataRetrievalSuccess != null)
                {
                    OnDataRetrievalSuccess(this, spatialRecords);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);

                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, ex);
                }
            }
        }

        private void GetGeoRecordsFromICursor(ICursor cursor)
        {
            try
            {
                var records = new List<GeoRecord>();
                var m_cursor = (Cursor)cursor;

                IRow row;
                while ((row = m_cursor.NextRow()) != null)
                {
                    records.Add(CreateRecord(row, this._Container.Fields));
                }

                if (OnDataRetrievalSuccess != null)
                {
                    OnDataRetrievalSuccess(this, records);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);

                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, ex);
                }
            }
        }

        private void GetSpatialRecords(List<XDocument> docs)
        {
            try
            {
                List<GeoRecord> spatialRecords = new List<GeoRecord>();

                foreach (XDocument doc in docs)
                {
                    string outputFile = GetOutputRetrievedFilePath();

                    var xmlRecords = from p in doc.Root.Descendants(ApplicationConstants.GeoRecordXmlElementName)
                                     select p;

                    spatialRecords.AddRange(xmlRecords.Select(record => CreateRecord(record, this._Container.Fields)).ToList());
                }

                if (OnDataRetrievalSuccess != null)
                {
                    OnDataRetrievalSuccess(this, spatialRecords);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);

                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, ex);
                }
            }
        }

        private void GetSpatialRecords(XDocument doc)
        {
            try
            {
                List<GeoRecord> spatialRecords = new List<GeoRecord>();

                string outputFile = GetOutputRetrievedFilePath();

                IEnumerable<XElement> records = doc.Root.Descendants(ApplicationConstants.GeoRecordXmlElementName);

                foreach (XElement recordElement in records)
                {
                    GeoRecord spatialRecord = CreateRecord(recordElement, this._Container.Fields);
                    
                    spatialRecords.Add(spatialRecord);
                }

                if (OnDataRetrievalSuccess != null)
                {
                    OnDataRetrievalSuccess(this, spatialRecords);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);

                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, ex);
                }
            }
        }

        private void GetSpatialRecords(List<IncidentRecord> incidentRecords)
        {
            try
            {
                List<GeoRecord> spatialRecords = new List<GeoRecord>();

                foreach (IncidentRecord incidentRecord in incidentRecords)
                {
                    spatialRecords.Add(this.CreateRecord(incidentRecord, this._Container.Fields));
                }

                if (OnDataRetrievalSuccess != null)
                {
                    OnDataRetrievalSuccess(this, spatialRecords);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);

                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, ex);
                }
            }
        }

        private void GetSpatialRecords(List<CarsReport> reports)
        {
            try
            {
                List<GeoRecord> spatialRecords = new List<GeoRecord>();

                foreach (CarsReport report in reports)
                {
                    spatialRecords.Add(this.CreateRecord(report, this._Container.Fields));
                }

                if (OnDataRetrievalSuccess != null)
                {
                    OnDataRetrievalSuccess(this, spatialRecords);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);

                if (OnDataRetrievalError != null)
                {
                    OnDataRetrievalError(this, ex);
                }
            }
        }

        private GeoRecord CreateRecord(IFeature feature, SCFields fields)
        {
            GeoRecord record = new GeoRecord(this._Container.geometry);

            foreach (Mapping mapping in fields.mappings)
            {
                var fieldIndex = feature.Fields.FindField(mapping.field_from);

                if (fieldIndex > -1 ||
                        mapping.HasTag(GeoFieldConstants.Tags.GENERATED))
                {
                    GeoField field = new GeoField(mapping);

                    if (mapping.type == SDESqlConstants.DataCaptureTypes.BINARY ||
                            mapping.type == SDESqlConstants.DataCaptureTypes.VARBINARY)
                    {
                        field.Data = this.GetBytes(feature.get_Value(fieldIndex));
                    }
                    else if (mapping.type == SDESqlConstants.DataCaptureTypes.COORDS_POLY)
                    {
                        field.Value = GetPolygonAsString(feature, field);
                    }
                    else
                    {
                        field.Value = feature.get_Value(fieldIndex).ToString();
                    }

                    record.fields.Add(field);

                    //  set the key for this record if the mapping for the key is the same as the current field mapping
                    if (this._Container.use_relationships &&
                            this._Container.key.ToLower() == mapping.field_from.ToLower())
                    {
                        record.id = field.Value;
                    }
                }
            }

            record.geometry = this._Container.geometry;
            record.uid = MD5Encoder.GetMD5HashId(record);

            return record;
        }

        private string GetPolygonAsString(IFeature feature, IGeoField geoField)
        {
            Polygon polygon = feature.ShapeCopy as Polygon;
            StringBuilder sBuilder = new StringBuilder();
            string pointAsStringFormat = "{0},{1}";

            if (polygon == null)
            {
                return string.Empty;
            }

            for (int i = 0, pointLen = polygon.PointCount; i < pointLen; i++)
            {
                IPoint point = polygon.get_Point(i);

                string pointAsString = string.Format(pointAsStringFormat, point.X, point.Y);
                
                sBuilder.Append(i < pointLen - 1
                    ? pointAsString + "|"
                    : pointAsString);
            }

            return sBuilder.ToString();
        }

        private GeoRecord CreateRecord(IRow row, SCFields fields, Mapping key = null)
        {
            var spatialRecord = new GeoRecord(this._Container.geometry);

            foreach (var mapping in fields.mappings)
            {
                var fieldIndex = row.Fields.FindField(mapping.field_from);

                if (fieldIndex > -1)
                {
                    GeoField recordField = new GeoField(mapping);

                    if (mapping.type == SDESqlConstants.DataCaptureTypes.BINARY ||
                            mapping.type == SDESqlConstants.DataCaptureTypes.VARBINARY)
                    {
                        recordField.Data = this.GetBytes(row.get_Value(fieldIndex));
                    }
                    else
                    {
                        recordField.Value = row.get_Value(fieldIndex).ToString();
                    }

                    //  set the key for this record if the mapping for the key is the same as the current field mapping
                    if (this._Container.use_relationships && 
                            this._Container.key.ToLower() == mapping.field_from.ToLower())
                    {
                        spatialRecord.id = recordField.Value;
                    }

                    spatialRecord.fields.Add(recordField);
                }
            }

            spatialRecord.geometry = this._Container.geometry;
            spatialRecord.uid = MD5Encoder.GetMD5HashId(spatialRecord);

            return spatialRecord;
        }

        private GeoRecord CreateRecord(XElement record, SCFields fields)
        {
            GeoRecord spatialRecord = new GeoRecord(this._Container.geometry);

            foreach (Mapping mapping in fields.mappings)
            {
                var xmlMatch = record.Descendants(mapping.field_from).FirstOrDefault();
                if (xmlMatch != null)
                {
                    GeoField recordField = new GeoField(mapping);

                    recordField.Value = xmlMatch.Value;
                    spatialRecord.fields.Add(recordField);

                    //  set the key for this record if the mapping for the key is the same as the current field mapping
                    if (this._Container.use_relationships &&
                            this._Container.key.ToLower() == mapping.field_from.ToLower())
                    {
                        spatialRecord.id = recordField.Value;
                    }
                }
            }

            spatialRecord.geometry = this._Container.geometry;
            spatialRecord.uid = MD5Encoder.GetMD5HashId(spatialRecord);

            return spatialRecord;
        }

        private GeoRecord CreateRecord(IncidentRecord incidentRecord, SCFields fields)
        {
            GeoRecord spatialRecord = new GeoRecord(this._Container.geometry);

            foreach (Mapping mapping in fields.mappings)
            {
                GeoField field = new GeoField(mapping);

                Type type = incidentRecord.GetType();
                PropertyInfo propInfo = type.GetProperty(field.GetName());

                if (propInfo == null)
                {
                    continue;
                }

                field.Value = propInfo.GetValue(incidentRecord, null).ToString();
                spatialRecord.fields.Add(field);

                //  set the key for this record if the mapping for the key is the same as the current field mapping
                if (this._Container.use_relationships &&
                        this._Container.key.ToLower() == mapping.field_from.ToLower())
                {
                    spatialRecord.id = field.Value;
                }
            }

            spatialRecord.geometry = this._Container.geometry;
            spatialRecord.uid = MD5Encoder.GetMD5HashId(spatialRecord);

            return spatialRecord;
        }

        private GeoRecord CreateRecord(CarsReport report, SCFields fields)
        {
            GeoRecord spatialRecord = new GeoRecord(this._Container.geometry);

            foreach (Mapping mapping in fields.mappings)
            {
                GeoField field = new GeoField(mapping);

                Type type = report.GetType();
                PropertyInfo propInfo = type.GetProperty(mapping.field_from, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (propInfo == null)
                {
                    continue;
                }

                field.Value = propInfo.GetValue(report, null).ToString();
                spatialRecord.fields.Add(field);

                //  set the key for this record if the mapping for the key is the same as the current field mapping
                if (this._Container.use_relationships &&
                        this._Container.key.ToLower() == mapping.field_from.ToLower())
                {
                    spatialRecord.id = field.Value;
                }
            }

            spatialRecord.geometry = this._Container.geometry;
            spatialRecord.uid = MD5Encoder.GetMD5HashId(spatialRecord);

            return spatialRecord;
        }

        private GeoRecord CreateRecord(RestFeature restFeature, SCFields fields)
        {
            GeoRecord spatialRecord = new GeoRecord(this._Container.geometry);

            foreach (Mapping mapping in fields.mappings)
            {
                GeoField field = new GeoField(mapping);

                RestAttribute matchingAttribute = restFeature.Attributes.FirstOrDefault(p => p.Key.ToLower() == mapping.field_from.ToLower());
                
                if (matchingAttribute == null)
                {
                    continue;
                }

                field.Value = matchingAttribute.Value;
                spatialRecord.fields.Add(field);

                //  set the key for this record if the mapping for the key is the same as the current field mapping
                if (this._Container.use_relationships &&
                        this._Container.key.ToLower() == mapping.field_from.ToLower())
                {
                    spatialRecord.id = field.Value;
                }
            }

            spatialRecord.geometry = this._Container.geometry;
            spatialRecord.uid = MD5Encoder.GetMD5HashId(spatialRecord);

            return spatialRecord;
        }

        private GeoRecord CreateRecord(TrafficWiseFeature feature, SCFields list)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region Helpers

        private byte[] GetBytes(object pObj)
        {
            //  get a ref the blob mem stream
            IMemoryBlobStream2 pBlob = new MemoryBlobStreamClass();
            pBlob = (IMemoryBlobStream2)pObj;

            //  prepare a byte array to read
            int n = (int)pBlob.Size;
            byte[] pReadByte = new Byte[n];

            //  export the blob mem stream from COM -> CLR object
            object pObj2 = null;
            (pBlob as IMemoryBlobStreamVariant).ExportToVariant(out pObj2);

            //  cast the CLR object to byte array
            pReadByte = (byte[])pObj2;

            return pReadByte;
        }

        private static string GetOutputRetrievedFilePath()
        {
            string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string dir = System.IO.Path.GetDirectoryName(location);
            string outputFile = dir + "\\retrieved.xml";

            return outputFile;
        }

        private XDocument TransformData(string input)
        {
            //  Create stringreader wrapper
            var stringReader = new StringReader(input);
            //  Create an xmlreader wrapper
            var xmlReader = XmlReader.Create(stringReader);

            //  Create the transform object
            var xslt = new XslCompiledTransform();

            //  Load the stylesheet
            xslt.Load(this._Container.Config.app_path + "\\" + this._Container.name + "\\transform.xsl");

            //  Creat the stream to write the transformed document to
            var memoryStream = new MemoryStream();

            //  Create the writer in a memorystream
            var writer = new XmlTextWriter(memoryStream, Encoding.UTF8);

            //  Perform the transform
            xslt.Transform(xmlReader, null, writer);

            //  reposition the stream to the beginning and flush the data into the stream
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.Flush();

            //  create the reader
            var reader = new StreamReader(memoryStream);

            //  read the stream to the string
            string data = reader.ReadToEnd();

            if (!string.IsNullOrWhiteSpace(this._Container.temp_dir))
            {
                string savePath = System.IO.Path.GetDirectoryName(this._Container.temp_dir);

                DataUtil.SaveStringToFile(string.Format("{0}\\{1}", savePath, "transform-output.xml"), data);
            }

            //  return the data parsed into an XDocument Object
            return XDocument.Parse(data);
        }

        private NetworkCredential GetNetworkCredential()
        {
            return new NetworkCredential()
            {
                UserName = this._Container.source_username,
                Password = this._Container.source_password
            };
        }

        private WebEOCCredentials GetWebEOCCredentials()
        {
            return new WebEOCCredentials()
            {
                Incident = this._Container.Config.webeoc_incident,
                Password = this._Container.Config.webeoc_password,
                Position = this._Container.Config.webeoc_position,
                Username = this._Container.Config.webeoc_username
            };
        }

        #endregion
    }
}
