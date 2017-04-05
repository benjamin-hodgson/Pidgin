using System;
using System.Collections.Generic;

namespace Pidgin.ParseStates
{
    internal sealed class ListParseState<TToken> : InMemoryParseState<TToken>
    {
        private readonly IList<TToken> _input;

        public ListParseState(IList<TToken> input, Func<TToken, SourcePos, SourcePos> posCalculator) : base(input.Count, posCalculator)
        {
            _input = input;
        }

        protected sealed override TToken GetElement(int index)
            => _input[index];
    }
}