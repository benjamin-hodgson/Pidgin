using System;
using Pidgin.ParseStates;

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

            public ChainAtLeastOnceLParser(Parser<TToken, T> parser, Func<U> seed, Func<U, T, U> func) : base(parser.Expected)
            {
                _parser = parser;
                _seed = seed;
                _func = func;
            }

            internal override Result<TToken, U> Parse(IParseState<TToken> state)
            {
                var result1 = _parser.Parse(state);
                if (!result1.Success)
                {
                    return Result.Failure<TToken, U>(
                        result1.Error,
                        result1.ConsumedInput
                    );
                }
                var z = _func(_seed(), result1.Value);
                var consumedInput = result1.ConsumedInput;

                var result = _parser.Parse(state);
                while (result.Success)
                {
                    if (!result.ConsumedInput)
                    {
                        throw new InvalidOperationException("Many() used with a parser which consumed no input");
                    }
                    consumedInput = true;
                    z = _func(z, result.Value);
                    result = _parser.Parse(state);
                }
                if (result.ConsumedInput)  // the most recent parser failed after consuming input
                {
                    return Result.Failure<TToken, U>(
                        result.Error,
                        true
                    );
                }
                return Result.Success<TToken, U>(z, consumedInput);
            }
        }
    }
}