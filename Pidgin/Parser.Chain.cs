using System;
using System.Collections.Immutable;

namespace Pidgin
{
    public partial class Parser<TToken, T>
    {
        private Parser<TToken, U> ReturnSeed<U>(Func<U> seed)
            => Parser<TToken>
                .Return(Unit.Value)
                .Select(_ => seed());

        internal Parser<TToken, U> ChainL<U>(Func<U> seed, Func<U, T, U> func)
            => this.ChainAtLeastOnceL(seed, func)
                .Or(ReturnSeed(seed));

        internal Parser<TToken, U> ChainAtLeastOnceL<U>(Func<U> seed, Func<U, T, U> func)
            => new ChainAtLeastOnceLParser<U>(this, seed, func);

        private class ChainAtLeastOnceLParser<U> : Parser<TToken, U>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Func<U> _seed;
            private readonly Func<U, T, U> _func;

            public ChainAtLeastOnceLParser(Parser<TToken, T> parser, Func<U> seed, Func<U, T, U> func)
            {
                _parser = parser;
                _seed = seed;
                _func = func;
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

                var result = _parser.Parse(ref state);
                while (result.Success)
                {
                    if (!result.ConsumedInput)
                    {
                        throw new InvalidOperationException("Many() used with a parser which consumed no input");
                    }
                    consumedInput = true;
                    z = _func(z, result.Value);
                    result = _parser.Parse(ref state);
                }
                if (result.ConsumedInput)  // the most recent parser failed after consuming input
                {
                    // state.Error set by _parser
                    return InternalResult.Failure<U>(true);
                }
                return InternalResult.Success<U>(z, consumedInput);
            }
        }
    }
}