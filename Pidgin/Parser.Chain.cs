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

        internal sealed override bool TryParse(ref ParseState<TToken> state, ref ExpectedCollector<TToken> expecteds, out U result)
        {
            var success1 = _parser.TryParse(ref state, ref expecteds, out var result1);
            if (!success1)
            {
                // state.Error set by _parser
                result = default;
                return false;
            }

            var chainer = _factory();
            chainer.Apply(result1);

            var lastStartLoc = state.Location;
            var childExpecteds = new ExpectedCollector<TToken>();
            var success = _parser.TryParse(ref state, ref childExpecteds, out var childResult);
            while (success)
            {
                var endLoc = state.Location;
                childExpecteds.Clear();
                if (endLoc <= lastStartLoc)
                {
                    childExpecteds.Dispose();
                    chainer.OnError();
                    throw new InvalidOperationException("Many() used with a parser which consumed no input");
                }
                chainer.Apply(childResult);

                lastStartLoc = endLoc;
                success = _parser.TryParse(ref state, ref childExpecteds, out childResult);
            }
            var lastParserConsumedInput = state.Location > lastStartLoc;
            expecteds.AddIf(ref childExpecteds, lastParserConsumedInput);
            childExpecteds.Dispose();

            if (lastParserConsumedInput)  // the most recent parser failed after consuming input
            {
                // state.Error set by _parser
                chainer.OnError();
                result = default;
                return false;
            }
            
            result = chainer.GetResult();
            return true;
        }
    }
}
