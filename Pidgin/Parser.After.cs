using System;

namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser that applies the specified parser followed by the current parser.
        /// The resulting parser returns the result of the current parser, ignoring the result of the second parser.
        /// </summary>
        /// <param name="parser">The parser to apply before applying the current parser.</param>
        /// <typeparam name="U">The type of the value returned by the second parser</typeparam>
        /// <returns>A parser that applies the specified parser followed by the current parser</returns>
        public Parser<TToken, T> After<U>(Parser<TToken, U> parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }
            return parser.Then(this, (u, t) => t);
        }
    }
}