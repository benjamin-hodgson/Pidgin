#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Linq;

namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser which tries to apply a first specified parser, applying a second specified parser if the first one fails without consuming any input.
        /// The resulting parser fails if both the first parser and the second parser fail, or if the first parser fails after consuming input.
        /// </summary>
        /// <param name="x">The parser to apply first</param>
        /// <param name="y">The alternative parser to apply if <paramref name="x"/> parser fails without consuming any input</param>
        /// <returns>A parser which tries to apply <paramref name="x"/>, and then applies <paramref name="y"/> if <paramref name="x"/> fails without consuming any input.</returns>
        public static Parser<TToken, T> operator |(Parser<TToken, T> x, Parser<TToken, T> y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }
            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }
            return x.Or(y);
        }

        /// <summary>
        /// Creates a parser which applies specified parsers in left-to-right order.
        /// The resulting parser returns the result of the second parser, ignoring the result of the first one.
        /// </summary>
        public static Parser<TToken, T> operator >(Parser<TToken, T> x, Parser<TToken, T> y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }
            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }
            return x.Then(y);
        }

        /// <summary>
        /// Creates a parser which applies specified parsers in left-to-right order.
        /// The resulting parser returns the result of the first parser, ignoring the result of the second one.
        /// </summary>
        public static Parser<TToken, T> operator <(Parser<TToken, T> x, Parser<TToken, T> y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }
            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }
            return x.Before(y);
        }

        public static Parser<TToken, T> operator <(Parser<TToken, T> parser, TToken token)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }
            var tokenParser = Parser<TToken>.Token(token);
            return parser.Before(tokenParser);
        }

        public static Parser<TToken, T> operator >(Parser<TToken, T> parser, TToken token)
        {
            if (typeof(T) == typeof(TToken))
            {
                var tokenParser = Parser<TToken>.Token(token);
                return (Parser<TToken, T>)(object)parser.Then(tokenParser);
            }
            throw new NotSupportedException("[TBD] Supported only if `T == TToken`");
        }

        public static Parser<TToken, T> operator <(TToken token, Parser<TToken, T> parser)
        {
            return parser > token;
        }

        public static Parser<TToken, T> operator >(TToken token, Parser<TToken, T> parser)
        {
            return parser < token;
        }

        public static Parser<TToken, IEnumerable<T>> operator +(Parser<TToken, T> x, Parser<TToken, T> y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }
            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }            
            return Parser<TToken>.Sequence(x, y);
        }

        public static Parser<TToken, IEnumerable<T>> operator +(Parser<TToken, IEnumerable<T>> sequence, Parser<TToken, T> newElement)
        {
            if (sequence == null)
            {
                throw new ArgumentNullException(nameof(sequence));
            }
            if (newElement == null)
            {
                throw new ArgumentNullException(nameof(newElement));
            }
            if (sequence is SequenceParser<TToken, T> seqParser)
            {
                return new SequenceParser<TToken, T>(seqParser.Parsers.Append(newElement).ToArray());
            }
            throw new NotSupportedException("TODO");
        }

        public static Parser<TToken, Unit> operator !(Parser<TToken, T> parser)
        {
            return Parser.Not(parser);
        }
    }
}
