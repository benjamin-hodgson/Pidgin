using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin.ParseStates;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser that parses and returns a literal string
        /// </summary>
        /// <param name="str">The string to parse</param>
        /// <returns>A parser that parses and returns a literal string</returns>
        public static Parser<char, string> String(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }
            return Parser<char>.Sequence<string>(str);
        }

        /// <summary>
        /// Creates a parser that parses and returns a literal string, in a case insensitive manner.
        /// The parser returns the actual string parsed.
        /// </summary>
        /// <param name="str">The string to parse</param>
        /// <returns>A parser that parses and returns a literal string, in a case insensitive manner.</returns>
        public static Parser<char, string> CIString(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }
            return new CIStringParser(str);
        }
        private sealed class CIStringParser : Parser<char, string>
        {
            private readonly string _value;

            public CIStringParser(string value)
                : base(new SortedSet<Expected<char>> { new Expected<char>(value.ToCharArray()) })
            {
                _value = value.ToLowerInvariant();
            }

            internal sealed override InternalResult<string> Parse(IParseState<char> state)
            {
                var consumedInput = false;

                var builder = new PooledStringBuilder(_value.Length);

                foreach (var c in _value)
                {
                    var result = state.Peek();
                    if (!result.HasValue)
                    {
                        state.Error = new ParseError<char>(
                            result,
                            true,
                            Expected,
                            state.SourcePos,
                            null
                        );
                        return InternalResult.Failure<string>(consumedInput);
                    }

                    var token = result.GetValueOrDefault();
                    if (char.ToLowerInvariant(token) != c)
                    {
                        state.Error = new ParseError<char>(
                            result,
                            false,
                            Expected,
                            state.SourcePos,
                            null
                        );
                        return InternalResult.Failure<string>(consumedInput);
                    }

                    consumedInput = true;
                    builder.Append(token);
                    state.Advance();
                }
                return InternalResult.Success<string>(builder.GetStringAndClear(), consumedInput);
            }
        }
    }
}