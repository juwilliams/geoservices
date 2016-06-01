using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.DAO
{
    public interface IGeoField
    {
        string Name { get; set;}
        string ToName { get; set; }
        string DataType { get; set; }
        string Length { get; set; }
        string Value { get; set; }
        string Tags { get; set; }

        Byte[] Data { get; set; }

        string GetName();
    }
}
