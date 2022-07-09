using System.Collections.Generic;
using System.Collections.Immutable;

using Pidgin.Examples.Json;

using Sprache;

using static Sprache.Parse;

namespace Pidgin.Bench.SpracheParsers;

// adapted from my Pidgin json parser example
public static class SpracheJsonParser
{
    private static readonly Sprache.Parser<char> _lBrace = Char('{');
    private static readonly Sprache.Parser<char> _rBrace = Char('}');
    private static readonly Sprache.Parser<char> _lBracket = Char('[');
    private static readonly Sprache.Parser<char> _rBracket = Char(']');
    private static readonly Sprache.Parser<char> _quote = Char('"');
    private static readonly Sprache.Parser<char> _colon = Char(':');
    private static readonly Sprache.Parser<char> _colonWhitespace =
        _colon.Contained(WhiteSpace.Many(), WhiteSpace.Many());
    private static readonly Sprache.Parser<char> _comma = Char(',');

    private static readonly Sprache.Parser<string> _string =
        Char(c => c != '"', "char except quote")
            .Many()
            .Contained(_quote, _quote)
            .Select(string.Concat);
    private static readonly Sprache.Parser<Json> _jsonString =
        _string.Select(s => new JsonString(s));

    private static readonly Sprache.Parser<Json> _json =
        _jsonString.Or(Ref(() => _jsonArray)).Or(Ref(() => _jsonObject));

    private static readonly Sprache.Parser<Json> _jsonArray =
        _json.Contained(WhiteSpace.Many(), WhiteSpace.Many())
            .DelimitedBy(_comma)
            .Contained(_lBracket, _rBracket)
            .Select(els => new JsonArray(els.ToImmutableArray()));

    private static readonly Sprache.Parser<KeyValuePair<string, Json>> _jsonMember =
        from name in _string.SelectMany(_ => _colonWhitespace, (name, ws) => name)  // avoid allocating a transparent identifier for a result we don't care about
        from val in _json
        select new KeyValuePair<string, Json>(name, val);

    private static readonly Sprache.Parser<Json> _jsonObject =
        _jsonMember.Contained(WhiteSpace.Many(), WhiteSpace.Many())
            .DelimitedBy(_comma)
            .Contained(_lBrace, _rBrace)
            .Select(kvps => new JsonObject(kvps.ToImmutableDictionary()));

    public static IResult<Json> Parse(string input) => _json(new Input(input));
}
