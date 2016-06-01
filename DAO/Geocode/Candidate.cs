using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace gbc.DAO.Geocode
{
    public class Candidate
    {
        public string address { get; set; }
        public Location location { get; set; }
        public double score { get; set; }
    }
}
