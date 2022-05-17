using System.Collections.Generic;
using System.Collections.Immutable;

using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Examples.Json
{
    public static class JsonParser
    {
        private static readonly Parser<char, char> _lBrace = Char('{');
        private static readonly Parser<char, char> _rBrace = Char('}');
        private static readonly Parser<char, char> _lBracket = Char('[');
        private static readonly Parser<char, char> _rBracket = Char(']');
        private static readonly Parser<char, char> _quote = Char('"');
        private static readonly Parser<char, char> _colon = Char(':');
        private static readonly Parser<char, char> _colonWhitespace =
            _colon.Between(SkipWhitespaces);
        private static readonly Parser<char, char> _comma = Char(',');

        private static readonly Parser<char, string> _string =
            Token(c => c != '"')
                .ManyString()
                .Between(_quote);
        private static readonly Parser<char, Json> _jsonString =
            _string.Select<Json>(s => new JsonString(s));

        private static readonly Parser<char, Json> _json =
            _jsonString.Or(Rec(() => _jsonArray!)).Or(Rec(() => _jsonObject!));

        private static readonly Parser<char, Json> _jsonArray =
            _json.Between(SkipWhitespaces)
                .Separated(_comma)
                .Between(_lBracket, _rBracket)
                .Select<Json>(els => new JsonArray(els.ToImmutableArray()));

        private static readonly Parser<char, KeyValuePair<string, Json>> _jsonMember =
            _string
                .Before(_colonWhitespace)
                .Then(_json, (name, val) => new KeyValuePair<string, Json>(name, val));

        private static readonly Parser<char, Json> _jsonObject =
            _jsonMember.Between(SkipWhitespaces)
                .Separated(_comma)
                .Between(_lBrace, _rBrace)
                .Select<Json>(kvps => new JsonObject(kvps.ToImmutableDictionary()));

        public static Result<char, Json> Parse(string input) => _json.Parse(input);
    }
}
