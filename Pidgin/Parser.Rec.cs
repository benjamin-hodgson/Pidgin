using System;
using System.Collections.Immutable;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser which lazily calls the supplied function and applies the resulting parser.
        /// This is primarily useful to allow mutual recursion in parsers.
        /// <seealso cref="Rec{TToken,T}(Lazy{Parser{TToken, T}})"/>
        /// </summary>
        /// <param name="parser">A function which returns a parser</param>
        /// <typeparam name="TToken">The type of tokens in the parser's input stream</typeparam>
        /// <typeparam name="T">The return type of the parser</typeparam>
        /// <returns>A parser which lazily calls the supplied function and applies the resulting parser</returns>
        /// <example>
        /// This example shows how to use mutual recursion to create a parser equivalent to <see cref="Parser{TToken, T}.Many()"/>
        /// <code>
        /// // many is equivalent to String("foo").Separated(Char(' '))
        /// Parser&lt;char, string&gt; separator = null;
        /// var many = String("foo").Then(Rec(() => separator).Optional(), (x, y) => x + y.GetValueOrDefault(""));
        /// separator = Char(' ').Then(Rec(() => many));
        /// </code>
        /// </example>
        public static Parser<TToken, T> Rec<TToken, T>(Func<Parser<TToken, T>> parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }
            return Rec(new Lazy<Parser<TToken, T>>(parser));
        }

        /// <summary>
        /// Creates a parser which lazily calls the supplied function and applies the resulting parser.
        /// This is primarily useful to allow mutual recursion in parsers.
        /// <seealso cref="Rec{TToken,T}(Func{Parser{TToken, T}})"/>
        /// </summary>
        /// <param name="parser">A lazy parser value</param>
        /// <typeparam name="TToken">The type of tokens in the parser's input stream</typeparam>
        /// <typeparam name="T">The return type of the parser</typeparam>
        /// <returns>A parser which lazily applies the specified parser</returns>
        public static Parser<TToken, T> Rec<TToken, T>(Lazy<Parser<TToken, T>> parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }
            return new RecParser<TToken, T>(parser);
        }
        
        private sealed class RecParser<TToken, T> : Parser<TToken, T>
        {
            private readonly Lazy<Parser<TToken, T>> _lazy;

            public RecParser(Lazy<Parser<TToken, T>> lazy)
            {
                _lazy = lazy;
            }

            private protected override ImmutableSortedSet<Expected<TToken>> CalculateExpected()
                => ImmutableSortedSet.Create<Expected<TToken>>();

            internal sealed override InternalResult<T> Parse(ref ParseState<TToken> state)
                => _lazy.Value.Parse(ref state);
        }
    }
}