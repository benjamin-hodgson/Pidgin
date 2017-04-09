using System;
using Pidgin.Expression;
using Xunit;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Tests
{
    public class ExpressionParserTests : ParserTestBase
    {
        private abstract class Expr : IEquatable<Expr>
        {
            public override bool Equals(object other)
                => other is Expr && this.Equals((Expr)other);
            public abstract bool Equals(Expr other);

            public override int GetHashCode() => 0;  // doesn't matter
        }
        private class Lit : Expr
        {
            public int Value { get; }
            public Lit(int value)
            {
                Value = value;
            }
            public override bool Equals(Expr other)
                => other is Lit
                && ((Lit)other).Value == this.Value;
        }
        private class Plus : Expr
        {
            public Expr Left { get; }
            public Expr Right { get; }
            public Plus(Expr left, Expr right)
            {
                Left = left;
                Right = right;
            }
            public override bool Equals(Expr other)
                => other is Plus
                && ((Plus)other).Left.Equals(this.Left)
                && ((Plus)other).Right.Equals(this.Right);
        }
        private class Minus : Expr
        {
            public Expr Left { get; }
            public Expr Right { get; }
            public Minus(Expr left, Expr right)
            {
                Left = left;
                Right = right;
            }
            public override bool Equals(Expr other)
                => other is Minus
                && ((Minus)other).Left.Equals(this.Left)
                && ((Minus)other).Right.Equals(this.Right);
        }
        private class Times : Expr
        {
            public Expr Left { get; }
            public Expr Right { get; }
            public Times(Expr left, Expr right)
            {
                Left = left;
                Right = right;
            }
            public override bool Equals(Expr other)
                => other is Times
                && ((Times)other).Left.Equals(this.Left)
                && ((Times)other).Right.Equals(this.Right);
        }

        [Fact]
        public void TestInfixL()
        {
            Parser<char, Expr> parser = null;
            var termParser = Digit.Select<Expr>(x => new Lit((int)char.GetNumericValue(x)));
            parser = ExpressionParser.Build(
                termParser,
                new[]
                {
                    Operator.InfixL(
                        Char('*').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Times(x, y)))
                    ),

                    Operator.InfixL(
                        Char('+').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Plus(x, y)))
                    ).And(Operator.InfixL(
                        Char('-').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Minus(x, y)))
                    ))
                }
            );

            AssertSuccess(
                parser.Parse("1+2+3+4"),
                new Plus(new Plus(new Plus(new Lit(1), new Lit(2)), new Lit(3)), new Lit(4)),
                true
            );
            AssertSuccess(
                parser.Parse("1+2-3+4"),
                new Plus(new Minus(new Plus(new Lit(1), new Lit(2)), new Lit(3)), new Lit(4)),
                true
            );
            AssertSuccess(
                parser.Parse("1*2*3+4*5"),
                new Plus(new Times(new Times(new Lit(1), new Lit(2)), new Lit(3)), new Times(new Lit(4), new Lit(5))),
                true
            );
        }

        [Fact]
        public void TestInfixR()
        {
            Parser<char, Expr> parser = null;
            var termParser = Digit.Select<Expr>(x => new Lit((int)char.GetNumericValue(x)));
            parser = ExpressionParser.Build(
                termParser,
                new[]
                {
                    new[]
                    {
                        Operator.InfixR(
                            Char('*').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Times(x, y)))
                        )
                    },
                    new[]
                    {
                        Operator.InfixR(
                            Char('+').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Plus(x, y)))
                        ),
                        Operator.InfixR(
                            Char('-').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Minus(x, y)))
                        )
                    }
                }
            );

            AssertSuccess(
                parser.Parse("1+2+3+4"),
                new Plus(new Lit(1), new Plus(new Lit(2), new Plus(new Lit(3), new Lit(4)))),
                true
            );
            // yeah it's not mathematically accurate but who cares, it's a test
            AssertSuccess(
                parser.Parse("1+2-3+4"),
                new Plus(new Lit(1), new Minus(new Lit(2), new Plus(new Lit(3), new Lit(4)))),
                true
            );
            AssertSuccess(
                parser.Parse("1*2*3+4*5"),
                new Plus(new Times(new Lit(1), new Times(new Lit(2), new Lit(3))), new Times(new Lit(4), new Lit(5))),
                true
            );
        }
    }
}