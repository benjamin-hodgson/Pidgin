using System;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin.Permutation
{
    public static class PermutationParser
    {
        public static PermutationParser<TToken, ValueTuple> Create<TToken>()
            => PermutationParser<TToken>.Create();

        public static Parser<TToken, R> Map<TToken, T1, T2, T3, R>(Func<T1, T2, T3, R> func, Permutable<TToken, T1> parser1, Permutable<TToken, T2> parser2, Permutable<TToken, T3> parser3)
            => parser3.AddTo(parser2.AddTo(parser1.AddTo(Create<TToken>())))
                .Build()
                .Select(args => func(args.Item1.Item1.Item2, args.Item1.Item2, args.Item2));
    }

    public static class PermutationParser<TToken>
    {
        public static PermutationParser<TToken, ValueTuple> Create()
            => new PermutationParser<TToken, ValueTuple>(
                ValueTuple.Create,
                ImmutableList.Create<PermutationParserBranch<TToken, ValueTuple>>()
            );
    }

    public sealed class PermutationParser<TToken, T>
    {
        private readonly Func<T> _exit;
        private readonly ImmutableList<PermutationParserBranch<TToken, T>> _forest;

        internal PermutationParser(Func<T> exit, ImmutableList<PermutationParserBranch<TToken, T>> forest)
        {
            _exit = exit;
            _forest = forest;
        }

        public Parser<TToken, T> Build()
        {
            var forest = Parser.OneOf(_forest.Select(t => t.Build()));
            if (_exit != null)
            {
                return forest.Or(Parser<TToken>.Return(_exit).Select(f => f()));
            }
            return forest;
        }

        public PermutationParser<TToken, (T, U)> Add<U>(Parser<TToken, U> parser)
            => new PermutationParser<TToken, (T, U)>(
                null,
                ConvertForestAndAddParser(b => b.Add(parser), parser)
            );

        public PermutationParser<TToken, (T, U)> AddOptional<U>(Parser<TToken, U> parser, U defaultValue)
            => AddOptional(parser, () => defaultValue);

        public PermutationParser<TToken, (T, U)> AddOptional<U>(Parser<TToken, U> parser, Func<U> defaultValueFactory)
        {
            var this_exit = _exit;
            return new PermutationParser<TToken, (T, U)>(
                _exit == null ? null as Func<(T, U)> : () => (_exit(), defaultValueFactory()),
                ConvertForestAndAddParser(b => b.AddOptional(parser, defaultValueFactory), parser)
            );
        }

        private ImmutableList<PermutationParserBranch<TToken, (T, U)>> ConvertForestAndAddParser<U>(
            Func<PermutationParserBranch<TToken, T>, PermutationParserBranch<TToken, (T, U)>> func,
            Parser<TToken, U> parser
        ) => _forest
            .ConvertAll(func)
            .Add(new PermutationParserBranchImpl<TToken, U, T, (T, U)>(parser, this, (u, t) => (t, u)));

        public PermutationParser<TToken, U> Select<U>(Func<T, U> func)
        {
            var this_exit = _exit;
            return new PermutationParser<TToken, U>(
                this_exit == null ? null as Func<U> : () => func(this_exit()),
                _forest.ConvertAll(t => t.Select(func))
            );
        }
    }
}