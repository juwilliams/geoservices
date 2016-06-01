using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.DAO.Geocode
{
    public class GeocodeResult
    {
        public SpatialReference spatialReference { get; set; }
        public List<Candidate> candidates { get; set; }
    }
}
