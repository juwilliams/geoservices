using gbc.DAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace gbc.Configuration
{
    public interface IHubConfiguration
    {
        #region Members

        bool HasAttachments { get; }
        bool TransformData { get; }
        bool IsSdeUpdate { get; }
        bool IsWebEocUpdate { get; }
        bool ShouldGeocode { get; }

        string TransformPath { get; }
        string DataSource { get; }
        string DataSourceGeometry { get; }
        string DataSourceUsername { get; }
        string DataSourcePassword { get; }
        string ResponseFormat { get; }
        string FileInArchive { get; }
        string OutputFile { get; }
        string ResourceArchiveDir { get; }
        string TempDir { get; }
        string WhereClause { get; }

        string GetTranslationXml();
        List<IGeoField> GetCaptureFields();

        #endregion


        #region SDE Members

        string SDEDestination { get; }
        string SDESource { get; }
        string SDEUsername { get; }
        string SDEPassword { get; }
        string SDEServer { get; }
        string SDEDatabase { get; }
        string SDEInstance { get; }
        string SDEVersion { get; }
        string SDEKeyword { get; }
        int Wkid { get; }

        #endregion


        #region WebEOC Members

        bool UseFilteredWebEOCBoardRequest { get; }
        string MaxAttachments { get; }
        string WebEOCViewFilter { get; }
        string WebEOCUsername { get; }
        string WebEOCPassword { get; }
        string WebEOCBoard { get; }
        string WebEOCView { get; }
        string WebEOCJurisdiction { get; }
        string WebEOCPosition { get; }
        string WebEOCIncident { get; }

        #endregion
    }
}
