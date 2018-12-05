using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin
{
    internal static class ExpectedUtil
    {
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