using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using LExpression = System.Linq.Expressions.Expression;

namespace Pidgin
{
    internal static class EnumerableExtensions
    {
        public static bool Equal<T>(ImmutableArray<T> left, ImmutableArray<T> right)
            => FastEqualInternal<T>.Go(left, right);

        private static bool SlowEqual<T>(ImmutableArray<T> left, ImmutableArray<T> right)
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

        // struct trick, inline Equals
        [SuppressMessage("CodeQuality", "IDE0051:Private member is unused", Justification = "Called through reflection")]
        private static bool FastEqual<T>(ImmutableArray<T> left, ImmutableArray<T> right)
            where T : struct, IEquatable<T>
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                var result = left[i].Equals(right[i]);
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
            => FastCompareInternal<T>.Go(left, right);

        private static int SlowCompare<T>(ImmutableArray<T> left, ImmutableArray<T> right)
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

        // struct trick, inline CompareTo
        [SuppressMessage("CodeQuality", "IDE0051:Private member is unused", Justification = "Called through reflection")]
        private static int FastCompare<T>(ImmutableArray<T> left, ImmutableArray<T> right)
            where T : struct, IComparable<T>
        {
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

                var result = left[i].CompareTo(right[i]);
                if (result != 0)
                {
                    return result;
                }
            }

            // Reached the end of collections => assume equal
            return 0;
        }

        private static class FastEqualInternal<T>
        {
            private static readonly Func<ImmutableArray<T>, ImmutableArray<T>, bool>? _callFastEqual = GetCallFastEqual();

            private static Func<ImmutableArray<T>, ImmutableArray<T>, bool>? GetCallFastEqual()
            {
                var type = typeof(T);
                var typeInfo = type.GetTypeInfo();

                var comparable = typeof(IEquatable<T>).GetTypeInfo();

                if (!typeInfo.IsValueType || !comparable.IsAssignableFrom(typeInfo))
                {
                    return null;
                }

                var immutableArray = typeof(ImmutableArray<T>);

                var method = typeof(EnumerableExtensions).GetTypeInfo().GetDeclaredMethod("FastEqual")!.MakeGenericMethod(type);

                var param1 = LExpression.Parameter(immutableArray);
                var param2 = LExpression.Parameter(immutableArray);
                var call = LExpression.Call(method, param1, param2);
                return LExpression.Lambda<Func<ImmutableArray<T>, ImmutableArray<T>, bool>>(call, param1, param2).Compile();
            }

            public static bool Go(ImmutableArray<T> left, ImmutableArray<T> right)
            {
                if (_callFastEqual != null)
                {
                    return _callFastEqual(left, right);
                }

                return SlowEqual(left, right);
            }
        }

        private static class FastCompareInternal<T>
        {
            private static readonly Comparison<ImmutableArray<T>>? _callFastCompare = GetCallFastCompare();

            private static Comparison<ImmutableArray<T>>? GetCallFastCompare()
            {
                var type = typeof(T);
                var typeInfo = type.GetTypeInfo();

                var comparable = typeof(IComparable<T>).GetTypeInfo();

                if (!typeInfo.IsValueType || !comparable.IsAssignableFrom(typeInfo))
                {
                    return null;
                }

                var immutableArray = typeof(ImmutableArray<T>);

                var method = typeof(EnumerableExtensions).GetTypeInfo().GetDeclaredMethod("FastCompare")!.MakeGenericMethod(type);

                var param1 = LExpression.Parameter(immutableArray);
                var param2 = LExpression.Parameter(immutableArray);
                var call = LExpression.Call(method, param1, param2);
                return LExpression.Lambda<Comparison<ImmutableArray<T>>>(call, param1, param2).Compile();
            }

            public static int Go(ImmutableArray<T> left, ImmutableArray<T> right)
            {
                if (_callFastCompare != null)
                {
                    return _callFastCompare(left, right);
                }

                return SlowCompare(left, right);
            }
        }
    }
}
