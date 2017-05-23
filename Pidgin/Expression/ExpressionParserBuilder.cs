using System;
using System.Collections.Generic;
using System.Linq;
using static Pidgin.Parser;

namespace Pidgin.Expression
{
    internal sealed class ExpressionParserBuilder<TToken, T>
    {
        private static readonly IEnumerable<Parser<TToken, Func<T, T>>> _returnIdentity
            = new[]{ Parser<TToken>.Return<Func<T,T>>(x => x) };

        private readonly Parser<TToken, T> _pTerm;
        private readonly Parser<TToken, Func<T, T, T>> _infixNOp;
        private readonly Parser<TToken, IEnumerable<Partial>> _lOpSequence;
        private readonly Parser<TToken, IEnumerable<Partial>> _rOpSequence;

        public ExpressionParserBuilder(Parser<TToken, T> term, OperatorTableRow<TToken, T> row)
        {
            _pTerm = Map(
                (pre, tm, post) => post(pre(tm)),
                OneOf(row.PrefixOps.Concat(_returnIdentity)),
                term,
                OneOf(row.PostfixOps.Concat(_returnIdentity))
            );
            _infixNOp = OneOf(row.InfixNOps);
            _lOpSequence = Associative(row.InfixLOps);
            _rOpSequence = Associative(row.InfixROps);
        }

        public Parser<TToken, T> Build()
            => _pTerm.Then(x => OneOf(
                InfixN(x),
                InfixL(x),
                InfixR(x),
                Parser<TToken>.Return(x)
            ));

        
        private Parser<TToken, T> InfixN(T x)
            => Map(
                (f, y) => f(x, y),
                _infixNOp,
                _pTerm
            );

        private Parser<TToken, T> InfixL(T x)
            => _lOpSequence.Select(
                fxs => fxs.Aggregate(x, (z, fx) => fx.Func(z, fx.Arg))
            );

        private Parser<TToken, T> InfixR(T x)
            => _rOpSequence.Select(fxs =>
                {
                    var reassociated = fxs is IList<Partial>
                        ? new List<Partial>(((IList<Partial>)fxs).Count)
                        : new List<Partial>();
                    foreach (var fx in fxs)
                    {
                        reassociated.Add(new Partial(fx.Func, x));
                        x = fx.Arg;
                    }
                    reassociated.Reverse();
                    return reassociated.Aggregate(x, (z, fx) => fx.Func(fx.Arg, z));
                }
            );
        
        private Parser<TToken, IEnumerable<Partial>> Associative(IEnumerable<Parser<TToken, Func<T, T, T>>> ops)
            => Map(
                (f, y) => new Partial(f, y),
                OneOf(ops),
                _pTerm
            ).AtLeastOnce();

        private struct Partial
        {
            public Func<T, T, T> Func { get; }
            public T Arg { get; }
            public Partial(Func<T, T, T> func, T arg)
            {
                Func = func;
                Arg = arg;
            }
        }
    }
}