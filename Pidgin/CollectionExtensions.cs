using System;
using System.Collections;
using System.Collections.Generic;

namespace Pidgin
{
    internal static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            if (collection is List<T> l)
            {
                l.AddRange(items);
                return;
            }
            foreach (var i in items)
            {
                collection.Add(i);
            }
        }

        public static ICollection<T> Empty<T>() => EmptyCollection<T>.Instance;

        private class EmptyCollection<T> : ICollection<T>
        {
            public static ICollection<T> Instance { get; } = new EmptyCollection<T>();

            private EmptyCollection() {}

            public int Count => 0;

            public bool IsReadOnly => false;

            public void Add(T item) {}

            public bool Remove(T item) => false;

            public void Clear() {}

            public bool Contains(T item) => false;

            public void CopyTo(T[] array, int arrayIndex)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }
                if (arrayIndex < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                }
            }

            public IEnumerator<T> GetEnumerator() => Enumerator.Instance;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private class Enumerator : IEnumerator<T>
            {
                public static IEnumerator<T> Instance { get; } = new Enumerator();

                private Enumerator() {}

                public T Current => default!;
                public bool MoveNext() => false;

                object IEnumerator.Current => null!;

                public void Dispose() {}
                public void Reset() {}
            }
        }
    }
}