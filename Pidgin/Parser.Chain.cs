using System;

namespace Pidgin
{
    public partial class Parser<TToken, T>
    {
        internal Parser<TToken, U> ChainAtLeastOnce<U, TChainer>(Func<TChainer> factory) where TChainer : struct, IChainer<T, U>
            => new ChainAtLeastOnceLParser<TToken, T, U, TChainer>(this, factory);
    }

    internal interface IChainer<in T, out U>
    {
        void Apply(T value);
        U GetResult();
        void OnError();
    }

    internal class ChainAtLeastOnceLParser<TToken, T, U, TChainer> : Parser<TToken, U> where TChainer : struct, IChainer<T, U>
    {
        private readonly Parser<TToken, T> _parser;
        private readonly Func<TChainer> _factory;

        public ChainAtLeastOnceLParser(Parser<TToken, T> parser, Func<TChainer> factory)
        {
            _parser = parser;
            _factory = factory;
        }

        internal override InternalResult<U> Parse(ref ParseState<TToken> state, ref ExpectedCollector<TToken> expecteds)
        {
            var result1 = _parser.Parse(ref state, ref expecteds);
            if (!result1.Success)
            {
                // state.Error set by _parser
                return InternalResult.Failure<U>(result1.ConsumedInput);
            }

            var chainer = _factory();
            chainer.Apply(result1.Value);
            var consumedInput = result1.ConsumedInput;

            var childExpecteds = new ExpectedCollector<TToken>();
            var result = _parser.Parse(ref state, ref childExpecteds);
            while (result.Success)
            {
                childExpecteds.Clear();
                if (!result.ConsumedInput)
                {
                    childExpecteds.Dispose();
                    chainer.OnError();
                    throw new InvalidOperationException("Many() used with a parser which consumed no input");
                }
                consumedInput = true;
                chainer.Apply(result.Value);

                result = _parser.Parse(ref state, ref childExpecteds);
            }
            expecteds.AddIf(ref childExpecteds, result.ConsumedInput);
            childExpecteds.Dispose();

            if (result.ConsumedInput)  // the most recent parser failed after consuming input
            {
                // state.Error set by _parser
                chainer.OnError();
                return InternalResult.Failure<U>(true);
            }
            var z = chainer.GetResult();
            return InternalResult.Success<U>(z, consumedInput);
        }
    }
}
