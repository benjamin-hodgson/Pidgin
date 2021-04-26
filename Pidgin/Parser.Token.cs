using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser that parses and returns a single token
        /// </summary>
        /// <param name="token">The token to parse</param>
        /// <returns>A parser that parses and returns a single token</returns>
        public static Parser<TToken, TToken> Token(TToken token)
            // equivalent to Token(token.Equals) but with better error messages
            => new TokenParser<TToken>(token);

        /// <summary>
        /// Creates a parser that parses and returns a single token satisfying a predicate
        /// </summary>
        /// <param name="predicate">A predicate function to apply to a token</param>
        /// <returns>A parser that parses and returns a single token satisfying a predicate</returns>
        public static Parser<TToken, TToken> Token(Func<TToken, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return new PredicateTokenParser<TToken>(predicate);
        }
    }

    internal sealed class TokenParser<TToken> : Parser<TToken, TToken>
    {
        private readonly TToken _token;
        private Expected<TToken> _expected;
        private Expected<TToken> Expected
        {
            get
            {
                if (_expected.InternalTokens.IsDefault)
                {
                    _expected = new Expected<TToken>(ImmutableArray.Create(_token));
                }
                return _expected;
            }
        }

        public TokenParser(TToken token)
        {
            _token = token;
        }

        internal sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out TToken result)
        {
            if (!state.HasCurrent)
            {
                state.Error = new InternalError<TToken>(
                    Maybe.Nothing<TToken>(),
                    true,
                    state.Location,
                    null
                );
                expecteds.Add(Expected);
                result = default;
                return false;
            }
            var token = state.Current;
            if (!EqualityComparer<TToken>.Default.Equals(token, _token))
            {
                state.Error = new InternalError<TToken>(
                    Maybe.Just(token),
                    false,
                    state.Location,
                    null
                );
                expecteds.Add(Expected);
                result = default;
                return false;
            }
            state.Advance();
            result = token;
            return true;
        }
    }

    internal sealed class PredicateTokenParser<TToken> : Parser<TToken, TToken>
    {
        private readonly Func<TToken, bool> _predicate;

        public PredicateTokenParser(Func<TToken, bool> predicate)
        {
            _predicate = predicate;
        }

        internal sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out TToken result)
        {
            if (!state.HasCurrent)
            {
                state.Error = new InternalError<TToken>(
                    Maybe.Nothing<TToken>(),
                    true,
                    state.Location,
                    null
                );
                result = default;
                return false;
            }
            var token = state.Current;
            if (!_predicate(token))
            {
                state.Error = new InternalError<TToken>(
                    Maybe.Just(token),
                    false,
                    state.Location,
                    null
                );
                result = default;
                return false;
            }
            state.Advance();
            result = token;
            return true;
        }
    }
}