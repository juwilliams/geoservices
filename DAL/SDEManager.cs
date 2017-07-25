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
                if (records.Count < 1)
                {
                    return updateResult;
                }

                _sde = new SDE(_Config, licenseType, sdeObject, geometryType, wkid);
                _sde.Connect(true);
                _sde.OpenSdeObject();

                updateResult.Affected = UpsertRecords(records);                

                _sde.Disconnect(true);
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

            foreach (GeoRecord record in records)
            {
                if (record.objectid > 0)
                {
                    bool wasUpdated = _sde.UpdateRecord(record);

                    if (wasUpdated)
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

        private List<string> DeleteRecords(IEnumerable<string> recordKeys, string keyField)
        {
            var removedRecordKeys = new List<string>();

            foreach (var key in recordKeys)
            {
                if (_sde.DeleteRecord(keyField, key))
                {
                    removedRecordKeys.Add(key);
                }
            }

            return removedRecordKeys;
        }
    }
}
