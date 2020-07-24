using System;
using System.IO;

namespace Pidgin.TokenStreams
{
    internal class ReaderTokenStream : ITokenStream<char>
    {
        public int ChunkSizeHint => 4096;

        private readonly TextReader _input;

        public ReaderTokenStream(TextReader input)
        {
            _input = input;
        }

        public int Read(Span<char> buffer) => _input.Read(buffer);
    }
}