using System;

namespace Pidgin.Permutation
{
    public static class Permutable
    {
        public static Permutable<TToken, T> Create<TToken, T>(Parser<TToken, T> parser)
            => new Permutable<TToken, T>(parser, null);
    }
    public sealed class Permutable<TToken, T>
    {
        private Parser<TToken, T> _parser;
        private Func<T> _defaultValueFactory;
        private bool IsOptional => _defaultValueFactory != null;

        internal Permutable(Parser<TToken, T> parser, Func<T> defaultValueFactory)
        {
            _parser = parser;
            _defaultValueFactory = defaultValueFactory;
        }

        internal PermutationParser<TToken, (U, T)> AddTo<U>(PermutationParser<TToken, U> permutationParser)
            => IsOptional
                ? permutationParser.AddOptional(_parser, _defaultValueFactory)
                : permutationParser.Add(_parser);
    }
}