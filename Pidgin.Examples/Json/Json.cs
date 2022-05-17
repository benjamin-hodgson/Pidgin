using System.Collections.Immutable;
using System.Linq;

namespace Pidgin.Examples.Json
{
    public interface IJson
    {
    }

    public class JsonArray : IJson
    {
        public ImmutableArray<IJson> Elements { get; }
        public JsonArray(ImmutableArray<IJson> elements)
        {
            Elements = elements;
        }
        public override string ToString()
            => $"[{string.Join(",", Elements.Select(e => e.ToString()))}]";
    }

    public class JsonObject : IJson
    {
        public IImmutableDictionary<string, IJson> Members { get; }
        public JsonObject(IImmutableDictionary<string, IJson> members)
        {
            Members = members;
        }
        public override string ToString()
            => $"{{{string.Join(",", Members.Select(kvp => $"\"{kvp.Key}\":{kvp.Value}"))}}}";
    }

    public class JsonString : IJson
    {
        public string Value { get; }
        public JsonString(string value)
        {
            Value = value;
        }

        public override string ToString()
            => $"\"{Value}\"";
    }
}
