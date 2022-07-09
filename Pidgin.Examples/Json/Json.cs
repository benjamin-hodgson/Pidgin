using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pidgin.Examples.Json;

[SuppressMessage("naming", "CA1724")]  // "The type name conflicts with the namespace name"
public abstract class Json
{
}

public class JsonArray : Json
{
    public ImmutableArray<Json> Elements { get; }
    public JsonArray(ImmutableArray<Json> elements)
    {
        Elements = elements;
    }
    public override string ToString()
        => $"[{string.Join(",", Elements.Select(e => e.ToString()))}]";
}

public class JsonObject : Json
{
    public IImmutableDictionary<string, Json> Members { get; }
    public JsonObject(IImmutableDictionary<string, Json> members)
    {
        Members = members;
    }
    public override string ToString()
        => $"{{{string.Join(",", Members.Select(kvp => $"\"{kvp.Key}\":{kvp.Value}"))}}}";
}

public class JsonString : Json
{
    public string Value { get; }
    public JsonString(string value)
    {
        Value = value;
    }

    public override string ToString()
        => $"\"{Value}\"";
}
