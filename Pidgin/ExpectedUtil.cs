using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin
{
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
        public static ImmutableSortedSet<T> Union<T>(ref PooledList<ImmutableSortedSet<T>> input)
        {
            var builder = ImmutableSortedSet.CreateBuilder<T>();
            for (var i = 0; i < input.Count; i++)
            {
                builder.UnionWith(input[i]);
            }
            return builder.ToImmutable();
        }
    }
}