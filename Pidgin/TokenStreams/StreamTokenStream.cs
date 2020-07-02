using System;
using System.IO;

namespace Pidgin.TokenStreams
{
    internal class StreamTokenStream : ITokenStream<byte>
    {
        public int ChunkSizeHint => 4096;

        private readonly Stream _input;

        public StreamTokenStream(Stream input)
        {
            _input = input;
        }

        public int ReadInto(Span<byte> buffer) => _input.Read(buffer);

        public void Dispose() { }
    }
}