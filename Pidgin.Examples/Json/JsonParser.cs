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
        private static readonly Parser<char, IJson> _jsonString =
            _string.Select<IJson>(s => new JsonString(s));

        private static readonly Parser<char, IJson> _json =
            _jsonString.Or(Rec(() => _jsonArray!)).Or(Rec(() => _jsonObject!));

        private static readonly Parser<char, IJson> _jsonArray =
            _json.Between(SkipWhitespaces)
                .Separated(_comma)
                .Between(_lBracket, _rBracket)
                .Select<IJson>(els => new JsonArray(els.ToImmutableArray()));

        private static readonly Parser<char, KeyValuePair<string, IJson>> _jsonMember =
            _string
                .Before(_colonWhitespace)
                .Then(_json, (name, val) => new KeyValuePair<string, IJson>(name, val));

        private static readonly Parser<char, IJson> _jsonObject =
            _jsonMember.Between(SkipWhitespaces)
                .Separated(_comma)
                .Between(_lBrace, _rBrace)
                .Select<IJson>(kvps => new JsonObject(kvps.ToImmutableDictionary()));

        public static Result<char, IJson> Parse(string input) => _json.Parse(input);
    }
}
