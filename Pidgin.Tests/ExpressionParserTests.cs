using System;
using System.Collections.Generic;
using System.Linq;
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
            public override bool Equals(object? other)
                => other is Expr && this.Equals((Expr)other);
            public bool Equals(Expr other)
            {
                // I had a normal recursive-virtual-method implementation
                // but it blew the stack on big inputs
                var stack = new Stack<(Expr, Expr)>();
                stack.Push((this, other));
                while (stack.Any())
                {
                    var (l, r) = stack.Pop();

                    if (l is Lit l1 && r is Lit l2)
                    {
                        if (l1.Value != l2.Value)
                        {
                            return false;
                        }
                    }
                    else if (l is Plus p1 && r is Plus p2)
                    {
                        stack.Push((p1.Left, p2.Left));
                        stack.Push((p1.Right, p2.Right));
                    }
                    else if (l is Minus m1 && r is Minus m2)
                    {
                        stack.Push((m1.Left, m2.Left));
                        stack.Push((m1.Right, m2.Right));
                    }
                    else if (l is Times t1 && r is Times t2)
                    {
                        stack.Push((t1.Left, t2.Left));
                        stack.Push((t1.Right, t2.Right));
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }

            public override int GetHashCode() => 0;  // doesn't matter
        }
        private class Lit : Expr
        {
            public int Value { get; }
            public Lit(int value)
            {
                Value = value;
            }
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
        }

        [Fact]
        public void TestInfixN()
        {
            var parser = ExpressionParser.Build(
                Digit.Select<Expr>(x => new Lit((int)char.GetNumericValue(x))),
                new[]
                {
                    Operator.InfixN(
                        Char('*').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Times(x, y)))
                    )
                }
            );

            AssertSuccess(
                parser.Parse("1"),
                new Lit(1),
                true
            );
            AssertSuccess(
                parser.Parse("1*2"),
                new Times(new Lit(1), new Lit(2)),
                true
            );
            AssertSuccess(
                parser.Parse("1*2*3"),
                new Times(new Lit(1), new Lit(2)),
                true
            );
        }

        [Fact]
        public void TestInfixL()
        {
            var parser = ExpressionParser.Build(
                Digit.Select<Expr>(x => new Lit((int)char.GetNumericValue(x))),
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
                parser.Parse("1"),
                new Lit(1),
                true
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
            
            // should work with large inputs
            var numbers = Enumerable.Repeat(1, 100000);
            var input = string.Join("+", numbers);
            var expected = numbers
                .Select(n => new Lit(n))
                .Cast<Expr>()
                .Aggregate((Expr?)null, (acc, x) => acc == null ? x : new Plus(acc, x));
            AssertSuccess(
                parser.Parse(input)!,
                expected,
                true
            );
        }

        [Fact]
        public void TestInfixR()
        {
            var parser = ExpressionParser.Build(
                Digit.Select<Expr>(x => new Lit((int)char.GetNumericValue(x))),
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
                parser.Parse("1"),
                new Lit(1),
                true
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
            
            // should work with large inputs
            var numbers = Enumerable.Repeat(1, 100000);
            var input = string.Join("+", numbers);
            var expected = numbers
                .Select(n => new Lit(n))
                .Cast<Expr>()
                .AggregateR((Expr?)null, (x, acc) => acc == null ? x : new Plus(x, acc));
            AssertSuccess(
                parser.Parse(input)!,
                expected,
                true
            );
        }

        [Fact]
        public void TestPrefix()
        {
            var parser = ExpressionParser.Build(
                expr => 
                    String("false").Select(_ => false)
                        .Or(String("true").Select(_ => true))
                        .Or(expr.Between(Char('('), Char(')'))),
                new[]
                {
                    new[]
                    {
                        Operator.Prefix(
                            Char('!').Select<Func<bool, bool>>(_ => b => !b)
                        )
                    }
                }
            );

            AssertSuccess(parser.Parse("true"), true, true);
            AssertSuccess(parser.Parse("!true"), false, true);
            AssertSuccess(parser.Parse("!(!true)"), true, true);
        }

        [Fact]
        public void TestPrefixChainable()
        {
            var parser = ExpressionParser.Build(
                expr => 
                    String("false").Select(_ => false)
                        .Or(String("true").Select(_ => true))
                        .Or(expr.Between(Char('('), Char(')'))),
                new[]
                {
                    new[]
                    {
                        Operator.PrefixChainable(
                            Char('!').Select<Func<bool, bool>>(_ => b => !b),
                            Char('~').Select<Func<bool, bool>>(_ => b => !b)
                        )
                    }
                }
            );

            AssertSuccess(parser.Parse("true"), true, true);
            AssertSuccess(parser.Parse("!true"), false, true);
            AssertSuccess(parser.Parse("!(!true)"), true, true);
            AssertSuccess(parser.Parse("!!true"), true, true);
            AssertSuccess(parser.Parse("!~true"), true, true);
            AssertSuccess(parser.Parse("~!true"), true, true);
        }

        [Fact]
        public void TestPostfix()
        {
            Func<dynamic> f = () => true;

            var termParser = String("f").Select<dynamic>(_ => f);
            var parser = ExpressionParser.Build(
                termParser,
                new[]
                {
                    new[]
                    {
                        Operator.Postfix(String("()").Select<Func<dynamic, dynamic>>(_ => g => g()))
                    }
                }
            );

            AssertSuccess(parser.Parse("f"), f, true);
            AssertSuccess(parser.Parse("f()"), f(), true);
        }

        [Fact]
        public void TestPostfixChainable()
        {
            Func<dynamic> f = () => true;
            Func<Func<dynamic>> g = () => f;

            var termParser = String("f").Select<dynamic>(_ => f).Or(String("g").Select<dynamic>(_ => g));
            var parser = ExpressionParser.Build(
                termParser,
                new[]
                {
                    new[]
                    {
                        Operator.PostfixChainable(String("()").Select<Func<dynamic, dynamic>>(_ => h => h()))
                    }
                }
            );

            AssertSuccess(parser.Parse("f"), f, true);
            AssertSuccess(parser.Parse("f()"), f(), true);
            AssertSuccess(parser.Parse("g()()"), g()(), true);
        }
    }
}