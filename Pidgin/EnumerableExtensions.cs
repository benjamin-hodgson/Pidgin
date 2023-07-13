using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Pidgin;

internal static class EnumerableExtensions
{
    public static bool Equal<T>(ImmutableArray<T> left, ImmutableArray<T> right)
    {
        var comparer = EqualityComparer<T>.Default;

        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            var result = comparer.Equals(left[i], right[i]);
            if (!result)
            {
                return false;
            }
        }

        return true;
    }

    public static int GetHashCode<T>(ImmutableArray<T> arr)
    {
        if (arr.IsDefault)
        {
            return 0;
        }

        var hash = 17;
        foreach (var x in arr)
        {
            hash = (hash * 23) + x!.GetHashCode();
        }

        return hash;
    }

    public static int Compare<T>(ImmutableArray<T> left, ImmutableArray<T> right)
    {
        var comparer = Comparer<T>.Default;

        for (var i = 0; i < Math.Max(left.Length, right.Length); i++)
        {
            if (i >= left.Length)
            {
                // left is shorter than right
                return -1;
            }

            if (i >= right.Length)
            {
                // right is shorter than left
                return 1;
            }

            var result = comparer.Compare(left[i], right[i]);
            if (result != 0)
            {
                return result;
            }
        }

        // Reached the end of collections => assume equal
        return 0;
    }
}
