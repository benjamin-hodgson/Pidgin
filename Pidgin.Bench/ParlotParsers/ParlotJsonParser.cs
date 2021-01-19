using System.Collections.Generic;
using System.Collections.Immutable;
using Parlot.Fluent;
using Pidgin.Examples.Json;

using static Parlot.Fluent.Parsers;

namespace Pidgin.Bench.ParlotParsers
{
    public class ParlotJsonParser
    {
        private static readonly Parlot.Fluent.Parser<IJson> Json;

        static ParlotJsonParser()
        {
            var LBrace = Terms.Char('{');
            var RBrace = Terms.Char('}');
            var LBracket = Terms.Char('[');
            var RBracket = Terms.Char(']');
            var Colon = Terms.Char(':');
            var Comma = Terms.Char(',');

            var String = Terms.String(StringLiteralQuotes.Double);

            var jsonString =
                String
                    .Then<IJson>(static s => new JsonString(s.ToString()));

            var json = Deferred<IJson>();

            var jsonArray =
                Between(LBracket, Separated(Comma, json), RBracket)
                    .Then<IJson>(static els => new JsonArray(els.ToImmutableArray()));

            var jsonMember =
                String.AndSkip(Colon).And(json)
                    .Then(static member => new KeyValuePair<string, IJson>(member.Item1.ToString(), member.Item2));

            var jsonObject =
                Between(LBrace, Separated(Comma, jsonMember), RBrace)
                    .Then<IJson>(static kvps => new JsonObject(kvps.ToImmutableDictionary()));

            Json = json.Parser = jsonString.Or(jsonArray).Or(jsonObject);
        }

        public static IJson Parse(string input)
        {
            if (Json.TryParse(input, out var result))
            {
                return result;
            }

            return null;
        }
    }
}