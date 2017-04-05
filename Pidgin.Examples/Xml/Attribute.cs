using System;

namespace Pidgin.Examples.Xml
{
    public class Attribute : IEquatable<Attribute>
    {
        public string Name { get; }
        public string Value { get; }

        public Attribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((Attribute)obj);
        }
        public bool Equals(Attribute other)
            => Name == other.Name
            && Value == other.Value;
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Name.GetHashCode();
                hash = hash * 23 + Value.GetHashCode();
                return hash;
            }
        }
    }
}