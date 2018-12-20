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

        public int ReadInto(byte[] buffer, int startIndex, int length)
            => _input.Read(buffer, startIndex, length);

        public void Dispose() { }
    }
}