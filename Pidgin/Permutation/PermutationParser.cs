using System;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin.Permutation
{
    public static class PermutationParser
    {
        public static PermutationParser<TToken, object> Create<TToken>()
            => PermutationParser<TToken>.Create();

        public static Parser<TToken, R> Map<TToken, T1, T2, T3, R>(Func<T1, T2, T3, R> func, Permutable<TToken, T1> parser1, Permutable<TToken, T2> parser2, Permutable<TToken, T3> parser3)
            => parser3.AddTo(parser2.AddTo(parser1.AddTo(Create<TToken>())))
                .Build()
                .Select(args => func(args.Item1.Item1.Item2, args.Item1.Item2, args.Item2));
    }

    public static class PermutationParser<TToken>
    {
        private static readonly object _object = new object();

        public static PermutationParser<TToken, object> Create()
            => new PermutationParser<TToken, object>(
                ImmutableList.Create<PermutationParserTree<TToken, object>>(
                    new PermutationParserLeaf<TToken, object>(() => _object)
                )
            );
    }

    public sealed class PermutationParser<TToken, T>
    {
        private readonly ImmutableList<PermutationParserTree<TToken, T>> _forest;

        internal PermutationParser(ImmutableList<PermutationParserTree<TToken, T>> forest)
        {
            _forest = forest;
        }

        public Parser<TToken, T> Build()
            => Parser.OneOf(_forest.Select(t => t.Build()));

        public PermutationParser<TToken, (T, U)> Add<U>(Parser<TToken, U> parser)
            => new PermutationParser<TToken, (T, U)>(
                _forest
                    .ConvertAll<PermutationParserTree<TToken, (T, U)>>(t => t.Add(parser))
                    .Add(new PermutationParserBranch<TToken, U, (T, U)>(
                        parser,
                        Extend<U>()
                    ))
            );

        public PermutationParser<TToken, (T, U)> AddOptional<U>(Parser<TToken, U> parser, Func<U> defaultValueFactory)
        {
            throw new NotImplementedException();
        }

        public PermutationParser<TToken, U> Select<U>(Func<T, U> func)
            => ConvertAll(t => t.Select(func));
        
        internal PermutationParser<TToken, Func<U, (T, U)>> Extend<U>()
            => ConvertAll(t => t.Extend<U>());

        private PermutationParser<TToken2, U> ConvertAll<TToken2, U>(Func<PermutationParserTree<TToken, T>, PermutationParserTree<TToken2, U>> func)
            => new PermutationParser<TToken2, U>(_forest.ConvertAll(func));
    }
}