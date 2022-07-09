using System;
using System.Collections.Immutable;

using Pidgin.Expression;

using static Pidgin.Parser;

namespace Pidgin.Examples.Expression;

public static class ExprParser
{
    private static Parser<char, T> Tok<T>(Parser<char, T> token)
        => Try(token).Before(SkipWhitespaces);
    private static Parser<char, string> Tok(string token)
        => Tok(String(token));

    private static Parser<char, T> Parenthesised<T>(Parser<char, T> parser)
        => parser.Between(Tok("("), Tok(")"));

    private static Parser<char, Func<Expr, Expr, Expr>> Binary(Parser<char, BinaryOperatorType> op)
        => op.Select<Func<Expr, Expr, Expr>>(type => (l, r) => new BinaryOp(type, l, r));
    private static Parser<char, Func<Expr, Expr>> Unary(Parser<char, UnaryOperatorType> op)
        => op.Select<Func<Expr, Expr>>(type => o => new UnaryOp(type, o));

    private static readonly Parser<char, Func<Expr, Expr, Expr>> _add
        = Binary(Tok("+").ThenReturn(BinaryOperatorType.Add));
    private static readonly Parser<char, Func<Expr, Expr, Expr>> _mul
        = Binary(Tok("*").ThenReturn(BinaryOperatorType.Mul));
    private static readonly Parser<char, Func<Expr, Expr>> _neg
        = Unary(Tok("-").ThenReturn(UnaryOperatorType.Neg));
    private static readonly Parser<char, Func<Expr, Expr>> _complement
        = Unary(Tok("~").ThenReturn(UnaryOperatorType.Complement));

    private static readonly Parser<char, Expr> _identifier
        = Tok(Letter.Then(LetterOrDigit.ManyString(), (h, t) => h + t))
            .Select<Expr>(name => new Identifier(name))
            .Labelled("identifier");
    private static readonly Parser<char, Expr> _literal
        = Tok(Num)
            .Select<Expr>(value => new Literal(value))
            .Labelled("integer literal");

    private static Parser<char, Func<Expr, Expr>> Call(Parser<char, Expr> subExpr)
        => Parenthesised(subExpr.Separated(Tok(",")))
            .Select<Func<Expr, Expr>>(args => method => new Call(method, args.ToImmutableArray()))
            .Labelled("function call");

    private static readonly Parser<char, Expr> _expr = ExpressionParser.Build<char, Expr>(
        expr => (
            OneOf(
                _identifier,
                _literal,
                Parenthesised(expr).Labelled("parenthesised expression")
            ),
            new[]
            {
                Operator.PostfixChainable(Call(expr)),
                Operator.Prefix(_neg).And(Operator.Prefix(_complement)),
                Operator.InfixL(_mul),
                Operator.InfixL(_add)
            }
        )
    ).Labelled("expression");

    public static Expr ParseOrThrow(string input)
        => _expr.ParseOrThrow(input);
}
