using System;
using System.Collections.Generic;
using Pidgin.Expression;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Examples.Expr
{
    public static class ExprParser
    {
        public static readonly Parser<char, char> LBrace = Char('{');
        public static readonly Parser<char, char> RBrace = Char('}');
        public static readonly Parser<char, char> LParen = Char('(');
        public static readonly Parser<char, char> RParen = Char(')');
        public static readonly Parser<char, char> Comma = Char(',');
        public static readonly Parser<char, char> UScore = Char('_');
        public static readonly Parser<char, char> DQuote = Char('"');
        public static readonly Parser<char, char> SQuote = Char('\'');
        public static readonly Parser<char, char> Dot = Char('.');
        private static readonly Parser<char, char> Sign = Char('+').Or(Char('-'));
        private static readonly Parser<char, string> Exp = OneOf(
            Try(Map((e, s, p) => e.ToString() + s + p, CIChar('e'), Sign, Digit.AtLeastOnceString())),
            Try(Map((e, p) => e + p, CIChar('e'), Digit.AtLeastOnceString()))
        );

        public static readonly Parser<char, string> Str = Token(c => c != '"').ManyString().Between(DQuote).Labelled("str");
        public static readonly Parser<char, string> Chr = Token(c => c != '\'').ManyString().Between(SQuote).Labelled("chr");

        public static readonly Parser<char, string> Int = Digit.AtLeastOnceString().Labelled("int");

        public static readonly Parser<char, string> Float = OneOf(
            Try(Map((h, d, t, e) => h + d + t + e, Digit.ManyString(), Dot, Digit.AtLeastOnceString(), Exp)),
            Try(Map((h, d, t) => h + d + t, Digit.ManyString(), Dot, Digit.AtLeastOnceString())),
            Try(Map((h, e) => h + e, Digit.AtLeastOnceString(), Exp))
        ).Labelled("float");
        private static readonly Parser<char, string> Num = Float.Or(Int).Labelled("num");

        public static readonly Parser<char, char> IdStart = UScore.Or(Letter);
        public static readonly Parser<char, char> IdRest = UScore.Or(LetterOrDigit);
        private static readonly Parser<char, string> IdInternal = Map((h, t) => h + t, IdStart, IdRest.ManyString());

        public static readonly Parser<char, string> True = String("true").Labelled("true");
        public static readonly Parser<char, string> False = String("false").Labelled("false");
        public static readonly Parser<char, string> If = String("if").Labelled("if");
        public static readonly Parser<char, string> Else = String("else").Labelled("else");
        public static readonly Parser<char, string> Then = String("then").Labelled("then");
        public static readonly Parser<char, string> In = String("in").Labelled("in");

        private static readonly Parser<char, Unit> LookaheadKeyWordEnd = Lookahead(Not(IdRest).Or(End()));

        public static readonly Parser<char, string> Bool = True.Or(False).Between(DQuote).Or(True.Or(False)).Labelled("bool");

        public static readonly Parser<char, string> KeyWords =
            OneOf(Else, Try(Then), Try(If), Try(In)).Before(LookaheadKeyWordEnd).Labelled("keyword");

        public static readonly Parser<char, string> ReservedWords =
            OneOf(False, Try(True), Else, Try(Then), Try(If), Try(In)).Before(LookaheadKeyWordEnd).Labelled("reserved");

        public static readonly Parser<char, string> Id = Try(Lookahead(Not(ReservedWords).Or(End()))).Then(IdInternal).Labelled("id");

        public static readonly Tuple<Parser<char, Expr>, Stack<bool>, Stack<bool>> Parser = BuildExprParser();

        public static Result<char, Expr> Parse(string input)
        {
            try
            {
                var ret = Parser.Item1.Parse(input.Trim());
                if (Parser.Item2.Count == 0 && Parser.Item3.Count == 0) return ret;

                if (Parser.Item2.Count>0)
                    throw new InvalidOperationException($"Unbalanced C-style Ternary Operators, still have {Parser.Item2.Count} unmatched.");
                else
                    throw new InvalidOperationException($"Unbalanced Python-style Ternary Operators, still have {Parser.Item3.Count} unmatched.");
            }
            finally
            {
                Parser.Item2.Clear();
                Parser.Item3.Clear();
            }
        }

        private static Tuple<Parser<char, Expr>, Stack<bool>, Stack<bool>> BuildExprParser()
        {
            Parser<char, Expr> exprParser = null;

            var listParser = Rec(() => exprParser).Separated(Comma.Before(SkipWhitespaces));
            var funcHead = Id.Or(If).Between(SkipWhitespaces).Before(LParen.Between(SkipWhitespaces));
            var funcParser = Try(Lookahead(funcHead)).Then(Map((id, prms) => (Expr) new Function(id, prms), funcHead,
                listParser.Before(RParen))).Labelled("func");
            var termParser = funcParser
                .Or(Try(Bool.Select<Expr>(x => new BoolLit(x)))
                .Or(Num.Select<Expr>(x => new NumLit(x)))
                .Or(Str.Select<Expr>(x => new StringLit(x)))
                .Or(Chr.Select<Expr>(x => new CharLit(x)))
                .Or(Id.Select<Expr>(x => new Identifier(x)))
                .Or(Rec(() => listParser)
                    .Between(LBrace.Then(SkipWhitespaces), SkipWhitespaces.Then(RBrace))
                    .Select<Expr>(x => new Membership(x)))
                .Or(Rec(() => exprParser)
                    .Between(LParen.Then(SkipWhitespaces), SkipWhitespaces.Then(RParen))
                    .Select<Expr>(x => new Nested(x)))
                ).Labelled("term")
            .Between(SkipWhitespaces.Or(End()));
            var cTerStack = new Stack<bool>();
            var pyTerStack = new Stack<bool>();
            exprParser = ExpressionParser.Build(termParser, new[]
            {
                // arithmatic
                new[] {
                    Operator.Prefix(Char('+').Between(SkipWhitespaces).Then(Return<Func<Expr,Expr>>(x =>new Positive(x)))),
                    Operator.Prefix(Char('-').Between(SkipWhitespaces).Then(Return<Func<Expr,Expr>>(x =>new Negative(x)))),
                },
                new[] {
                    Operator.InfixR(Char('^').Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Power(x,y)))),
                },
                new[] {
                    Operator.InfixL(Char('*').Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Mult(x,y)))),
                    Operator.InfixL(Char('/').Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Div(x,y)))),
                },
                new[] {
                    Operator.InfixL(Char('%').Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Mod(x,y)))),
                },
                new[] {
                    Operator.InfixL(Char('+').Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Plus(x,y)))),
                    Operator.InfixL(Char('-').Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Minus(x,y)))),
                },
                new[] { // VB string cat
                    Operator.InfixL(Try(Lookahead(Not(Char('&').Before(Char('&'))))).Then(Char('&').Between(SkipWhitespaces)).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Cat(x,y)))),
                },
                new[] {
                    Operator.InfixL(Try(Lookahead(String("??"))).Then(String("??").Between(SkipWhitespaces)).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new NullCoalesce(x,y)))),
                },
                // comparision. TODO: check how to separate arithmatic, comparison and logical ops
                new[] {
                    Operator.InfixL(Try(String(">=").Between(SkipWhitespaces)).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Ge(x,y)))),
                    Operator.InfixL(Try(String("<=").Between(SkipWhitespaces)).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Le(x,y)))),
                    Operator.InfixL(Try(String("<>").Between(SkipWhitespaces)).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Ne("<>",x,y)))),
                    Operator.InfixL(String("!=").Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Ne(x,y)))),
                    Operator.InfixL(String(">").Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Gt(x,y)))),
                    Operator.InfixL(String("<").Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Lt(x,y)))),
                    Operator.InfixL(Try(String("==")).Or(String("=")).Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Eq(x,y)))),
                    Operator.InfixL(Try(String("in").Between(SkipWhitespaces).Before(Lookahead(LBrace))).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new In(x,y)))),
                },
                // logical. TODO: check how to separate arithmatic, comparison and logical ops
                new[] {
                    Operator.Prefix(Char('!').Between(SkipWhitespaces).Then(Return<Func<Expr,Expr>>(x=>new Not(x)))),
                },
                new[] {
                    Operator.InfixL(String("&&").Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new And(x,y)))),
                },
                new[] {
                    Operator.InfixL(String("||").Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>new Or(x,y)))),
                },
                new[] {
                    Operator.InfixR(If.Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>
                    {
                        if (pyTerStack.Count==0) throw new InvalidOperationException("Unbalanced Python-style Ternary Operators, fewer 'else' parts than 'then' parts");
                        pyTerStack.Pop();
                        return TerUtils.FormPyTerExpr(x, y);
                    }))),
                    Operator.InfixR(Else.Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>
                    {
                        pyTerStack.Push(true);
                        return new PyTerExprElse(x, y);
                    }))),
                    Operator.InfixR(Try(Lookahead(Not(Char('?').Before(Char('?'))))).Then(Char('?').Between(SkipWhitespaces)).Then(Return<Func<Expr,Expr,Expr>>((x,y) =>
                    {
                        if (cTerStack.Count==0) throw new InvalidOperationException("Unbalanced C-style Ternary Operators, fewer 'else' parts than 'then' parts");
                        cTerStack.Pop();
                        return TerUtils.FormCTerExpr(x, y);
                    }))),
                    Operator.InfixR(Char(':').Between(SkipWhitespaces).Then(Return<Func<Expr,Expr,Expr>>((x,y)=>
                    {
                        cTerStack.Push(true);
                        return new CTerElseExpr(x, y);
                    }))),
                },
            });
            return Tuple.Create(exprParser,cTerStack,pyTerStack);
        }
    }
}