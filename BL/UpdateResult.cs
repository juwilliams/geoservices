using System.Collections.Generic;
using gbc.DAO;

namespace gbc.BL
{
    public class UpdateResult : IUpdateResult
    {
        public List<GeoRecord> Affected { get; set; }

        public List<string> Removed { get; set; }
    }
}
