using System.Collections.Immutable;
using System.Linq;

namespace Pidgin.Examples.Json
{
    public class JsonObject : IJson
    {
        public IImmutableDictionary<string, IJson> Members { get; }
        public JsonObject(IImmutableDictionary<string, IJson> members)
        {
            Members = members;
        }
        public override string ToString()
            => $"{{{string.Join(",", Members.Select(kvp => $"\"{kvp.Key}\":{kvp.Value.ToString()}"))}}}";
    }
}