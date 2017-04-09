using System;
using System.Collections.Generic;
using System.Linq;
using static Pidgin.Parser;

namespace Pidgin.Expression
{
    /// <summary>
    /// Represents a row in a table of operators.
    /// Contains a collection of parsers for operators at a single precendence level.
    /// </summary>
    public sealed class OperatorTableRow<TToken, T>
    {
        /// <summary>
        /// A collection of parsers for the non-associative infix operators at this precedence level
        /// </summary>
        /// <returns>A collection of parsers for the non-associative infix operators at this precedence level</returns>
        public IEnumerable<Parser<TToken, Func<T, T, T>>> InfixNOps { get; }
        /// <summary>
        /// A collection of parsers for the left-associative infix operators at this precedence level
        /// </summary>
        /// <returns>A collection of parsers for the left-associative infix operators at this precedence level</returns>
        public IEnumerable<Parser<TToken, Func<T, T, T>>> InfixLOps { get; }
        /// <summary>
        /// A collection of parsers for the right-associative infix operators at this precedence level
        /// </summary>
        /// <returns>A collection of parsers for the right-associative infix operators at this precedence level</returns>
        public IEnumerable<Parser<TToken, Func<T, T, T>>> InfixROps { get; }
        /// <summary>
        /// A collection of parsers for the prefix operators at this precedence level
        /// </summary>
        /// <returns>A collection of parsers for the prefix operators at this precedence level</returns>
        public IEnumerable<Parser<TToken, Func<T, T>>> PrefixOps { get; }
        /// <summary>
        /// A collection of parsers for the postfix operators at this precedence level
        /// </summary>
        /// <returns>A collection of parsers for the postfix operators at this precedence level</returns>
        public IEnumerable<Parser<TToken, Func<T, T>>> PostfixOps { get; }

        /// <summary>
        /// Creates a row in a table of operators containing a collection of parsers for operators at a single precedence level.
        /// </summary>
        /// <param name="infixNOps">A collection of parsers for the non-associative infix operators at this precedence level</param>
        /// <param name="infixLOps">A collection of parsers for the left-associative infix operators at this precedence level</param>
        /// <param name="infixROps">A collection of parsers for the right-associative infix operators at this precedence level</param>
        /// <param name="prefixOps">A collection of parsers for the prefix operators at this precedence level</param>
        /// <param name="postfixOps">A collection of parsers for the postfix operators at this precedence level</param>
        public OperatorTableRow(
            IEnumerable<Parser<TToken, Func<T, T, T>>> infixNOps,
            IEnumerable<Parser<TToken, Func<T, T, T>>> infixLOps,
            IEnumerable<Parser<TToken, Func<T, T, T>>> infixROps,
            IEnumerable<Parser<TToken, Func<T, T>>> prefixOps,
            IEnumerable<Parser<TToken, Func<T, T>>> postfixOps
        )
        {
            InfixNOps = infixNOps ?? Enumerable.Empty<Parser<TToken, Func<T, T, T>>>();
            InfixLOps = infixLOps ?? Enumerable.Empty<Parser<TToken, Func<T, T, T>>>();
            InfixROps = infixROps ?? Enumerable.Empty<Parser<TToken, Func<T, T, T>>>();
            PrefixOps = prefixOps ?? Enumerable.Empty<Parser<TToken, Func<T, T>>>();
            PostfixOps = postfixOps ?? Enumerable.Empty<Parser<TToken, Func<T, T>>>();
        }

        /// <summary>
        /// An empty row in a table of operators
        /// </summary>
        /// <returns>An empty row in a table of operators</returns>
        public static OperatorTableRow<TToken, T> Empty { get; }
            = new OperatorTableRow<TToken, T>(null, null, null, null, null);
        
        /// <summary>
        /// Combine two rows at the same precedence level
        /// </summary>
        /// <param name="otherRow">A collection of parsers for operators</param>
        /// <returns>The current collection of parsers combined with <paramref name="otherRow"/></returns>
        public OperatorTableRow<TToken, T> And(OperatorTableRow<TToken, T> otherRow)
            => new OperatorTableRow<TToken, T>(
                InfixNOps.Concat(otherRow.InfixNOps),
                InfixLOps.Concat(otherRow.InfixLOps),
                InfixROps.Concat(otherRow.InfixROps),
                PrefixOps.Concat(otherRow.PrefixOps),
                PostfixOps.Concat(otherRow.PostfixOps)
            );

        internal Parser<TToken, T> Build(Parser<TToken, T> term)
            => new Builder(term, this).Build();

        private sealed class Builder
        {
            private static readonly IEnumerable<Parser<TToken, Func<T, T>>> _returnIdentity
                = new[]{ Parser<TToken>.Return<Func<T,T>>(x => x) };

            private readonly Parser<TToken, T> _term;
            private readonly OperatorTableRow<TToken, T> _row;

            public Builder(Parser<TToken, T> term, OperatorTableRow<TToken, T> row)
            {
                _term = term;
                _row = row;
            }

            public Parser<TToken, T> Build()
                => PTerm.Then(x => OneOf(
                    InfixN(x),
                    InfixL(x),
                    InfixR(x),
                    Parser<TToken>.Return(x)
                ));

            private Parser<TToken, T> _pTerm;
            private Parser<TToken, T> PTerm
            {
                get
                {
                    if (_pTerm == null)
                    {
                        _pTerm = Map(
                            (pre, tm, post) => post(pre(tm)),
                            OneOf(_row.PrefixOps.Concat(_returnIdentity)),
                            _term,
                            OneOf(_row.PostfixOps.Concat(_returnIdentity))
                        );
                    }
                    return _pTerm;
                }
            }
            
            private Parser<TToken, Func<T, T, T>> _infixNOp;
            private Parser<TToken, Func<T, T, T>> InfixNOp
            {
                get
                {
                    if (_infixNOp == null)
                    {
                        _infixNOp = OneOf(_row.InfixNOps);
                    }
                    return _infixNOp;
                }
            }
            private Parser<TToken, T> InfixN(T x)
                => Map(
                    (f, y) => f(x, y),
                    InfixNOp,
                    PTerm
                );

            private Parser<TToken, Func<T, T, T>> _infixLOp;
            private Parser<TToken, Func<T, T, T>> InfixLOp
            {
                get
                {
                    if (_infixLOp == null)
                    {
                        _infixLOp = OneOf(_row.InfixLOps);
                    }
                    return _infixLOp;
                }
            }
            private Func<T, Parser<TToken, T>> _infixLTail;
            private Func<T, Parser<TToken, T>> InfixLTail
            {
                get
                {
                    if (_infixLTail == null)
                    {
                        _infixLTail = r =>
                            InfixL(r)
                                .Or(Parser<TToken>.Return(r));
                    }
                    return _infixLTail;
                }
            }
            private Parser<TToken, T> InfixL(T x)
                => Map(
                    (f, y) => f(x, y),
                    InfixLOp,
                    PTerm
                ).Then(InfixLTail);

            private Parser<TToken, Func<T, T, T>> _infixROp;
            private Parser<TToken, Func<T, T, T>> InfixROp
            {
                get
                {
                    if (_infixROp == null)
                    {
                        _infixROp = OneOf(_row.InfixROps);
                    }
                    return _infixROp;
                }
            }
            private Func<T, Parser<TToken, T>> _infixRTail;
            private Func<T, Parser<TToken, T>> InfixRTail
            {
                get
                {
                    if (_infixRTail == null)
                    {
                        _infixRTail = t =>
                            InfixR(t)
                                .Or(Parser<TToken>.Return(t)
                        );
                    }
                    return _infixRTail;
                }
            }
            private Parser<TToken, T> InfixR(T x)
                => Map(
                    (f, y) => f(x, y),
                    InfixROp,
                    PTerm.Then(InfixRTail)
                );
        }
    }
}