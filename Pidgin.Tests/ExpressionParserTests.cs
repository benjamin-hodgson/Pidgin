using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Pidgin.Expression;
using Xunit;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Tests;

public class ExpressionParserTests : ParserTestBase
{
    [SuppressMessage("Design", "CA1034", Justification = "Test code")]
    public abstract class Expr : IEquatable<Expr>
    {
        public override bool Equals(object? obj)
            => obj is Expr expr && Equals(expr);

        public bool Equals(Expr? other)
        {
            if (other == null)
            {
                return false;
            }

            // I had a normal recursive-virtual-method implementation
            // but it blew the stack on big inputs
            var stack = new Stack<(Expr, Expr)>();
            stack.Push((this, other));
            while (stack.Count > 0)
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

    private sealed class Lit : Expr
    {
        public int Value { get; }

        public Lit(int value)
        {
            Value = value;
        }
    }

    private sealed class Plus : Expr
    {
        public Expr Left { get; }

        public Expr Right { get; }

        public Plus(Expr left, Expr right)
        {
            Left = left;
            Right = right;
        }
    }

    private sealed class Minus : Expr
    {
        public Expr Left { get; }

        public Expr Right { get; }

        public Minus(Expr left, Expr right)
        {
            Left = left;
            Right = right;
        }
    }

    private sealed class Times : Expr
    {
        public Expr Left { get; }

        public Expr Right { get; }

        public Times(Expr left, Expr right)
        {
            Left = left;
            Right = right;
        }
    }

    public static IEnumerable<object[]> InfixNCases()
    {
        yield return new object[] { "1", new Lit(1), 1 };
        yield return new object[] { "1*2", new Times(new Lit(1), new Lit(2)), 3 };
        yield return new object[] { "1*2*3", new Times(new Lit(1), new Lit(2)), 3 };  // shouldn't consume "*3"
    }

    [Theory]
    [MemberData(nameof(InfixNCases))]
    public void TestInfixN(string input, Expr expected, int consumed)
    {
        var operatorTable = new[]
        {
            Operator.InfixN(
                Char('*').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Times(x, y)))
            )
        };
        var parser = ExpressionParser.Build(
            Digit.Select<Expr>(x => new Lit((int)char.GetNumericValue(x))),
            operatorTable
        );

        AssertPartialParse(parser, input, expected, consumed);
    }

    public static IEnumerable<object[]> InfixLCases()
    {
        yield return new object[]
        {
            "1",
            new Lit(1),
        };
        yield return new object[]
        {
            "1+2+3+4",
            new Plus(new Plus(new Plus(new Lit(1), new Lit(2)), new Lit(3)), new Lit(4)),
        };
        yield return new object[]
        {
            "1+2-3+4",
            new Plus(new Minus(new Plus(new Lit(1), new Lit(2)), new Lit(3)), new Lit(4)),
        };
        yield return new object[]
        {
            "1*2*3+4*5",
            new Plus(new Times(new Times(new Lit(1), new Lit(2)), new Lit(3)), new Times(new Lit(4), new Lit(5))),
        };
        {
            // should work with large inputs
            var numbers = Enumerable.Repeat(1, 100000);
            var input = string.Join("+", numbers);
            var expected = numbers
                .Select(n => new Lit(n))
                .Cast<Expr>()
                .Aggregate((acc, x) => new Plus(acc, x));
            yield return new object[] { input, expected };
        }
    }

    [Theory]
    [MemberData(nameof(InfixLCases))]
    public void TestInfixL(string input, Expr expected)
    {
        var operatorTable = new[]
        {
            Operator.InfixL(
                Char('*').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Times(x, y)))
            ),

            Operator.InfixL(
                Char('+').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Plus(x, y)))
            ).And(Operator.InfixL(
                Char('-').Then(Return<Func<Expr, Expr, Expr>>((x, y) => new Minus(x, y)))
            ))
        };
        var parser = ExpressionParser.Build(
            Digit.Select<Expr>(x => new Lit((int)char.GetNumericValue(x))),
            operatorTable
        );

        AssertFullParse(parser, input, expected);
    }

    public static IEnumerable<object[]> InfixRCases()
    {
        yield return new object[]
        {
            "1",
            new Lit(1),
        };
        yield return new object[]
        {
            "1+2+3+4",
            new Plus(new Lit(1), new Plus(new Lit(2), new Plus(new Lit(3), new Lit(4)))),
        };

        // yeah it's not mathematically accurate but who cares, it's a test
        yield return new object[]
        {
            "1+2-3+4",
            new Plus(new Lit(1), new Minus(new Lit(2), new Plus(new Lit(3), new Lit(4)))),
        };
        yield return new object[]
        {
            "1*2*3+4*5",
            new Plus(new Times(new Lit(1), new Times(new Lit(2), new Lit(3))), new Times(new Lit(4), new Lit(5))),
        };
        {
            // should work with large inputs
            var numbers = Enumerable.Repeat(1, 100000);
            var expected = numbers
                .Select(n => new Lit(n))
                .Cast<Expr>()
                .Reverse()
                .Aggregate((acc, x) => new Plus(x, acc));
            yield return new object[] { string.Join("+", numbers), expected };
        }
    }

    [Theory]
    [MemberData(nameof(InfixRCases))]
    public void TestInfixR(string input, Expr expected)
    {
        var operatorTable = new[]
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
        };
        var parser = ExpressionParser.Build(
            Digit.Select<Expr>(x => new Lit((int)char.GetNumericValue(x))),
            operatorTable
        );
        AssertFullParse(parser, input, expected);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("!true", false)]
    [InlineData("!(!true)", true)]
    public void TestPrefix(string input, bool expected)
    {
        var operatorTable = new[]
        {
            new[]
            {
                Operator.Prefix(
                    Char('!').Select<Func<bool, bool>>(_ => b => !b)
                )
            }
        };
        var parser = ExpressionParser.Build(
            expr =>
                String("false").Select(_ => false)
                    .Or(String("true").Select(_ => true))
                    .Or(expr.Between(Char('('), Char(')'))),
            operatorTable
        );

        AssertFullParse(parser, input, expected);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("!true", false)]
    [InlineData("!(!true)", true)]
    [InlineData("!!true", true)]
    [InlineData("!~true", true)]
    [InlineData("~!true", true)]
    [InlineData("!~!true", false)]
    [InlineData("~!~true", false)]
    public void TestPrefixChainable(string input, bool expected)
    {
        var operatorTable = new[]
        {
            new[]
            {
                Operator.PrefixChainable(
                    Char('!').Select<Func<bool, bool>>(_ => b => !b),
                    Char('~').Select<Func<bool, bool>>(_ => b => !b)
                )
            }
        };
        var parser = ExpressionParser.Build(
            expr =>
                String("false").Select(_ => false)
                    .Or(String("true").Select(_ => true))
                    .Or(expr.Between(Char('('), Char(')'))),
            operatorTable
        );

        AssertFullParse(parser, input, expected);
    }

    [Theory]
    [InlineData("f")]
    [InlineData("f()")]
    public void TestPostfix(string value)
    {
        var operatorTable = new[]
        {
            new[]
            {
                Operator.Postfix(String("()").ThenReturn<Func<string, string>>(f => f + "()"))
            }
        };
        var parser = ExpressionParser.Build(String("f"), operatorTable);

        AssertFullParse(parser, value, value);
    }

    [Theory]
    [InlineData("f")]
    [InlineData("f()")]
    [InlineData("f()()")]
    [InlineData("f()()()")]
    public void TestPostfixChainable(string value)
    {
        var operatorTable = new[]
        {
            new[]
            {
                Operator.PostfixChainable(String("()").ThenReturn<Func<string, string>>(f => f + "()"))
            }
        };
        var parser = ExpressionParser.Build(String("f"), operatorTable);

        AssertFullParse(parser, value, value);
    }
}
