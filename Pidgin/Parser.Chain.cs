using System;
using System.Collections.Immutable;

namespace Pidgin
{
    public partial class Parser<TToken, T>
    {
        private static Parser<TToken, U> ReturnSeed<U>(Func<U> seed)
            => Parser<TToken>
                .Return(seed)
                .Select(f => f());

        internal Parser<TToken, U> ChainL<U>(Func<U> seed, Func<U, T, U> func, Action<U>? onFail = null)
            => this.ChainAtLeastOnceL(seed, func, onFail)
                .Or(ReturnSeed(seed));

        internal Parser<TToken, U> ChainAtLeastOnceL<U>(Func<U> seed, Func<U, T, U> func, Action<U>? onFail = null)
            => new ChainAtLeastOnceLParser<U>(this, seed, func, onFail);

        private class ChainAtLeastOnceLParser<U> : Parser<TToken, U>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Func<U> _seed;
            private readonly Func<U, T, U> _func;
            private readonly Action<U>? _onFail;

            public ChainAtLeastOnceLParser(Parser<TToken, T> parser, Func<U> seed, Func<U, T, U> func, Action<U>? onFail)
            {
                _parser = parser;
                _seed = seed;
                _func = func;
                _onFail = onFail;
            }

            internal override InternalResult<U> Parse(ref ParseState<TToken> state)
            {
                var result1 = _parser.Parse(ref state);
                if (!result1.Success)
                {
                    // state.Error set by _parser
                    return InternalResult.Failure<U>(result1.ConsumedInput);
                }
                var z = _func(_seed(), result1.Value);
                var consumedInput = result1.ConsumedInput;

                state.BeginExpectedTran();
                var result = _parser.Parse(ref state);
                while (result.Success)
                {
                    state.EndExpectedTran(false);
                    if (!result.ConsumedInput)
                    {
                        _onFail?.Invoke(z);
                        throw new InvalidOperationException("Many() used with a parser which consumed no input");
                    }
                    consumedInput = true;
                    z = _func(z, result.Value);

                    state.BeginExpectedTran();
                    result = _parser.Parse(ref state);
                }
                state.EndExpectedTran(result.ConsumedInput);
                if (result.ConsumedInput)  // the most recent parser failed after consuming input
                {
                    // state.Error set by _parser
                    _onFail?.Invoke(z);
                    return InternalResult.Failure<U>(true);
                }
                return InternalResult.Success<U>(z, consumedInput);
            }
        }
    }
}
