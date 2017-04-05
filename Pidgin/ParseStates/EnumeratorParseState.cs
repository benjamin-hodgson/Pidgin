using System;
using System.Collections.Generic;

namespace Pidgin.ParseStates
{
    internal sealed class EnumeratorParseState<TToken> : BufferedParseState<TToken>
    {
        private readonly IEnumerator<TToken> _input;

        public EnumeratorParseState(IEnumerator<TToken> input, Func<TToken, SourcePos, SourcePos> posCalculator) : base(posCalculator)
        {
            _input = input;
        }

        protected sealed override Maybe<TToken> AdvanceInput()
        {
            var success = _input.MoveNext();
            if (!success)
            {
                // we've gone past the end of the input
                return Maybe.Nothing<TToken>();
            }
            return Maybe.Just(_input.Current);
        }
    }
}
