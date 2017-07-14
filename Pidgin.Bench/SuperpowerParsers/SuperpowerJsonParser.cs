using System.Collections.Generic;
using System.Collections.Immutable;
using Pidgin.Examples.Json;
using Superpower;
using Superpower.Parsers;

namespace Pidgin.Bench.SuperpowerParsers
{
    // adapted from my Pidgin json parser example
    public static class SuperpowerJsonParser
    {
        private static Superpower.TextParser<T> Between<T, U, V>(this Superpower.TextParser<T> p, Superpower.TextParser<U> before, Superpower.TextParser<V> after)
            => before.IgnoreThen(p).Then(x => after.Value(x));

        private static readonly Superpower.TextParser<char> LBrace = Character.EqualTo('{');
        private static readonly Superpower.TextParser<char> RBrace = Character.EqualTo('}');
        private static readonly Superpower.TextParser<char> LBracket = Character.EqualTo('[');
        private static readonly Superpower.TextParser<char> RBracket = Character.EqualTo(']');
        private static readonly Superpower.TextParser<char> Quote = Character.EqualTo('"');
        private static readonly Superpower.TextParser<char> Colon = Character.EqualTo(':');
        private static readonly Superpower.TextParser<char> ColonWhitespace =
            Colon.Between(Character.WhiteSpace.Many(), Character.WhiteSpace.Many());
        private static readonly Superpower.TextParser<char> Comma = Character.EqualTo(',');

        private static readonly Superpower.TextParser<string> String =
            Character.Matching(c => c != '"', "char except quote")
                .Many()
                .Between(Quote, Quote)
                .Select(string.Concat);
        private static readonly Superpower.TextParser<IJson> JsonString =
            String.Select(s => (IJson)new JsonString(s));
            
        private static readonly Superpower.TextParser<IJson> Json =
            JsonString.Or(Superpower.Parse.Ref(() => JsonArray)).Or(Superpower.Parse.Ref(() => JsonObject));

        private static readonly Superpower.TextParser<IJson> JsonArray = 
            Json.Between(Character.WhiteSpace.Many(), Character.WhiteSpace.Many())
                .ManyDelimitedBy(Comma)
                .Between(LBracket, RBracket)
                .Select(els => (IJson)new JsonArray(els.ToImmutableArray()));
        
        private static readonly Superpower.TextParser<KeyValuePair<string, IJson>> JsonMember =
            from name in String.SelectMany(_ => ColonWhitespace, (name, ws) => name)  // avoid allocating a transparent identifier for a result we don't care about
            from val in Json
            select new KeyValuePair<string, IJson>(name, val);

        private static readonly Superpower.TextParser<IJson> JsonObject = 
            JsonMember.Between(Character.WhiteSpace.Many(), Character.WhiteSpace.Many())
                .ManyDelimitedBy(Comma)
                .Between(LBrace, RBrace)
                .Select(kvps => (IJson)new JsonObject(kvps.ToImmutableDictionary()));
        
        public static IJson Parse(string input) => Json.Parse(input);
    }
}