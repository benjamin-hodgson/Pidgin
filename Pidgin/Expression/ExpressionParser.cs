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
            IEnumerable<OperatorTableRow<TToken, T>> operatorTable
        ) => operatorTable.Aggregate(term, Build);

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
        ) => Build(term, Flatten(operatorTable));

        /// <summary>
        /// Builds a parser for expressions built from the operators in <paramref name="operatorTable"/>.
        /// The operator table is a sequence of operators in precedence order:
        /// the operators in the first row have the highest precedence and operators in later rows have lower precedence.
        /// 
        /// This overload is useful for recursive expressions (for example, languages with parenthesised subexpressions).
        /// <paramref name="termFactory"/>'s argument will be a parser which parses a full subexpression.
        /// </summary>
        /// <param name="termFactory">A function which produces a parser for a single term</param>
        /// <param name="operatorTable">A table of operators</param>
        /// <returns>A parser for expressions built from the operators in <paramref name="operatorTable"/>.</returns>
        public static Parser<TToken, T> Build<TToken, T>(
            Func<Parser<TToken, T>, Parser<TToken, T>> termFactory,
            IEnumerable<OperatorTableRow<TToken, T>> operatorTable
        )
        {
            Parser<TToken, T>? expr = null;
            var term = termFactory(Rec(() => expr!));
            expr = Build(term, operatorTable);
            return expr;
        }
        
        /// <summary>
        /// Builds a parser for expressions built from the operators in <paramref name="operatorTable"/>.
        /// The operator table is a sequence of operators in precedence order:
        /// the operators in the first row have the highest precedence and operators in later rows have lower precedence.
        /// 
        /// This overload is useful for recursive expressions (for example, languages with parenthesised subexpressions).
        /// <paramref name="termFactory"/>'s argument will be a parser which parses a full subexpression.
        /// </summary>
        /// <param name="termFactory">A function which produces a parser for a single term</param>
        /// <param name="operatorTable">A table of operators</param>
        /// <returns>A parser for expressions built from the operators in <paramref name="operatorTable"/>.</returns>
        public static Parser<TToken, T> Build<TToken, T>(
            Func<Parser<TToken, T>, Parser<TToken, T>> termFactory,
            IEnumerable<IEnumerable<OperatorTableRow<TToken, T>>> operatorTable
        ) => Build(termFactory, Flatten(operatorTable));

        /// <summary>
        /// Builds a parser for expressions built from the operators in <paramref name="operatorTableFactory"/>'s result.
        /// The operator table is a sequence of operators in precedence order:
        /// the operators in the first row have the highest precedence and operators in later rows have lower precedence.
        /// 
        /// This overload is useful for recursive expressions (for example, languages with parenthesised subexpressions).
        /// <paramref name="operatorTableFactory"/>'s argument will be a parser which parses a full subexpression.
        /// </summary>
        /// <param name="term">A parser for a single term in an expression language</param>
        /// <param name="operatorTableFactory">A function which produces a table of operators</param>
        /// <returns>A parser for expressions built from the operators in the operator table</returns>
        public static Parser<TToken, T> Build<TToken, T>(
            Parser<TToken, T> term,
            Func<Parser<TToken, T>, IEnumerable<OperatorTableRow<TToken, T>>> operatorTableFactory
        )
        {
            Parser<TToken, T>? expr = null;
            var operatorTable = operatorTableFactory(Rec(() => expr!));
            expr = Build(term, operatorTable);
            return expr;
        }

        /// <summary>
        /// Builds a parser for expressions built from the operators in <paramref name="operatorTableFactory"/>'s result.
        /// The operator table is a sequence of operators in precedence order:
        /// the operators in the first row have the highest precedence and operators in later rows have lower precedence.
        /// 
        /// This overload is useful for recursive expressions (for example, languages with parenthesised subexpressions).
        /// <paramref name="operatorTableFactory"/>'s argument will be a parser which parses a full subexpression.
        /// </summary>
        /// <param name="term">A parser for a single term in an expression language</param>
        /// <param name="operatorTableFactory">A function which produces a table of operators</param>
        /// <returns>A parser for expressions built from the operators in the operator table</returns>
        public static Parser<TToken, T> Build<TToken, T>(
            Parser<TToken, T> term,
            Func<Parser<TToken, T>, IEnumerable<IEnumerable<OperatorTableRow<TToken, T>>>> operatorTableFactory
        )
        {
            Parser<TToken, T>? expr = null;
            var operatorTable = operatorTableFactory(Rec(() => expr!));
            expr = Build(term, operatorTable);
            return expr;
        }

        /// <summary>
        /// Builds a parser for expressions built from the operators in <paramref name="termAndOperatorTableFactory"/>'s second result.
        /// The operator table is a sequence of operators in precedence order:
        /// the operators in the first row have the highest precedence and operators in later rows have lower precedence.
        /// 
        /// This overload is useful for recursive expressions (for example, languages with parenthesised subexpressions).
        /// <paramref name="termAndOperatorTableFactory"/>'s argument will be a parser which parses a full subexpression.
        /// </summary>
        /// <param name="termAndOperatorTableFactory">A function which produces a parser for a single term and a table of operators</param>
        /// <returns>A parser for expressions built from the operators in the operator table</returns>
        public static Parser<TToken, T> Build<TToken, T>(
            Func<Parser<TToken, T>, (Parser<TToken, T> term, IEnumerable<OperatorTableRow<TToken, T>> operatorTable)> termAndOperatorTableFactory
        )
        {
            Parser<TToken, T>? expr = null;
            var (term, operatorTable) = termAndOperatorTableFactory(Rec(() => expr!));
            expr = Build(term, operatorTable);
            return expr;
        }

        /// <summary>
        /// Builds a parser for expressions built from the operators in <paramref name="termAndOperatorTableFactory"/>'s second result.
        /// The operator table is a sequence of operators in precedence order:
        /// the operators in the first row have the highest precedence and operators in later rows have lower precedence.
        /// 
        /// This overload is useful for recursive expressions (for example, languages with parenthesised subexpressions).
        /// <paramref name="termAndOperatorTableFactory"/>'s argument will be a parser which parses a full subexpression.
        /// </summary>
        /// <param name="termAndOperatorTableFactory">A function which produces a parser for a single term and a table of operators</param>
        /// <returns>A parser for expressions built from the operators in the operator table</returns>
        public static Parser<TToken, T> Build<TToken, T>(
            Func<Parser<TToken, T>, (Parser<TToken, T> term, IEnumerable<IEnumerable<OperatorTableRow<TToken, T>>> operatorTable)> termAndOperatorTableFactory
        )
        {
            Parser<TToken, T>? expr = null;
            var (term, operatorTable) = termAndOperatorTableFactory(Rec(() => expr!));
            expr = Build(term, operatorTable);
            return expr;
        }


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
                        for (var i = 0; i < fxs.Count; i++)
                        {
                            z = fxs[i].ApplyL(z);
                        }
                        fxs.Dispose();
                        return z;
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
                        var partial = new Partial<T>((y, _) => y, default(T)!);
                        for (var i = fxs.Count - 1; i >= 0; i--)
                        {
                            var fx = fxs[i];
                            partial = new Partial<T>(fx.Func, partial.ApplyL(fx.Arg));
                        }
                        fxs.Dispose();
                        return z => partial.ApplyL(z);
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

        private static IEnumerable<OperatorTableRow<TToken, T>> Flatten<TToken, T>(IEnumerable<IEnumerable<OperatorTableRow<TToken, T>>> operatorTable)
            => operatorTable.Select(r => r.Aggregate(OperatorTableRow<TToken, T>.Empty, (p, q) => p.And(q)));

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