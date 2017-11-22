using System;
using System.Collections.Generic;
using System.Linq;
using static Pidgin.Parser;

namespace Pidgin.Expression
{
    /// <summary>
    /// Contains tools for parsing expression languages with associative infix operators.
    /// </summary>
    public static class ExpressionParser
    {
        /// <summary>
        /// Builds a parser for expressions built from the operators in <paramref name="operatorTable"/>.
        /// <paramref name="operatorTable"/> is a sequence of operators in precedence order:
        /// the operators in the first row have the highest precedence and operators in later rows have lower precedence.
        /// </summary>
        /// <param name="term">A parser for a single term in an expression language</param>
        /// <param name="operatorTable">A table of operators</param>
        /// <returns>A parser for expressions built from the operators in <paramref name="operatorTable"/>.</returns>
        public static Parser<TToken, T> Build<TToken, T>(
            Parser<TToken, T> term,
            IEnumerable<IEnumerable<OperatorTableRow<TToken, T>>> operatorTable
        ) => Build(
            term,
            operatorTable.Select(r => r.Aggregate(OperatorTableRow<TToken, T>.Empty, (p, q) => p.And(q)))
        );

        /// <summary>
        /// Builds a parser for expressions built from the operators in <paramref name="operatorTable"/>.
        /// <paramref name="operatorTable"/> is a sequence of operators in precedence order:
        /// the operators in the first row have the highest precedence and operators in later rows have lower precedence.
        /// </summary>
        /// <param name="term">A parser for a single term in an expression language</param>
        /// <param name="operatorTable">A table of operators</param>
        /// <returns>A parser for expressions built from the operators in <paramref name="operatorTable"/>.</returns>
        public static Parser<TToken, T> Build<TToken, T>(
            Parser<TToken, T> term,
            IEnumerable<OperatorTableRow<TToken, T>> operatorTable
        ) => operatorTable.Aggregate(term, Build);


        private static Parser<TToken, T> Build<TToken, T>(Parser<TToken, T> term, OperatorTableRow<TToken, T> row)
        {
            var returnIdentity = Parser<TToken>.Return<Func<T,T>>(x => x);
            var returnIdentityArray = new[]{ returnIdentity };

            var pTerm = Map(
                (pre, tm, post) => post(pre(tm)),
                OneOf(row.PrefixOps.Concat(returnIdentityArray)),
                term,
                OneOf(row.PostfixOps.Concat(returnIdentityArray))
            );

            var infixN = Op(pTerm, row.InfixNOps).Select<Func<T, T>>(p => z => p.ApplyL(z));
            var infixL = Op(pTerm, row.InfixLOps)
                .AtLeastOncePooled()
                .Select<Func<T, T>>(fxs =>
                    z =>
                    {
                        var result = fxs.Aggregate(
                            z,
                            (exp, fx) => fx.ApplyL(exp)
                        );
                        fxs.Clear();
                        return result;
                    }
                );
            var infixR = Op(pTerm, row.InfixROps)
                .AtLeastOncePooled()
                .Select<Func<T, T>>(fxs =>
                    {
                        // reassociate the parsed operators:
                        // move the right-hand term of each operator to the
                        // left-hand side of the next operator on the right,
                        // leaving a hole at the left
                        var result = fxs.AggregateR(
                            new Partial<T>((y, _) => y, default(T)),
                            (fx, agg) => new Partial<T>(fx.Func, agg.ApplyL(fx.Arg))
                        );
                        fxs.Clear();
                        return z => result.ApplyL(z);
                    }
                );
            
            var op = OneOf(
                infixN,
                infixL,
                infixR,
                returnIdentity
            );
            
            return Map(
                (x, f) => f(x),
                pTerm,
                op
            );
        }

        private static Parser<TToken, Partial<T>> Op<TToken, T>(Parser<TToken, T> pTerm, IEnumerable<Parser<TToken, Func<T, T, T>>> ops)
            => Map(
                (f, y) => new Partial<T>(f, y),
                OneOf(ops),
                pTerm
            );

        private struct Partial<T>
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