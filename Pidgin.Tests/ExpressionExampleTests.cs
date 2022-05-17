using System.Collections.Immutable;

using Pidgin.Examples.Expression;

using Xunit;

namespace Pidgin.Tests;

public class ExpressionExampleTests
{
    [Fact]
    public void TestExpression()
    {
        var input = "12 * 3 + foo(-3, x)() * (2 + 1)";

        var expected = new BinaryOp(
            BinaryOperatorType.Add,
            new BinaryOp(
                BinaryOperatorType.Mul,
                new Literal(12),
                new Literal(3)
            ),

            new BinaryOp(
                BinaryOperatorType.Mul,

                new Call(
                    new Call(
                        new Identifier("foo"),
                        ImmutableArray.Create<Expr>(
                            new UnaryOp(UnaryOperatorType.Neg, new Literal(3)),
                            new Identifier("x")
                        )
                    ),
                    ImmutableArray.Create<Expr>()
                ),

                new BinaryOp(
                    BinaryOperatorType.Add,
                    new Literal(2),
                    new Literal(1)
                )
            )
        );

        Assert.Equal(ExprParser.ParseOrThrow(input), expected);
    }
}
