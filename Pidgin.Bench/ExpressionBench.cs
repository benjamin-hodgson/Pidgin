using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Pidgin.Expression;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Bench
{
    [Config(typeof(Config))]
    public class ExpressionBench
    {
        private string _bigExpression;
        private Parser<char, int> _leftAssoc1;
        private Parser<char, int> _leftAssoc2;
        private Parser<char, int> _rightAssoc1;
        private Parser<char, int> _rightAssoc2;

        [Setup]
        public void Setup()
        {
            var numbers = Enumerable.Range(1, 1000);
            _bigExpression = "0" + numbers.Aggregate("", (x, y) => x + "+" + y);

            var term = Parser.Digit.Many().Select(cs => int.Parse(string.Concat(cs)));
            var infixL = Operator.InfixL(Parser.Char('+').Then(Return<Func<int, int, int>>((x, y) => x + y)));
            _leftAssoc1 = ExpressionParser.Build(
                term,
                new[] { new[] { infixL } }
            );
            _leftAssoc2 = ExpressionParser.Build2(
                term,
                new[] { new[] { infixL } }
            );
            var infixR = Operator.InfixR(Parser.Char('+').Then(Return<Func<int, int, int>>((x, y) => x + y)));
            _rightAssoc1 = ExpressionParser.Build(
                term,
                new[] { new[] { infixR } }
            );
            _rightAssoc2 = ExpressionParser.Build2(
                term,
                new[] { new[] { infixR } }
            );
        }
    
        [Benchmark]
        public void InfixL1()
        {
            _leftAssoc1.ParseOrThrow(_bigExpression);
        }
        [Benchmark]
        public void InfixL2()
        {
            _leftAssoc2.ParseOrThrow(_bigExpression);
        }
        [Benchmark]
        public void InfixR1()
        {
            _rightAssoc1.ParseOrThrow(_bigExpression);
        }
        [Benchmark]
        public void InfixR2()
        {
            _rightAssoc2.ParseOrThrow(_bigExpression);
        }
    }
}