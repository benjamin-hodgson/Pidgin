using System;
using System.Collections.Generic;
using System.Linq;

namespace Pidgin.Examples.Xml
{
    public class Tag : IEquatable<Tag>
    {
        public string Name { get; }
        public IEnumerable<Attribute> Attributes { get; }
        public IEnumerable<Tag>? Content { get; }

        public Tag(string name, IEnumerable<Attribute> attributes, IEnumerable<Tag>? content)
        {
            Name = name;
            Attributes = attributes;
            Content = content;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((Tag)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + Name.GetHashCode();
                hash = hash * 23 + Attributes.GetHashCode();
                hash = hash * 23 + Content?.GetHashCode() ?? 0;
                return hash;
            }
        }

        public bool Equals(Tag? other)
            => Name == other?.Name
            && Attributes.SequenceEqual(other.Attributes)
            && ((Content is null && other.Content is null) || Content!.SequenceEqual(other.Content!));
    }

    public class Attribute : IEquatable<Attribute>
    {
        public string Name { get; }
        public string Value { get; }

        public Attribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((Attribute)obj);
        }
        public bool Equals(Attribute? other)
            => Name == other?.Name
            && Value == other.Value;
        public override int GetHashCode() => HashCode.Combine(Name, Value);
    }
}
