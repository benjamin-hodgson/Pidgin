using System.Collections.Generic;
using System.Collections.Immutable;

using Pidgin.Examples.Json;

using Superpower;
using Superpower.Parsers;

namespace Pidgin.Bench.SuperpowerParsers;

// adapted from my Pidgin json parser example
public static class SuperpowerJsonParser
{
    private static TextParser<T> Between<T, U, V>(this TextParser<T> p, TextParser<U> before, TextParser<V> after)
        => before.IgnoreThen(p).Then(x => after.Value(x));

    private static readonly TextParser<char> _lBrace = Character.EqualTo('{');
    private static readonly TextParser<char> _rBrace = Character.EqualTo('}');
    private static readonly TextParser<char> _lBracket = Character.EqualTo('[');
    private static readonly TextParser<char> _rBracket = Character.EqualTo(']');
    private static readonly TextParser<char> _quote = Character.EqualTo('"');
    private static readonly TextParser<char> _colon = Character.EqualTo(':');
    private static readonly TextParser<char> _colonWhitespace =
        _colon.Between(Character.WhiteSpace.Many(), Character.WhiteSpace.Many());
    private static readonly TextParser<char> _comma = Character.EqualTo(',');

    private static readonly TextParser<string> _string =
        Character.Matching(c => c != '"', "char except quote")
            .Many()
            .Between(_quote, _quote)
            .Select(string.Concat);
    private static readonly TextParser<Json> _jsonString =
        _string.Select(s => (Json)new JsonString(s));

    private static readonly TextParser<Json> _json =
        _jsonString.Or(Superpower.Parse.Ref(() => _jsonArray)).Or(Superpower.Parse.Ref(() => _jsonObject));

    private static readonly TextParser<Json> _jsonArray =
        _json.Between(Character.WhiteSpace.Many(), Character.WhiteSpace.Many())
            .ManyDelimitedBy(_comma)
            .Between(_lBracket, _rBracket)
            .Select(els => (Json)new JsonArray(els.ToImmutableArray()));

    private static readonly TextParser<KeyValuePair<string, Json>> _jsonMember =
        from name in _string.SelectMany(_ => _colonWhitespace, (name, ws) => name)  // avoid allocating a transparent identifier for a result we don't care about
        from val in _json
        select new KeyValuePair<string, Json>(name, val);

    private static readonly TextParser<Json> _jsonObject =
        _jsonMember.Between(Character.WhiteSpace.Many(), Character.WhiteSpace.Many())
            .ManyDelimitedBy(_comma)
            .Between(_lBrace, _rBrace)
            .Select(kvps => (Json)new JsonObject(kvps.ToImmutableDictionary()));

    public static Json Parse(string input) => _json.Parse(input);
}
