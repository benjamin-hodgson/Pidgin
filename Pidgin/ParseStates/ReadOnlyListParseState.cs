using System;
using System.Collections.Generic;

namespace Pidgin.ParseStates
{
    internal sealed class ReadOnlyListParseState<TToken> : InMemoryParseState<TToken>
    {
        private readonly IReadOnlyList<TToken> _input;

        public ReadOnlyListParseState(IReadOnlyList<TToken> input, Func<TToken, SourcePos, SourcePos> posCalculator) : base(input.Count, posCalculator)
        {
            _input = input;
        }

        protected sealed override TToken GetElement(int index)
            => _input[index];
    }
}