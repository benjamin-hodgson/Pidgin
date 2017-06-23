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
                fxs => fxs.Aggregate(x, (z, fx) => fx.Apply(z))
            );

        private Parser<TToken, T> InfixR(T x)
            => _rOpSequence.Select(fxs =>
                {
                    var list = fxs is IList<Partial> l ? l : fxs.ToList();
                    var p = new Partial((y, _) => y, default(T));
                    for (var i = list.Count - 1; i >= 0; i--)
                    {
                        var partial = list[i];
                        p = new Partial(partial.Func, p.Apply(partial.Arg));
                    }
                    return p.Apply(x);
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
            public T Apply(T arg) => Func(arg, Arg);
        }
    }
}