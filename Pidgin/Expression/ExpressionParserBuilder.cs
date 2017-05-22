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
        private readonly Parser<TToken, Func<T, T, T>> _infixLOp;
        private readonly Parser<TToken, Func<T, T, T>> _infixROp;
        private readonly Parser<TToken, T> _infixRTail;
        private readonly Parser<TToken, IEnumerable<FuncAndArg>> _lOpSequence;
        private Parser<TToken, IEnumerable<FuncAndArg>> _rOpSequence;

        public ExpressionParserBuilder(Parser<TToken, T> term, OperatorTableRow<TToken, T> row)
        {
            _pTerm = Map(
                (pre, tm, post) => post(pre(tm)),
                OneOf(row.PrefixOps.Concat(_returnIdentity)),
                term,
                OneOf(row.PostfixOps.Concat(_returnIdentity))
            );
            _infixNOp = OneOf(row.InfixNOps);
            _infixLOp = OneOf(row.InfixLOps);
            _infixROp = OneOf(row.InfixROps);
            _infixRTail = _pTerm.Then(t => InfixR(t).Or(Parser<TToken>.Return(t)));
            _lOpSequence = Map(
                (f, y) => new FuncAndArg(f, y),
                _infixLOp,
                _pTerm
            ).AtLeastOnce();
            _rOpSequence = Map(
                (f, y) => new FuncAndArg(f, y),
                _infixROp,
                _pTerm
            ).AtLeastOnce();
        }

        public Parser<TToken, T> Build()
            => _pTerm.Then(x => OneOf(
                InfixN(x),
                InfixL(x),
                InfixR(x),
                Parser<TToken>.Return(x)
            ));
        public Parser<TToken, T> Build2()
            => _pTerm.Then(x => OneOf(
                InfixN(x),
                InfixL2(x),
                InfixR2(x),
                Parser<TToken>.Return(x)
            ));

        
        private Parser<TToken, T> InfixN(T x)
            => Map(
                (f, y) => f(x, y),
                _infixNOp,
                _pTerm
            );

        private Parser<TToken, T> InfixL(T x)
            => Map(
                (f, y) => f(x, y),
                _infixLOp,
                _pTerm
            ).Then(r => InfixL(r).Or(Parser<TToken>.Return(r)));

        private Parser<TToken, T> InfixL2(T x)
            => _lOpSequence.Select(
                fxs => fxs.Aggregate(x, (z, fx) => fx.Func(z, fx.Arg))
            );

        private Parser<TToken, T> InfixR(T x)
            => Map(
                (f, y) => f(x, y),
                _infixROp,
                _infixRTail
            );

        private Parser<TToken, T> InfixR2(T x)
            => _rOpSequence.Select(fxs => {
                var reassociated = fxs is IList<FuncAndArg>
                    ? new List<FuncAndArg>(((IList<FuncAndArg>)fxs).Count)
                    : new List<FuncAndArg>();
                foreach (var fx in fxs)
                {
                    reassociated.Add(new FuncAndArg(fx.Func, x));
                    x = fx.Arg;
                }
                reassociated.Add(new FuncAndArg((y, _) => y, x));
                reassociated.Reverse();
                return reassociated.Aggregate(default(T), (z, fx) => fx.Func(fx.Arg, z));
            });

        private struct FuncAndArg
        {
            public Func<T, T, T> Func { get; }
            public T Arg { get; }
            public FuncAndArg(Func<T, T, T> func, T arg)
            {
                Func = func;
                Arg = arg;
            }
        }
    }
}