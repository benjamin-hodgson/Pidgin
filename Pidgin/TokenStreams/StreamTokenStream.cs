using System;
using System.Buffers;
using System.IO;

namespace Pidgin.TokenStreams
{
    internal class StreamTokenStream : BufferedTokenStream<byte>
    {
        private readonly Stream _input;

        public StreamTokenStream(Stream input) : base(4096)
        {
            _input = input;
        }

        protected override int Read()
            => _input.Read(_buffer, _index, Math.Min(_chunkSize, _buffer.Length - _index));
    }
}