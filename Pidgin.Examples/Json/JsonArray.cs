using System.Collections.Immutable;
using System.Linq;

namespace Pidgin.Examples.Json
{
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
}