using System;
using System.Collections.Immutable;

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
    }

    internal sealed class CIStringParser : Parser<char, string>
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

        public sealed override InternalResult<string> Parse(ref ParseState<char> state)
        {
            var span = state.LookAhead(_value.Length);  // span.Length <= _valueTokens.Length

            var errorPos = -1;
            for (var i = 0; i < span.Length; i++)
            {
                if (!char.ToLowerInvariant(span[i]).Equals(char.ToLowerInvariant(_value[i])))
                {
                    errorPos = i;
                    break;
                }
            }

            if (errorPos != -1)
            {
                // strings didn't match
                state.Advance(errorPos);
                state.Error = new InternalError<char>(
                    Maybe.Just(span[errorPos]),
                    false,
                    state.Location,
                    null
                );
                state.AddExpected(Expected);
                return InternalResult.Failure<string>(errorPos > 0);
            }

            if (span.Length < _value.Length)
            {
                // strings matched but reached EOF
                state.Advance(span.Length);
                state.Error = new InternalError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    state.Location,
                    null
                );
                state.AddExpected(Expected);
                return InternalResult.Failure<string>(span.Length > 0);
            }

            // OK
            state.Advance(_value.Length);
            return InternalResult.Success<string>(span.ToString(), _value.Length > 0);
        }
    }
}
