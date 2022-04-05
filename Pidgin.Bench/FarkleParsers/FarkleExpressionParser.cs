using Farkle;
using Farkle.Builder;
using Farkle.Builder.OperatorPrecedence;
using System;

namespace Pidgin.Bench.FarkleParsers
{
    internal static class FarkleExpressionParser
    {
        private static readonly RuntimeFarkle<int> _parserL = CreateParser(AssociativityType.LeftAssociative);
        private static readonly RuntimeFarkle<int> _parserR = CreateParser(AssociativityType.RightAssociative);

        private static RuntimeFarkle<int> CreateParser(AssociativityType associativity)
        {
            var number = Terminals.Int32("Number");
            var expr = Nonterminal.Create<int>("Expression");
            expr.SetProductions(
                expr.Extended().Append("+").Extend(expr).Finish((x1, x2) => x1 + x2),
                number.AsIs()
            );

            var opScope = new OperatorScope(new AssociativityGroup(associativity, "+"));
            return expr.WithOperatorScope(opScope).Build();
        }

        private static int ParseOrThrow(string str, RuntimeFarkle<int> parser)
        {
            var result = parser.Parse(str);
            if (result.IsOk)
            {
                return result.ResultValue;
            }
            else
            {
                throw new Exception(result.ErrorValue.ToString());
            }
        }

        public static int ParseL(string str) => ParseOrThrow(str, _parserL);
        public static int ParseR(string str) => ParseOrThrow(str, _parserR);
    }
}