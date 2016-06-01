using gbc.DAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gbc.Extensions
{
    public static class IGeoFieldExtensions
    {
        public static bool HasTag(this IGeoField field, string tag)
        {
            return field.Tags.ToLower().Split(',').Any(p => p == tag.ToLower());
        }
    }
}
