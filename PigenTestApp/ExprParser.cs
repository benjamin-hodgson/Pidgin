using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Pidgin;
using Pidgin.Comment;
using Pidgin.Expression;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

//using StringHelpers;

// cSpell: ignore parenthesised

namespace ApplicationSupport.Parsers
{
    public static class ExprParser
    {
        private static Parser<char, T> Tok<T>(Parser<char, T> token)
            => Try(token).Before(SkipWhitespaces);
        private static Parser<char, string> Tok(string token)
            => Tok(String(token));

        private static Parser<char, T> Parenthesised<T>(Parser<char, T> parser)
            => parser.Between(Tok("("), Tok(")"));

        private static Parser<char, Func<IExpr, IExpr, IExpr>> Binary(Parser<char, BinaryOperatorType> op)
            => op.Select<Func<IExpr, IExpr, IExpr>>(type => (l, r) => new BinaryOp(type, l, r));
        private static Parser<char, Func<IExpr, IExpr>> Unary(Parser<char, UnaryOperatorType> op)
            => op.Select<Func<IExpr, IExpr>>(type => o => new UnaryOp(type, o));

        private static readonly Parser<char, Func<IExpr, IExpr, IExpr>> Plus
            = Binary(Tok("+").ThenReturn(BinaryOperatorType.Plus));
        private static readonly Parser<char, Func<IExpr, IExpr, IExpr>> Minus
            = Binary(Tok("-").ThenReturn(BinaryOperatorType.Minus));
        private static readonly Parser<char, Func<IExpr, IExpr, IExpr>> Multiply
            = Binary(Tok("*").ThenReturn(BinaryOperatorType.Multiply));
        private static readonly Parser<char, Func<IExpr, IExpr, IExpr>> Divide
            = Binary(Tok("/").ThenReturn(BinaryOperatorType.Divide));
        private static readonly Parser<char, Func<IExpr, IExpr, IExpr>> EqualTo
           = Binary(Tok("=").Then(String("=")).ThenReturn(BinaryOperatorType.EqualTo));        // "=="
        private static readonly Parser<char, Func<IExpr, IExpr, IExpr>> EqualToA
        //    //= Binary(Tok("＝").ThenReturn(BinaryOperatorType.EqualTo));                       // 'FULLWIDTH EQUALS SIGN' (U+FF1D)
            = Binary(Tok("⩵").ThenReturn(BinaryOperatorType.EqualTo));                           // Two Consecutive Equals Signs (U+2A75)
        private static readonly Parser<char, Func<IExpr, IExpr, IExpr>> NotEqualTo
            = Binary(Tok("!").Then(String("=")).ThenReturn(BinaryOperatorType.NotEqualTo));     // "!="
        private static readonly Parser<char, Func<IExpr, IExpr, IExpr>> NotEqualToA
            = Binary(Tok("≠").ThenReturn(BinaryOperatorType.NotEqualTo));                       // 'NOT EQUAL TO' (U+2260)
        private static readonly Parser<char, Func<IExpr, IExpr, IExpr>> AssignTo
            = Binary(Tok(":").Then(String("=")).ThenReturn(BinaryOperatorType.AssignTo));       // ":="
        private static readonly Parser<char, Func<IExpr, IExpr, IExpr>> AssignToA
            = Binary(Tok("≔").ThenReturn(BinaryOperatorType.AssignTo));                         // 'COLON EQUALS' (U+2254)
        private static readonly Parser<char, Func<IExpr, IExpr>> Neg
            = Unary(Tok("-").ThenReturn(UnaryOperatorType.Neg));
        private static readonly Parser<char, Func<IExpr, IExpr>> UPlus
            = Unary(Tok("+").ThenReturn(UnaryOperatorType.UPlus));
        private static readonly Parser<char, Func<IExpr, IExpr>> Complement
            = Unary(Tok("~").ThenReturn(UnaryOperatorType.Complement));

        private static readonly Parser<char, IExpr> Identifier
            = Tok(Letter.Then(LetterOrDigit.ManyString(), (h, t) => h + t))
                .Select<IExpr>(name => new Identifier(name))
                .Labelled("identifier");

        private static readonly Parser<char, IExpr> Literal
            = Tok(Num)
                .Select<IExpr>(value => new Literal(value))
                .Labelled("integer literal");

        private static readonly Parser<char, IExpr> LiteralFloat
           = Tok(Float)
               .Select<IExpr>(value => new Literal(value))
               .Labelled("integer literal");

        private static readonly Parser<char, char> Quote = Char('"');

        private static readonly Parser<char, string> String =
            Token(c => c != '"')
                .ManyString()
                .Between(Quote);

        // Quoted string
        // (results in Literal)
        private static readonly Parser<char, IExpr> LiteralString
            = Tok(String)
                .Select<IExpr>(value => new Literal(value))
                .Labelled("string literal");

        private static Parser<char, IExpr> BuildExpressionParser()
        {
            Parser<char, IExpr> expr = null;

            var term = OneOf(
                Identifier,
                LiteralFloat,
                LiteralString,
                Literal,
                Parenthesised(Rec(() => expr)).Labelled("parenthesised expression")
            );

            var call = Parenthesised(Rec(() => expr).Separated(Tok(",")))
                .Select<Func<IExpr, IExpr>>(args => method => new Call(method, args.ToImmutableArray()))
                .Labelled("function call");

            // IEnumerable<OperatorTableRow<char, IExpr>>OperTable=  new[]
            // {
            //     Operator.PostfixChainable(call),
            //     Operator.Prefix(Neg).And(Operator.Prefix(Complement)).And(Operator.Prefix(UPlus)),
            //     Operator.InfixL(Multiply).And(Operator.InfixL(Divide)),
            //     Operator.InfixL(Plus).And(Operator.InfixL(Minus)),
            //     Operator.InfixL(EqualTo).And(Operator.InfixL(NotEqualTo))
            // };

            expr = ExpressionParser.Build(
                term,
                new[]
                {
                    Operator.PostfixChainable(call),
                    Operator.Prefix(Neg).And(Operator.Prefix(Complement)).And(Operator.Prefix(UPlus)),
                    //Operator.InfixL(Multiply).And(Operator.InfixL(Divide)).And(Operator.InfixL(Plus)).And(Operator.InfixL(Minus)),
                    Operator.InfixL(Multiply).And(Operator.InfixL(Divide)),
                    Operator.InfixL(Plus).And(Operator.InfixL(Minus)),
                    Operator.InfixL(EqualTo).And(Operator.InfixL(EqualToA))
                        .And(Operator.InfixL(NotEqualTo)).And(Operator.InfixL(NotEqualToA))
                        .And(Operator.InfixL(AssignTo)).And(Operator.InfixL(AssignToA))
                    //Operator.InfixL(EqualToA).And(Operator.InfixL(NotEqualToA)).And(Operator.InfixL(AssignToA))
                    //Operator.InfixL(EqualTo).And(Operator.InfixL(NotEqualTo)),
                    //Operator.InfixL(AssignTo)
                }
            ).Labelled("expression");

            return expr;
        }

        private static readonly Parser<char, IExpr> Expr = BuildExpressionParser();

        public static IExpr ParseOrThrow(string input)
        {
            try
            {
                return Expr.ParseOrThrow(input);
            }
            catch (Exception ex)
            {
                //AppLog.LogException(ex, $"Expression parser {input.Truncate(50)}");
                return null;
            }
        }
        //=> Expr.ParseOrThrow(input);
    }
}
