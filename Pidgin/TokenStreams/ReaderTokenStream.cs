using System;
using System.Buffers;
using System.IO;

namespace Pidgin.TokenStreams
{
    internal class ReaderTokenStream : BufferedTokenStream<char>
    {
        private readonly TextReader _input;

        public ReaderTokenStream(TextReader input) : base(4096)
        {
            _input = input;
        }

        protected override int Read()
            => _input.Read(_buffer, _index, Math.Min(_chunkSize, _buffer.Length - _index));
    }
}