using gbc.DAO;
using System.Collections.Generic;

namespace gbc.BL
{
    public interface IUpdateResult
    {
        List<GeoRecord> Affected { get; set; }
        List<string> Removed { get; set; }
    }
}
