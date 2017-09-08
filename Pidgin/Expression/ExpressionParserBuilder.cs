using System;
using System.Collections.Generic;
using System.Linq;
using static Pidgin.Parser;

namespace Pidgin.Expression
{
    internal static class ExpressionParserBuilder<TToken, T>
    {
        private static readonly IEnumerable<Parser<TToken, Func<T, T>>> _returnIdentity
            = new[]{ Parser<TToken>.Return<Func<T,T>>(x => x) };

        public static Parser<TToken, T> Build(Parser<TToken, T> term, OperatorTableRow<TToken, T> row)
        {
            var pTerm = Map(
                (pre, tm, post) => post(pre(tm)),
                OneOf(row.PrefixOps.Concat(_returnIdentity)),
                term,
                OneOf(row.PostfixOps.Concat(_returnIdentity))
            );

            var infixN = Op(pTerm, row.InfixNOps).Select<Func<T, T>>(p => p.ApplyL);
            var infixL = Op(pTerm, row.InfixLOps)
                .AtLeastOnce()
                .Select<Func<T, T>>(fxs => z =>
                    fxs.Aggregate(
                        z,
                        (exp, fx) => fx.ApplyL(exp)
                    )
                );
            var infixR = Op(pTerm, row.InfixROps)
                .AtLeastOnce()
                .Select(fxs =>
                    // reassociate the parsed operators:
                    // move the right-hand term of each operator to the
                    // left-hand side of the next operator on the right,
                    // leaving a hole at the left
                    fxs.AggregateR(
                        new Partial((y, _) => y, default(T)),
                        (fx, agg) => new Partial(fx.Func, agg.ApplyL(fx.Arg))
                    )
                )
                .Select<Func<T, T>>(p => p.ApplyL);
            
            var op = OneOf(
                infixN,
                infixL,
                infixR,
                Parser<TToken>.Return<Func<T, T>>(x => x)
            );
            return Map(
                (x, f) => f(x),
                pTerm,
                op
            );
        }

        private static Parser<TToken, Partial> Op(Parser<TToken, T> pTerm, IEnumerable<Parser<TToken, Func<T, T, T>>> ops)
            => Map(
                (f, y) => new Partial(f, y),
                OneOf(ops),
                pTerm
            );

        private struct Partial
        {
            public Func<T, T, T> Func { get; }
            public T Arg { get; }
            public Partial(Func<T, T, T> func, T arg)
            {
                Func = func;
                Arg = arg;
            }
            public T ApplyL(T arg) => Func(arg, Arg);
        }
    }
}