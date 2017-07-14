using System.Collections.Generic;
using System.Linq;

namespace Pidgin
{
    internal static class ExpectedUtil<TToken>
    {
        public static SortedSet<Expected<TToken>> Empty { get; }
            // you bloody well better not mutate this
            = new SortedSet<Expected<TToken>>();
        /// <summary>
        /// For parsers which don't expect anything (eg Return(...))
        /// </summary>
        public static SortedSet<Expected<TToken>> Nil { get; }
            // you bloody well better not mutate this
            = new SortedSet<Expected<TToken>> { new Expected<TToken>(Enumerable.Empty<TToken>()) };
    }
    internal static class ExpectedUtil
    {
        public static SortedSet<T> Union<T>(params SortedSet<T>[] input)
            => Union(input.AsEnumerable());
        public static SortedSet<T> Union<T>(IEnumerable<SortedSet<T>> input)
        {
            var s = new SortedSet<T>();
            foreach (var x in input)
            {
                // this does a merge sort when the argument is also a SortedSet:
                // https://github.com/dotnet/corefx/blob/d467d43109d339f24edd623088d36a66bcc670ec/src/System.Collections/src/System/Collections/Generic/SortedSet.cs#L942
                s.UnionWith(x);
            }
            return s;
        }

        public static SortedSet<Expected<TToken>> Concat<TToken>(params IEnumerable<Expected<TToken>>[] sets)
            => Concat(sets.AsEnumerable());
        public static SortedSet<Expected<TToken>> Concat<TToken>(IEnumerable<IEnumerable<Expected<TToken>>> sets)
            => new SortedSet<Expected<TToken>>(sets.Aggregate((z, s) => z.SelectMany(_ => s, ConcatExpected)));
        private static Expected<TToken> ConcatExpected<TToken>(Expected<TToken> left, Expected<TToken> right)
        {
            if (left.Tokens?.Count() == 0)
            {
                return right;
            }
            if (left.Tokens != null && right.Tokens != null)
            {
                return new Expected<TToken>(left.Tokens.Concat(right.Tokens));
            }
            return left;
        }
    }
}