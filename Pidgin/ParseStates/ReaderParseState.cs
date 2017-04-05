using System;
using System.IO;

namespace Pidgin.ParseStates
{
    internal sealed class ReaderParseState : BufferedParseState<char>
    {
        private readonly TextReader _reader;

        public ReaderParseState(TextReader reader, Func<char, SourcePos, SourcePos> posCalculator) : base(posCalculator)
        {
            _reader = reader;
        }

        protected sealed override Maybe<char> AdvanceInput()
        {
            // TextReader does its own internal buffering,
            // so it's perfectly efficient to just read a single character.
            // (In fact it would be a perf bug if I were to try to add buffering on top.)
            // The only buffering I need to do is whatever I need for backtracking
            var result = _reader.Read();
            if (result == -1)
            {
                // we're at the end of the input
                return Maybe.Nothing<char>();
            }
            return Maybe.Just(Convert.ToChar(result));
        }
    }
}