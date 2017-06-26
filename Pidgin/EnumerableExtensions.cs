using System;
using System.Collections.Generic;
using System.Linq;

namespace Pidgin
{
    internal static class EnumerableExtensions
    {
        public static U AggregateR<T, U>(this IEnumerable<T> input, U seed, Func<T, U, U> func)
        {
            var list = input is IList<T> l ? l : input.ToList();
            var z = seed;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                z = func(list[i], z);
            }
            return z;
        }

        public static int CompareTo<T>(this IEnumerable<T> left, IEnumerable<T> right)
        {
            // http://stackoverflow.com/a/18211470/1523776
            using (var e1 = left.GetEnumerator())
            using (var e2 = right.GetEnumerator())
            {
                int result;
                var comparer = Comparer<T>.Default;

                do
                {
                    bool gotFirst = e1.MoveNext();
                    bool gotSecond = e2.MoveNext();

                    // Reached the end of collections => assume equal
                    if (!gotFirst && !gotSecond)
                    {
                        return 0;
                    }

                    // Different sizes => treat collection of larger size as "greater"
                    if (gotFirst != gotSecond)
                    {
                        return gotFirst ? 1 : -1;
                    }

                    result = comparer.Compare(e1.Current, e2.Current);
                }
                while (result == 0);

                return result;
            }
        }
    }
}