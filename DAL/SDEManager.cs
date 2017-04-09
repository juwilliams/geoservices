using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using gbc.BL;
using SpatialConnect.Entity;
using gbc.Interfaces;
using gbc.DAO;
using System.Linq;

namespace gbc.DAL
{
    public class SDEManager
    {
        #region Properties and Fields

        public string SdeObject { get; set; }

        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private SDE _sde { get; set; }
        private Config _Config;
        private Container _Container;

        #endregion

        public SDEManager(Container container)
        {
            this._Config = container.Config;
            this._Container = container;
        }

        public UpdateResult UpdateSDE(string licenseType, string sdeObject, string geometryType, int wkid, List<GeoRecord> records)
        {
            UpdateResult updateResult = new UpdateResult();

            try
            {
                _sde = new SDE(_Config, licenseType, sdeObject, geometryType, wkid);
                _sde.Connect(true);
                _sde.OpenSdeObject();

                updateResult.Affected = UpsertRecords(records);                

                _sde.Disconnect(true);

                foreach (GeoRecord record in updateResult.Affected)
                {
                    Key key =
                        this._Container.Cache.keys.FirstOrDefault(p => p.internal_id == record.objectid.ToString());

                    if (key == null)
                    {
                        key = new Key()
                        {
                            internal_id = record.objectid.ToString(),
                            external_id = record.GetKeyFieldValue(this._Container.key),
                            field_name = "sde_objectid"
                        };

                        this._Container.Cache.keys.Add(key);

                        _log.Debug("cache key added: " + key.internal_id);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                _log.Info("error encountered closing and saving sde edits!");
                _log.Error(ex.Message, ex);

                _sde.Disconnect(false);
            }

            return updateResult;
        }

        public ICursor GetCursor(string licenseType, string sourceObj, string whereClause)
        {
            _sde = new SDE(this._Config, licenseType, sourceObj, string.Empty, 0);
            _sde.Connect(false);

            ICursor cursor = _sde.GetSearchCursor(whereClause);

            _sde.Disconnect(false);

            return cursor;
        }

        public static IFeatureCursor GetFeatureCursorFromShapefile(string file, string tempDir, string queryFilterWhereClause = "")
        {
            return SDE.GetSearchFeatureCursorFromShapefile(file, tempDir, queryFilterWhereClause);
        }

        private List<GeoRecord> UpsertRecords(List<GeoRecord> records)
        {
            List<GeoRecord> affectedRecords = new List<GeoRecord>();
            List<GeoRecord> newRecords = records
                .Where(p => !this._Container.Cache.keys
                    .Any(x => x.external_id == p.GetKeyFieldValue(this._Container.key))).ToList();
            List<GeoRecord> updateRecords = records
                .Where(p => this._Container.Cache.keys
                    .Any(x => x.external_id == p.GetKeyFieldValue(this._Container.key))).ToList();

            foreach (var record in records)
            {
                if (record.objectid > -1)
                {
                    if (_sde.UpdateRecord(record))
                    {
                        affectedRecords.Add(record);
                    }       
                }
                else
                {
                    record.objectid = _sde.InsertRecord(record);

                    if (record.objectid > 0)
                    {
                        affectedRecords.Add(record);
                    }
                }
            }

            _log.Debug("affected records: " + affectedRecords.Count);
            return affectedRecords;
        }

        private List<string> DeleteRecords(IEnumerable<string> recordKeys)
        {
            var removedRecordKeys = new List<string>();

            foreach (var key in recordKeys)
            {
                if (_sde.DeleteRecord(key))
                {
                    removedRecordKeys.Add(key);
                }
            }

            return removedRecordKeys;
        }
    }
}
