using System.Collections.Generic;
using System.Collections.Immutable;
using Pidgin.Examples.Json;
using Sprache;
using static Sprache.Parse;

namespace Pidgin.Bench.SpracheParsers
{
    // adapted from my Pidgin json parser example
    public static class SpracheJsonParser
    {
        private static readonly Sprache.Parser<char> LBrace = Char('{');
        private static readonly Sprache.Parser<char> RBrace = Char('}');
        private static readonly Sprache.Parser<char> LBracket = Char('[');
        private static readonly Sprache.Parser<char> RBracket = Char(']');
        private static readonly Sprache.Parser<char> Quote = Char('"');
        private static readonly Sprache.Parser<char> Colon = Char(':');
        private static readonly Sprache.Parser<char> ColonWhitespace =
            Colon.Contained(WhiteSpace.Many(), WhiteSpace.Many());
        private static readonly Sprache.Parser<char> Comma = Char(',');

        private static readonly Sprache.Parser<string> String =
            Char(c => c != '"', "char except quote")
                .Many()
                .Contained(Quote, Quote)
                .Select(string.Concat);
        private static readonly Sprache.Parser<IJson> JsonString =
            String.Select(s => new JsonString(s));
            
        private static readonly Sprache.Parser<IJson> Json =
            JsonString.Or(Ref(() => JsonArray)).Or(Ref(() => JsonObject));

        private static readonly Sprache.Parser<IJson> JsonArray = 
            Json.Contained(WhiteSpace.Many(), WhiteSpace.Many())
                .DelimitedBy(Comma)
                .Contained(LBracket, RBracket)
                .Select(els => new JsonArray(els.ToImmutableArray()));
        
        private static readonly Sprache.Parser<KeyValuePair<string, IJson>> JsonMember =
            from name in String.SelectMany(_ => ColonWhitespace, (name, ws) => name)  // avoid allocating a transparent identifier for a result we don't care about
            from val in Json
            select new KeyValuePair<string, IJson>(name, val);

        private static readonly Sprache.Parser<IJson> JsonObject = 
            JsonMember.Contained(WhiteSpace.Many(), WhiteSpace.Many())
                .DelimitedBy(Comma)
                .Contained(LBrace, RBrace)
                .Select(kvps => new JsonObject(kvps.ToImmutableDictionary()));
        
        public static IResult<IJson> Parse(string input) => Json(new Input(input));
    }
}