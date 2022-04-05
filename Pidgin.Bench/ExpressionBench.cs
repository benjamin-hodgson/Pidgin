using System;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Pidgin.Expression;
using static Pidgin.Parser<char>;

namespace Pidgin.Bench
{
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class ExpressionBench
    {
        #nullable disable
        private string _bigExpression;
        private Parser<char, int> _leftAssoc;
        private Parser<char, int> _rightAssoc;
        #nullable restore

        [GlobalSetup]
        public void Setup()
        {
            _bigExpression = string.Join("+", Enumerable.Range(1, 1000));

            var infixL = Operator.InfixL(Parser.Char('+').Then(Return<Func<int, int, int>>((x, y) => x + y)));
            _leftAssoc = ExpressionParser.Build(
                Parser.Num,
                new[] { new[] { infixL } }
            );
            var infixR = Operator.InfixR(Parser.Char('+').Then(Return<Func<int, int, int>>((x, y) => x + y)));
            _rightAssoc = ExpressionParser.Build(
                Parser.Num,
                new[] { new[] { infixR } }
            );
            RuntimeHelpers.RunClassConstructor(typeof(FarkleParsers.FarkleExpressionParser).TypeHandle);
        }

        [Benchmark, BenchmarkCategory("Long")]
        public void LongInfixL_Pidgin()
        {
            _leftAssoc.ParseOrThrow(_bigExpression);
        }
        [Benchmark, BenchmarkCategory("Long")]
        public void LongInfixR_Pidgin()
        {
            _rightAssoc.ParseOrThrow(_bigExpression);
        }
        [Benchmark(Baseline = true), BenchmarkCategory("Long")]
        public void LongInfixL_FParsec()
        {
            Pidgin.Bench.FParsec.ExpressionParser.parseL(_bigExpression);
        }
        [Benchmark, BenchmarkCategory("Long")]
        public void LongInfixR_FParsec()
        {
            Pidgin.Bench.FParsec.ExpressionParser.parseR(_bigExpression);
        }
        [Benchmark, BenchmarkCategory("Long")]
        public void LongInfixL_Farkle()
        {
            FarkleParsers.FarkleExpressionParser.ParseL(_bigExpression);
        }
        [Benchmark, BenchmarkCategory("Long")]
        public void LongInfixR_Farkle()
        {
            FarkleParsers.FarkleExpressionParser.ParseR(_bigExpression);
        }

        [Benchmark, BenchmarkCategory("Short")]
        public void ShortInfixL_Pidgin()
        {
            _leftAssoc.ParseOrThrow("1+1");
        }
        [Benchmark, BenchmarkCategory("Short")]
        public void ShortInfixR_Pidgin()
        {
            _rightAssoc.ParseOrThrow("1+1");
        }
        [Benchmark(Baseline = true), BenchmarkCategory("Short")]
        public void ShortInfixL_FParsec()
        {
            Pidgin.Bench.FParsec.ExpressionParser.parseL("1+1");
        }
        [Benchmark, BenchmarkCategory("Short")]
        public void ShortInfixR_FParsec()
        {
            Pidgin.Bench.FParsec.ExpressionParser.parseR("1+1");
        }
        [Benchmark, BenchmarkCategory("Short")]
        public void ShortInfixL_Farkle()
        {
            FarkleParsers.FarkleExpressionParser.ParseL("1+1");
        }
        [Benchmark, BenchmarkCategory("Short")]
        public void ShortInfixR_Farkle()
        {
            FarkleParsers.FarkleExpressionParser.ParseR("1+1");
        }
    }
}