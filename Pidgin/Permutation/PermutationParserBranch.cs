using System;

namespace Pidgin.Permutation
{
    internal abstract class PermutationParserBranch<TToken, T>
    {
        public abstract PermutationParserBranch<TToken, (T, U)> Add<U>(Parser<TToken, U> parser);

        public abstract PermutationParserBranch<TToken, (T, U)> AddOptional<U>(Parser<TToken, U> parser, Func<U> defaultValueFactory);

        public abstract PermutationParserBranch<TToken, R> Select<R>(Func<T, R> func);

        public abstract Parser<TToken, T> Build();
    }
    internal sealed class PermutationParserBranchImpl<TToken, U, T, R> : PermutationParserBranch<TToken, R>
    {
        private readonly Parser<TToken, U> _parser;
        private readonly PermutationParser<TToken, T> _perm;
        private readonly Func<U, T, R> _func;

        public PermutationParserBranchImpl(Parser<TToken, U> parser, PermutationParser<TToken, T> perm, Func<U, T, R> func)
        {
            _parser = parser;
            _perm = perm;
            _func = func;
        }

        public override PermutationParserBranch<TToken, (R, V)> Add<V>(Parser<TToken, V> parser)
            => Add(p => p.Add(parser));

        public override PermutationParserBranch<TToken, (R, V)> AddOptional<V>(Parser<TToken, V> parser, Func<V> defaultValueFactory)
            => Add(p => p.AddOptional(parser, defaultValueFactory));

        private PermutationParserBranch<TToken, (R, V)> Add<V>(Func<PermutationParser<TToken, T>, PermutationParser<TToken, (T, V)>> addPerm)
        {
            var this_func = _func;
            return new PermutationParserBranchImpl<TToken, U, (T, V), (R, V)>(
                _parser,
                addPerm(_perm),
                (u, tv) => (this_func(u, tv.Item1), tv.Item2)
            );
        }

        public override Parser<TToken, R> Build()
            => Parser.Map(_func, _parser, _perm.Build());

        public override PermutationParserBranch<TToken, V> Select<V>(Func<R, V> func)
        {
            var this_func = _func;
            return new PermutationParserBranchImpl<TToken, U, T, V>(
                _parser,
                _perm,
                (x, y) => func(this_func(x, y))
            );
        }
    }
}