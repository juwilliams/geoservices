using gbc.DAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.Interfaces
{
    public interface IGeoRecord
    {
        int dataid { get; set; }

        string uid { get; set; }
        string geometry { get; set; }

        List<GeoField> fields { get; }
    }
}
