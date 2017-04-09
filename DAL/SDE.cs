using ESRI.ArcGIS;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using gbc.Configuration;
using gbc.Constants;
using gbc.Interfaces;
using gbc.Util;
using log4net;
using SpatialConnect.Entity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Runtime.InteropServices;

namespace gbc.DAL
{
    public class SDE
    {
        #region Properties and Fields

        private static readonly log4net.ILog _log = LogManager.GetLogger(typeof(SDE));

        public string endpoint { get; set; }

        private Config _Config;
        private static string _licenseType;
        private static LicenseInitializer m_license;
        private static bool m_licenseCheckedOut;
        private SDEUtil _SdeUtil;
        private DataUtil _DataUtil;
        private IWorkspace m_workspace;
        private IWorkspaceFactory m_workspaceFactory;
        private IWorkspaceEdit m_editSession;
        private IFeatureWorkspace m_featureWorkspace;
        private IFeatureClass m_featureClass;
        private ICursor m_insertCursor;
        private IFeatureCursor m_insertFeatureCursor;
        private ITable m_table;
        private string _sdeObject { get; set; }
        private string _geometryType { get; set; }
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static List<string> _existingRecordIds;
        private SqlConnection _sqlConnection;

        #endregion


        #region Ctor

        public SDE(Config config, string licenseType, string sdeObject, string geometryType, Int64 wkid)
        {
            log4net.LogicalThreadContext.Properties["MessageTarget"] = sdeObject;

            _existingRecordIds = new List<string>();

            m_licenseCheckedOut = false;
            _DataUtil = new DataUtil();
            _SdeUtil = new SDEUtil(wkid);

            _Config = config;
            _licenseType = licenseType;
            _sdeObject = sdeObject;
            _geometryType = geometryType;

            _log.Debug("sde instance initialized..");
        }

        #endregion


        #region License Management

        public static void BindLicense(string licenseType)
        {
            if (ConfigurationManager.AppSettings["env"].ToString() == "dev")
            {
                RuntimeManager.BindLicense(ProductCode.EngineOrDesktop);

                m_licenseCheckedOut = true;

                _log.Info("arcgis dev license checked out!");

                return;
            }

            //	ensure the license initializer is instanced
            if (m_license == null)
            {
                m_license = new LicenseInitializer();
            }

            _log.Info("attempting to checkout sde license [" + licenseType + "]");

            //	perform a checkout if license is not checked out
            if (!m_licenseCheckedOut)
            {
                switch (licenseType)
                {
                    default:
                    case "DESKTOP_BASIC":
                        {
                            //	initialize the license
                            m_license.InitializeApplication(new ESRI.ArcGIS.esriSystem.esriLicenseProductCode[] {
                                    esriLicenseProductCode.esriLicenseProductCodeBasic },
                                    new esriLicenseExtensionCode[] { });
                            break;
                        }
                    case "DESKTOP_STANDARD":
                        {
                            //	initialize the license
                            m_license.InitializeApplication(new ESRI.ArcGIS.esriSystem.esriLicenseProductCode[] {
                                    esriLicenseProductCode.esriLicenseProductCodeStandard },
                                    new esriLicenseExtensionCode[] { });
                            break;
                        }
                    case "DESKTOP_ADVANCED":
                        {
                            //	initialize the license
                            m_license.InitializeApplication(new ESRI.ArcGIS.esriSystem.esriLicenseProductCode[] {
                                    esriLicenseProductCode.esriLicenseProductCodeAdvanced },
                                    new esriLicenseExtensionCode[] { });

                            break;
                        }

                }
                //	set the license checkout property to true for checks when attempting to perform inserts
                m_licenseCheckedOut = true;

                _log.Info("arcgis production license checked out!");

                return;
            }

            _log.Info("arcgis license already checked out!");
        }

        public static void CheckInLicense()
        {
            if (m_licenseCheckedOut)
            {
                m_license.ShutdownApplication();
            }
        }

        #endregion


        #region Connection Management

        public void Connect(bool withEdits)
        {
            BindLicense(_licenseType);

            //	open the workspace
            m_workspaceFactory = (IWorkspaceFactory2)new SdeWorkspaceFactory();
            m_workspace = m_workspaceFactory.Open(_DataUtil.GetPropertySet(_Config), 0);

            if (withEdits)
            {
                //	start the edit session on the workspace
                m_editSession = (IWorkspaceEdit2)m_workspace;
                m_editSession.StartEditing(false);
                m_editSession.StartEditOperation();
            }

            Log.Info("SDE Connection opened");
        }

        public void Disconnect(bool saveEdits)
        {
            if (m_editSession != null && m_editSession.IsBeingEdited())
            {
                //	stop the edit operation, save edits and stop editing
                m_editSession.StopEditOperation();

                //	stop editing
                try
                {
                    m_editSession.StopEditing(saveEdits);
                }
                catch
                {
                    var versionEdit = (IVersionEdit)m_editSession;
                    versionEdit.Reconcile(_Config.arcgis_version);
                    m_editSession.StopEditing(saveEdits);
                }
            }

            CheckInLicense();

            ReleaseCOMLocks();

            Log.Info("SDE Connection closed");
        }

        #endregion


        #region Sde Helpers

        public int GetNextObjectID(string schema = "dbo")
        {
            int nextObjectId = -1;

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings[ConfigConstants.ConnectionStrings.MSSQLSDE].ConnectionString))
            {
                conn.Open();

                SqlCommand command = new SqlCommand(string.Format(SDESqlConstants.QueryFormat.SELECT_NEXT_OBJECTID_FOR_TABLE, schema, this._sdeObject), conn);

                SqlDataReader reader = command.ExecuteReader();

                while(reader.Read())
                {
                    nextObjectId = int.Parse(reader[SDESqlConstants.QueryReturns.NEXT_OBJECTID].ToString());
                }

                conn.Close();
            }

            return nextObjectId;
        }

        private void ReleaseCOMLocks()
        {
            m_table = null;
            m_featureClass = null;
            m_insertCursor = null;
            m_insertFeatureCursor = null;
            m_workspaceFactory = null;
            m_workspace = null;
            m_editSession = null;
        }

        public void OpenSdeObject()
        {
            if (_geometryType.ToLower() == ApplicationConstants.SDEGeometry.Table)
            {
                OpenTable();
            }
            else
            {
                OpenFeatureClass();
            }
        }

        public void CloseSdeObject()
        {
            if (m_insertCursor != null)
            {
                try
                {
                    m_insertCursor.Flush();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, ex);
                }
                finally
                {
                    Marshal.ReleaseComObject(m_insertCursor);
                }
            }

            if (m_insertFeatureCursor != null)
            {
                try
                {
                    m_insertFeatureCursor.Flush();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, ex);
                }
                finally
                {
                    Marshal.ReleaseComObject(m_insertFeatureCursor);
                }
            }
        }

        private void OpenFeatureClass()
        {
            try   //	open an existing featureclass
            {
                m_featureWorkspace = (IFeatureWorkspace)m_workspace;
                m_featureClass = m_featureWorkspace.OpenFeatureClass(_sdeObject);
                
                Log.Info(string.Format("{0} [{1}]", "Feature class opened", _sdeObject));
            }
            catch   //  create a new feature class
            {
                m_featureClass = _SdeUtil.CreateFeatureClass(
                    m_featureWorkspace,
                    _sdeObject,
                    _SdeUtil.CreateRequiredFields(this._geometryType),
                    _geometryType,
                    _Config.arcgis_keyword
                );

                Log.Info(string.Format("{0} [{1}]", "Feature class created", _sdeObject));
            }
        }

        private void OpenTable()
        {
            try   //    open an existing table
            {
                m_featureWorkspace = (IFeatureWorkspace)m_workspace;
                m_table = m_featureWorkspace.OpenTable(_sdeObject);
                
                Log.Info(string.Format("{0} [{1}]", "Table opened", _sdeObject));
            }
            catch   //  create a new table
            {
                m_table = _SdeUtil.CreateTable(
                    m_featureWorkspace,
                    this._sdeObject,
                    _SdeUtil.CreateRequiredFields(this._geometryType),
                    _Config.arcgis_keyword
                );

                Log.Info(string.Format("{0} [{1}]", "Table created", _sdeObject));
            }

            m_insertCursor = GetInsertCursor();
        }

        /// <summary>
        /// Attempts to open the table from a featureclass if it exists, if that fails, from a table directly or returns
        /// a null reference if neither of those attempts are successful
        /// </summary>
        /// <returns>ITable</returns>
        public ICursor GetSearchCursor(string queryFilterWhereClause = "")
        {
            try
            {
                IQueryFilter queryFilter = null;

                m_featureWorkspace = (IFeatureWorkspace)m_workspace;
                m_featureClass = m_featureWorkspace.OpenFeatureClass(_sdeObject);
                
                if (!string.IsNullOrEmpty(queryFilterWhereClause))
                {
                    string convertedQueryFilterWhereClause = queryFilterWhereClause.Replace("\\\"", "'");

                    _log.Debug("Querying with WhereClause: " + convertedQueryFilterWhereClause);

                    queryFilter = new QueryFilter();
                    queryFilter.WhereClause = convertedQueryFilterWhereClause;
                }

                return queryFilter == null
                    ? (ICursor)m_featureClass.Search(null, true)
                    : (ICursor)m_featureClass.Search(queryFilter, true);
            }
            catch
            {
                m_table = m_featureWorkspace.OpenTable(_sdeObject);

                return m_table.Search(null, true);
            }
        }

        public static IFeatureCursor GetSearchFeatureCursorFromShapefile(string file, string tempDir, string queryFilterWhereClause)
        {
            try
            {
                IQueryFilter queryFilter = null;
                IWorkspaceFactory workspaceFactoryShape = new ESRI.ArcGIS.DataSourcesFile.ShapefileWorkspaceFactoryClass(); 
                IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspaceFactoryShape.OpenFromFile(tempDir, 0);
                IFeatureClass featureClass = 
                    featureWorkspace.OpenFeatureClass(System.IO.Path.GetFileNameWithoutExtension(file));

                if (!string.IsNullOrEmpty(queryFilterWhereClause))
                {
                    string convertedQueryFilterWhereClause = queryFilterWhereClause.Replace("\\\"", "'");

                    _log.Debug("Querying with WhereClause: " + convertedQueryFilterWhereClause);

                    queryFilter = new QueryFilter();
                    queryFilter.WhereClause = convertedQueryFilterWhereClause;
                }

                return queryFilter == null
                    ? featureClass.Search(null, true)
                    : featureClass.Search(queryFilter, true);
            }
            catch (Exception ex)
            {
                Log.Error(ExceptionConstants.Messages.SDE.COULD_NOT_GET_SEARCH_CURSOR_FOR_SHAPEFILE, ex);
                
                return null;
            }
        }

        private ICursor GetInsertCursor()
        {
            if (m_table == null)
            {
                OpenTable();
            }

            return m_table.Insert(true);
        }

        private IFeatureCursor GetInsertFeatureCursor()
        {
            if (m_featureClass == null)
            {
                OpenFeatureClass();
            }

            return m_featureClass.Insert(true);
        }

        private string GetSqlGeometryFormat(string geometry)
        {
            switch (geometry.ToLower())
            {
                default:
                case ApplicationConstants.SDEGeometry.Kml_Point:
                case ApplicationConstants.SDEGeometry.Point:
                    {
                        return SDESqlConstants.STGeometryFormat.POINT;
                    }
                case ApplicationConstants.SDEGeometry.Kml_PolyLine:
                case ApplicationConstants.SDEGeometry.PolyLine:
                case ApplicationConstants.SDEGeometry.Line:
                    {
                        return SDESqlConstants.STGeometryFormat.LINE;
                    }
                case ApplicationConstants.SDEGeometry.Polygon:
                case ApplicationConstants.SDEGeometry.Kml_Polygon:
                    {
                        return SDESqlConstants.STGeometryFormat.POLYGON;
                    }
            }
        }

        #endregion


        #region ArcObjects Record Manipulation

        public bool UpdateRecord(IGeoRecord record)
        {
            if (!m_editSession.IsBeingEdited())
            {
                m_editSession.StartEditing(true);
                m_editSession.StartEditOperation();
            }

            try
            {
                if (record.geometry.ToLower() == ApplicationConstants.SDEGeometry.Table)
                {
                    return _SdeUtil.UpdateRow(m_table, record.fields, record.objectid);
                }
                else
                {
                    _SdeUtil.AddMissingFields(record.fields, m_featureClass);

                    return _SdeUtil.UpdateFeature(m_featureClass, record.fields, this._geometryType, record.objectid);
                }
            }
            catch (Exception ex) // TODO: log this exception
            {
                Log.Error(ex.Message, ex);

                return false;
            }
        }

        public int InsertRecord(IGeoRecord record)
        {
            if (!m_editSession.IsBeingEdited())
            {
                m_editSession.StartEditing(true);
                m_editSession.StartEditOperation();
            }
            
            try
            {
                if (record.geometry.ToLower() == ApplicationConstants.SDEGeometry.Table)
                {
                    return _SdeUtil.InsertRow(m_table, record.fields, record.uid);
                }
                else 
                {
                    _SdeUtil.AddMissingFields(record.fields, m_featureClass);

                    return _SdeUtil.InsertFeature(m_featureClass, record.fields, this._geometryType, record.uid);
                }
            }
            catch (Exception ex) // TODO: log this exception
            {
                Log.Error(ex.Message, ex);

                return -1;
            }
        }
        
        public bool DeleteRecord(string recordUniqueKey)
        {
            //	ensure that an edit operation is in effect
            if (!m_editSession.IsBeingEdited())
            {
                m_editSession.StartEditing(true);
                m_editSession.StartEditOperation();
            }

            //	open the featureclass if it isnt already open
            if (_geometryType.ToLower() == ApplicationConstants.SDEGeometry.Table)
            {
                return _SdeUtil.DeleteRow(m_table, recordUniqueKey);
            }
            else
            {
                return _SdeUtil.DeleteFeature(m_featureClass, recordUniqueKey);
            }
        }

        public void DeleteNullRecords(string geometry, string sdeObject)
        {
            if (m_table != null)
            {
                _SdeUtil.DeleteNullRows(m_table, sdeObject);
                return;
            }

            _SdeUtil.DeleteNullFeatures(m_featureClass);
        }

        #endregion


        #region Sql Record Manipulation

        public bool SqlInsertRecord(IGeoRecord record)
        {
            return false;
        }

        public bool SqlUpdateRecord(IGeoRecord record)
        {
            return false;
        }

        public bool SqlDeleteRecord(IGeoRecord record)
        {
            return false;
        }

        #endregion
    }
}
