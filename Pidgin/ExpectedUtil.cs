using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin
{
    internal static class ExpectedUtil<TToken>
    {
        /// <summary>
        /// For parsers which don't expect anything (eg Return(...))
        /// </summary>
        public static ImmutableSortedSet<Expected<TToken>> Nil { get; }
            = ImmutableSortedSet.Create(new Expected<TToken>(ImmutableList.Create<TToken>()));
    }
    internal static class ExpectedUtil
    {
        public static ImmutableSortedSet<T> Union<T>(params ImmutableSortedSet<T>[] input)
            => Union(input.AsEnumerable());
        public static ImmutableSortedSet<T> Union<T>(IEnumerable<ImmutableSortedSet<T>> input)
        {
            var builder = ImmutableSortedSet.CreateBuilder<T>();
            foreach (var set in input)
            {
                builder.UnionWith(set);
            }
            return builder.ToImmutable();
        }

        public static ImmutableSortedSet<Expected<TToken>> Concat<TToken>(params ImmutableSortedSet<Expected<TToken>>[] sets)
            => Concat(sets.AsEnumerable());
        public static ImmutableSortedSet<Expected<TToken>> Concat<TToken>(IEnumerable<IEnumerable<Expected<TToken>>> sets)
            => sets.Aggregate((z, s) => z.SelectMany(_ => s, ConcatExpected)).ToImmutableSortedSet();

        private static Expected<TToken> ConcatExpected<TToken>(Expected<TToken> left, Expected<TToken> right)
        {
            if (left.InternalTokens?.Count() == 0)
            {
                return right;
            }
            if (left.InternalTokens != null && right.InternalTokens != null)
            {
                return new Expected<TToken>(left.InternalTokens.AddRange(right.InternalTokens));
            }
            return left;
        }
    }
}