using System;
using System.IO;

namespace Pidgin.ParseStates
{
    internal sealed class StreamParseState : BufferedParseState<byte>
    {
        private readonly Stream _input;

        public StreamParseState(Stream input, Func<byte, SourcePos, SourcePos> posCalculator) : base(posCalculator)
        {
            _input = input;
        }

        protected sealed override Maybe<byte> AdvanceInput()
        {
            // Most common implementations of stream (eg FileStream)
            // do their own internal buffering, so I don't need to read
            // the input in chunks, it's fine to read single bytes and
            // would probably be a perf bug to try and add my own buffering.
            // (Well, the method call isn't free.)
            // Unfortunately this is exactly the wrong thing to do if the
            // stream happens not to do its own internal buffering,
            // and there's no way to find out whether it's buffered or not.
            var result = _input.ReadByte();
            if (result == -1)
            {
                // we've reached the end of the input
                return Maybe.Nothing<byte>();
            }
            return Maybe.Just(Convert.ToByte(result));
        }
    }
}