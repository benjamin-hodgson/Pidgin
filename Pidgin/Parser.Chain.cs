using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Pidgin.Configuration;

namespace Pidgin
{
    public partial class Parser<TToken, T>
    {
        internal Parser<TToken, U> ChainAtLeastOnce<U, TChainer>(Func<IConfiguration<TToken>, TChainer> factory) where TChainer : struct, IChainer<T, U>
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
        private readonly Func<IConfiguration<TToken>, TChainer> _factory;

        public ChainAtLeastOnceLParser(Parser<TToken, T> parser, Func<IConfiguration<TToken>, TChainer> factory)
        {
            _parser = parser;
            _factory = factory;
        }

        internal sealed override bool TryParse(ref ParseState<TToken> state, ICollection<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out U result)
        {
            if (!_parser.TryParse(ref state, expecteds, out var result1))
            {
                // state.Error set by _parser
                result = default;
                return false;
            }

            var chainer = _factory(state.Configuration);
            chainer.Apply(result1);

            var lastStartLoc = state.Location;
            var childExpecteds = state.GetExpectedCollector();
            while (_parser.TryParse(ref state, childExpecteds, out var childResult))
            {
                var endLoc = state.Location;
                childExpecteds.Clear();
                if (endLoc <= lastStartLoc)
                {
                    state.ReturnExpectedCollector(childExpecteds);
                    chainer.OnError();
                    throw new InvalidOperationException("Many() used with a parser which consumed no input");
                }
                chainer.Apply(childResult);

                lastStartLoc = endLoc;
            }

            if (state.Location > lastStartLoc)  // the most recent parser failed after consuming input
            {
                // state.Error set by _parser
                expecteds.AddRange(childExpecteds);
                state.ReturnExpectedCollector(childExpecteds);
                chainer.OnError();
                result = default;
                return false;
            }
            
            state.ReturnExpectedCollector(childExpecteds);
            result = chainer.GetResult();
            return true;
        }
    }
}
