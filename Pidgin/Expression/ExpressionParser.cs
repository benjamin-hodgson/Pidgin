using System.Collections.Generic;
using System.Linq;

namespace Pidgin.Expression
{
    /// <summary>
    /// Contains tools for parsing expression languages with associative infix operators.
    /// </summary>
    // TODO: needz moar tests
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
        public static Parser<TToken, T> Build2<TToken, T>(
            Parser<TToken, T> term,
            IEnumerable<IEnumerable<OperatorTableRow<TToken, T>>> operatorTable
        ) => Build2(
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
        ) => operatorTable.Aggregate(term, (tm, row) => row.Build(tm));
        public static Parser<TToken, T> Build2<TToken, T>(
            Parser<TToken, T> term,
            IEnumerable<OperatorTableRow<TToken, T>> operatorTable
        ) => operatorTable.Aggregate(term, (tm, row) => row.Build2(tm));
    }
}