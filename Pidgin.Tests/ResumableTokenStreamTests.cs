using System;
using System.IO;
using Pidgin.TokenStreams;
using Xunit;

namespace Pidgin.Tests
{
    public class ReusableTokenStreamTests
    {
        [Fact]
        public void TestResume()
        {
            var input = "aaabb";
            var stream = new ResumableTokenStream<char>(new ReaderTokenStream(new StringReader(input)));

            // consume two 'a's, reject the third one
            var chunk = new char[3].AsSpan();
            stream.Read(chunk);
            stream.OnParserEnd(chunk.Slice(2));

            stream.Read(chunk);
            Assert.Equal("abb", chunk.ToString());
        }

        [Fact]
        public void TestReturnMultipleChunks()
        {
            var stream = new ResumableTokenStream<char>(new ReaderTokenStream(new StringReader("")));
            stream.OnParserEnd("aa");
            stream.OnParserEnd("bb");

            var chunk = new char[4].AsSpan();
            stream.Read(chunk);
            Assert.Equal("bbaa", chunk.ToString());
        }
    }
}