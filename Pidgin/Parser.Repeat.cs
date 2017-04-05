using System.Collections.Generic;
using System.Linq;

namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser which applies the current parser <paramref name="count"/> times.
        /// </summary>
        /// <param name="count">The number of times to apply the current parser</param>
        /// <exception cref="System.InvalidOperationException"><paramref name="count"/> is less than 0</exception>
        /// <returns>A parser which applies the current parser <paramref name="count"/> times.</returns>
        public Parser<TToken, IEnumerable<T>> Repeat(int count)
            => Parser<TToken>.Sequence(Enumerable.Repeat(this, count));
    }
}