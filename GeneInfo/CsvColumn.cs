using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public class CsvColumn
    {
        public string? Name { get; set; }
        public CsvType Type { get; set; }

        public CsvColumn(CsvType type)
        {
            Type = type;
        }

        public CsvColumn(string? name, CsvType type)
        {
            Name = name;
            Type = type;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null && this != null) return false;
            if (this == null && obj != null) return false;
            if (this == null && obj == null) return true;

            if (obj is CsvColumn other) return Equals(other);
            else return base.Equals(obj);
        }

        public bool Equals(CsvColumn? obj)
        {
            if (obj == null && this != null) return false;
            if (this == null && obj != null) return false;
            if (this == null && obj == null) return true;

            return (Name?.Equals(obj!.Name) ?? Name == null && obj!.Name == null) && Type.Equals(obj.Type);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Type);
        }
    }
}
