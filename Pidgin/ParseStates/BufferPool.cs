using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Pidgin.ParseStates
{
    internal static class BufferPool<T>
    {
        // invariant: _bags[i].All(b => 2^(i+4) <= b.Length < 2^(i+5))
        // TODO: what is a good number of arrays of each size to keep hold of?
        //       what is a good maximum array length to keep hold of?
        private static readonly BlockingCollection<T[]>[] _bags
            = Enumerable
                .Range(4, 6)
                .Select(_ => new BlockingCollection<T[]>(new ConcurrentBag<T[]>(), 1))
                .ToArray();
        
        public static T[] Acquire(int minimumSize)
        {
            var exp = Convert.ToInt32(Math.Ceiling(Math.Log(minimumSize, 2)));
            var arraySize = Convert.ToInt32(Math.Pow(2, exp));
            var ix = exp - 4;
            if (ix >= _bags.Length || ix < 0)
            {
                return new T[arraySize];
            }

            var success = _bags[ix].TryTake(out T[] result);
            if (success)
            {
                return result;
            }
            return new T[arraySize];
        }

        // You bloody well better not keep a
        // reference to the array after freeing it
        public static void Release(T[] array)
        {
            var ix = Convert.ToInt32(Math.Floor(Math.Log(array.Length, 2))) - 4;
            if (ix >= _bags.Length || ix < 0)
            {
                return;
            }
            // if it can't be added (because the bag is full),
            // oh well, it can be GCed
            _bags[ix].TryAdd(array);
        }
    }
}