using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser that parses and returns a literal string
        /// </summary>
        /// <param name="str">The string to parse</param>
        /// <returns>A parser that parses and returns a literal string</returns>
        /// 
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
            private Expected<char> _expected;
            private Expected<char> Expected
            {
                get
                {
                    if (_expected.InternalTokens.IsDefault)
                    {
                        _expected = new Expected<char>(_value.ToImmutableArray());
                    }
                    return _expected;
                }
            }

            public CIStringParser(string value)
            {
                _value = value;
            }

            internal sealed override InternalResult<string> Parse(ref ParseState<char> state)
            {
                var consumedInput = false;

                var builder = new InplaceStringBuilder(_value.Length);

                foreach (var c in _value)
                {
                    if (!state.HasCurrent)
                    {
                        state.Error = new InternalError<char>(
                            Maybe.Nothing<char>(),
                            true,
                            state.Location,
                            null
                        );
                        state.AddExpected(Expected);
                        return InternalResult.Failure<string>(consumedInput);
                    }

                    var token = state.Current;
                    if (char.ToLowerInvariant(token) != char.ToLowerInvariant(c))
                    {
                        state.Error = new InternalError<char>(
                            Maybe.Just(token),
                            false,
                            state.Location,
                            null
                        );
                        state.AddExpected(Expected);
                        return InternalResult.Failure<string>(consumedInput);
                    }

                    consumedInput = true;
                    builder.Append(token);
                    state.Advance();
                }
                return InternalResult.Success<string>(builder.ToString(), consumedInput);
            }
        }
    }
}