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
        private readonly Parser<TToken, Partial> _nOp;
        private readonly Parser<TToken, Partial> _lOp;
        private readonly Parser<TToken, Partial> _infixR;

        public ExpressionParserBuilder(Parser<TToken, T> term, OperatorTableRow<TToken, T> row)
        {
            _pTerm = Map(
                (pre, tm, post) => post(pre(tm)),
                OneOf(row.PrefixOps.Concat(_returnIdentity)),
                term,
                OneOf(row.PostfixOps.Concat(_returnIdentity))
            );
            _nOp = Op(row.InfixNOps);
            _lOp = Op(row.InfixLOps);
            _infixR = Op(row.InfixROps)
                .AtLeastOnce()
                .Select(fxs =>
                    fxs.AggregateR(
                        new Partial((y, _) => y, default(T)),
                        (fx, p) => new Partial(fx.Func, p.Apply(fx.Arg))
                    )
                );
        }

        public Parser<TToken, T> Build()
            => _pTerm.Then(x => OneOf(
                InfixN(x),
                InfixL(x),
                InfixR(x),
                Parser<TToken>.Return(x)
            ));

        
        private Parser<TToken, T> InfixN(T x)
            => _nOp.Select(p => p.Apply(x));

        private Parser<TToken, T> InfixL(T x)
            => _lOp.ChainAtLeastOnceL(
                () => x,
                (z, fx) => fx.Apply(z)
            );

        private Parser<TToken, T> InfixR(T x)
            => _infixR.Select(p => p.Apply(x));
        
        private Parser<TToken, Partial> Op(IEnumerable<Parser<TToken, Func<T, T, T>>> ops)
            => Map(
                (f, y) => new Partial(f, y),
                OneOf(ops),
                _pTerm
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
            public T Apply(T arg) => Func(arg, Arg);
        }
    }
}