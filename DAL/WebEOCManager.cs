using gbc.BL;
using gbc.Constants;
using gbc.DAO;
using gbc.Interfaces;
using gbc.Util;
using gbc.WebEOC7;
using SpatialConnect.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Services.Protocols;
using System.Xml.Linq;

using Container = SpatialConnect.Entity.Container;

namespace gbc.DAL
{
    public class WebEOCManager
    {
        #region private properties

        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Config _Config;
        private Container _Container;
        private WebEOC7.APISoapClient api { get; set; }
        private WebEOCCredentials credentials { get; set; }
        private APISoapClient api7 { get; set; }
        
        #endregion


        public WebEOCManager(Container container)
        {
            _Config = container.Config;
            _Container = container;

            Initialize();
        }

        private void Initialize()
        {
            this.api = new WebEOC7.APISoapClient();
            this.api7 = new APISoapClient();

            SetCredentials();
        }

        public UpdateResult UpdateWebEOC(string board, string inputView, IEnumerable<IGeoRecord> newRecords, bool hasAttachments = false, BackgroundWorker worker = null)
        {
            UpdateResult updateResult = new UpdateResult();
            List<GeoRecord> addedRecordKeys = new List<GeoRecord>();

            _log.Debug("webeoc board update in progress..");

            try
            {
                foreach (var record in newRecords)
                {
                    if (worker != null && worker.CancellationPending)
                    {
                        updateResult.Affected = addedRecordKeys;

                        return updateResult;
                    }

                    if (hasAttachments)
                    {
                        if (AddAttachment(board, inputView, record))
                        {
                            addedRecordKeys.Add((GeoRecord)record);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (record.dataid == 0 && AddData(board, inputView, record))
                    {
                        addedRecordKeys.Add((GeoRecord)record);
                    }
                    else
                    {
                        if (UpdateData(board, inputView, record))
                        {
                            addedRecordKeys.Add((GeoRecord)record);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);
            }
            finally
            {
                updateResult.Affected = addedRecordKeys;
            }

            return updateResult;
        }

        private bool AddAttachment(string board, string inputView, IGeoRecord record)
        {
            try
            {
                _log.Debug("webeoc record add attachment..");

                WebEOC7.WebEOCCredentials credentials = new WebEOC7.WebEOCCredentials()
                {
                    Incident = this.credentials.Incident,
                    Password = this.credentials.Password,
                    Username = this.credentials.Username,
                    Position = this.credentials.Position
                };

                if (record.dataid == 0)
                {
                    return false;
                }

                string fieldName = "attachments";

                //  find a relationship for this data id and an attachment field
                Key key =
                    this._Container.Cache.keys.FirstOrDefault(p => p.internal_id == record.dataid.ToString());

                if (key == null)
                {
                    key = new Key()
                    {
                        internal_id = record.dataid.ToString()
                    };

                    this._Container.Cache.keys.Add(key);

                    _log.Debug("cache key added: " + key.internal_id);
                }

                key.external_id = key.external_id <= 19 ? key.external_id + 1 : 1;
                key.field_name = fieldName;

                _log.Debug("cache key updated: external id=" + key.external_id + "; field_name=" + key.field_name);

                fieldName = fieldName + key.external_id.ToString();

                string attachmentName =
                    record.fields.First(x => x.ToName.ToLower() == GeoFieldConstants.Tags.ATTACHMENT_NAME).Value;

                byte[] attachmentData =
                    record.fields.First(x => x.ToName.ToLower() == GeoFieldConstants.Tags.BINARY_DATA).Data;

                api7.AddAttachment(credentials, board, inputView, fieldName, record.dataid, attachmentData, attachmentName);

                _log.Debug("added attachment to: " + fieldName + ", filename: " + attachmentName);

                return true;
            }
            catch (SoapException ex)
            {
                _log.Error("SOAP EXCEPTION: " + ex.Message, ex);

                return false;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);

                return false;
            }
        }

        private bool AddData(string board, string inputView, IGeoRecord record)
        {
            try
            {
                var xmlPayload = GetXmlPayload(record);
                if (string.IsNullOrEmpty(xmlPayload))
                {
                    return false;
                }

                APISoapClient api7 = new APISoapClient();
                WebEOC7.WebEOCCredentials webEoc7Credentials = new WebEOC7.WebEOCCredentials()
                {
                    Incident = credentials.Incident,
                    Password = credentials.Password,
                    Position = credentials.Position,
                    Username = credentials.Username
                };

                record.dataid = api7.AddData(webEoc7Credentials, board, inputView, xmlPayload);

                return true;
            }
            catch (SoapException ex) // TODO: log this error
            {
                _log.Error(ex.Message, ex);

                return false;
            }
        }

        private bool UpdateData(string board, string inputView, IGeoRecord record)
        {
            try
            {
                var xmlPayload = GetXmlPayload(record, false);

                //_log.Info("webeoc board update record..\r\n" + xmlPayload);

                api.UpdateData(this.credentials, board, inputView, xmlPayload, (int)record.dataid);

                //_log.Info("webeoc board updated record!");

                return true;
            }
            catch (SoapException ex)
            {
                _log.Error(ex.Message, ex);

                return false;
            }
        }

        private string GetXmlPayload(IGeoRecord record, bool includePostedDate = true)
        {
            try
            {
                var data = new XElement("data");

                if (includePostedDate)
                {
                    data.Add(new XElement("posted_date", DataUtil.GetWebEOCDateTime()));
                }

                foreach (var field in record.fields)
                {
                    if (string.IsNullOrEmpty(field.GetName()))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(field.Value))
                    {
                        field.Value = "";
                    }

                    field.Value = field.Value.Replace(",", "");

                    data.Add(new XElement(field.GetName(), (field.DataType.ToLower() == "short" || field.DataType.ToLower() == "long") && string.IsNullOrEmpty(field.Value) ? "0" : field.Value));
                }

                return data.ToString();
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);

                return string.Empty;
            }
        }

        private void SetCredentials()
        {
            this.credentials = new WebEOCCredentials();
            this.credentials.Incident = _Config.webeoc_incident;
            this.credentials.Password = _Config.webeoc_password;
            this.credentials.Position = _Config.webeoc_position;
            this.credentials.Username = _Config.webeoc_username;
        }
    }
}
