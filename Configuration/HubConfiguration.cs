using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Text.RegularExpressions;
using gbc.DAO;

namespace gbc.Configuration
{
    public class HubConfiguration : IHubConfiguration
    {
        #region Members

        public bool HasAttachments { get; set; }
        public bool TransformData { get; set; }
        public bool ShouldGeocode { get; set; }

        public string TransformPath { get; set; }
        public string DataSource { get; set; }
        public string DataSourceGeometry { get; set; }
        public string DataSourceUsername { get; set; }
        public string DataSourcePassword { get; set; }
        public string ResponseFormat { get; set; }
        public string FileInArchive { get; set; }
        public string OutputFile { get; set; }
        public string ResourceArchiveDir { get; set; }
        public string TempDir { get; set; }
        public string WhereClause { get; set; }

        [NonSerialized]
        [XmlIgnore]
        public string TranslationXml;

        [NonSerialized]
        [XmlIgnore]
        public List<IGeoField> CaptureFields;

        #endregion


        #region ArcGIS Members

        public bool IsSdeUpdate { get; set; }

        public int Wkid { get; set; }

        public string SDEDestination { get; set; }
        public string SDESource { get; set; }
        public string SDEUsername { get; set; }
        public string SDEPassword { get; set; }
        public string SDEServer { get; set; }
        public string SDEDatabase { get; set; }
        public string SDEInstance { get; set; }
        public string SDEVersion { get; set; }
        public string SDEKeyword { get; set; }

        #endregion


        #region WebEOC Members

        public bool IsWebEocUpdate { get; set; }
        public bool UseFilteredWebEOCBoardRequest { get; set; }

        public string MaxAttachments { get; set; }

        public string WebEOCUsername { get; set; }
        public string WebEOCPassword { get; set; }
        public string WebEOCBoard { get; set; }
        public string WebEOCView { get; set; }
        public string WebEOCJurisdiction { get; set; }
        public string WebEOCPosition { get; set; }
        public string WebEOCIncident { get; set; }
        public string WebEOCViewFilter { get; set; }

        #endregion


        #region Deserialization

        public List<IGeoField> GetCaptureFields()
        {
            return CaptureFields;
        }

        public string GetTranslationXml()
        {
            return TranslationXml;
        }

        public static HubConfiguration Load(string serializedConfig)
        {
            Regex regex = new Regex(@"\\r\\n|\\r\\n");

            serializedConfig = regex.Replace(serializedConfig, "");

            XmlSerializer serializer = new XmlSerializer(typeof(HubConfiguration));
            StringReader stringReader = new StringReader(serializedConfig);

            return serializer.Deserialize(stringReader) as HubConfiguration;
        }

        #endregion


        #region Ctor

        public HubConfiguration()
        {
            this.CaptureFields = new List<IGeoField>();
        }

        #endregion
    }
}
