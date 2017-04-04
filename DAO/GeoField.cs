using SpatialConnect.Entity;
using System;

namespace gbc.DAO
{
    public class GeoField : IGeoField
    {
        public string Name { get; set; }
        public string ToName { get; set; }
        public string DataType { get; set; }
        public string Length { get; set;}
        public string Value { get; set; }
        public string Tags { get; set; }
        public Byte[] Data { get; set; }
        public bool IsKey { get; set; }

        public GeoField()
        {

        }

        public GeoField(Mapping mapping)
        {
            this.Name = mapping.field_from;
            this.ToName = mapping.field_to;
            this.DataType = mapping.type;
            this.Length = mapping.length;
            this.Value = string.Empty;
            this.Tags = mapping.tags;
        }

        public GeoField (IGeoField field)
        {
            this.Name = field.Name;
            this.ToName = field.ToName;
            this.DataType = field.DataType;
            this.Length = field.Length;
            this.Value = field.Value;
            this.Tags = field.Tags;

            this.Data = field.Data;
        }

        public string GetName()
        {
            return this.ToName ?? this.Name;
        }

        public bool HasTag(string tagName)
        {
            throw new NotImplementedException();
        }

        public void AddAfterSaveAction(Action a)
        {
            throw new NotImplementedException();
        }
    }
}
