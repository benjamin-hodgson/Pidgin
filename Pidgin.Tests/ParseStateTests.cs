using System;
using Pidgin.TokenStreams;
using Xunit;

namespace Pidgin.Tests
{
    public class ParseStateTests
    {
        [Fact]
        public void TestEmptyInput()
        {
            var input = "".AsSpan();
            var state = new ParseState<char>((_, x) => x, new SpanTokenStream<char>(ref input));

            Assert.False(state.HasCurrent);
        }

        [Fact]
        public void TestAdvance()
        {
            var input = "foo".AsSpan();
            var state = new ParseState<char>((_, x) => x, new SpanTokenStream<char>(ref input));

            Consume('f', ref state);
            Consume('o', ref state);
            Consume('o', ref state);

            Assert.False(state.HasCurrent);
        }

        [Fact]
        public void TestDiscardChunk()
        {
            var input = ('f' + new string('o', ChunkSize)).AsSpan();  // Length == ChunkSize + 1
            var state = new ParseState<char>((_, x) => x, new SpanTokenStream<char>(ref input));

            Consume('f', ref state);
            for (var _ = 0; _ < ChunkSize; _++)
            {
                Consume('o', ref state);
            }
            Assert.False(state.HasCurrent);
        }

        [Fact]
        public void TestSaveWholeChunkAligned()
        {
            // grows buffer on final iteration of loop
            //
            // |----|----|
            // foooo
            // ^----
            AlignedChunkTest(ChunkSize);
        }
        [Fact]
        public void TestSaveWholeChunkUnaligned()
        {
            // grows buffer on final iteration of loop
            //
            // |----|----|
            // faoooo
            //  ^----
            UnalignedChunkTest(ChunkSize);
        }
        [Fact]
        public void TestSaveMoreThanWholeChunkAligned()
        {
            // grows buffer on penultimate iteration of loop
            //
            // |----|----|
            // fooooo
            // ^-----
            AlignedChunkTest(ChunkSize + 1);
        }
        [Fact]
        public void TestSaveMoreThanWholeChunkUnaligned()
        {
            // grows buffer on penultimate iteration of loop
            //
            // |----|----|
            // faoooo
            //  ^----
            UnalignedChunkTest(ChunkSize + 1);
        }
        [Fact]
        public void TestSaveLessThanWholeChunkAligned()
        {
            // does not grow buffer
            //
            // |----|----|
            // fooooo
            // ^-----
            AlignedChunkTest(ChunkSize - 1);
        }
        [Fact]
        public void TestSaveLessThanWholeChunkUnaligned()
        {
            // does not grow buffer
            //
            // |----|----|
            // fooooo
            // ^-----
            UnalignedChunkTest(ChunkSize - 1);
        }

        private static void AlignedChunkTest(int inputLength)
        {
            var input = ('f' + new string('o', inputLength - 1)).AsSpan();
            var state = new ParseState<char>((_, x) => x, new SpanTokenStream<char>(ref input));

            state.PushBookmark();

            Consume('f', ref state);
            Consume(new string('o', inputLength - 1), ref state);
            Assert.False(state.HasCurrent);

            state.Rewind();
            Consume('f', ref state);
        }

        private static void UnalignedChunkTest(int inputLength)
        {
            var input = ("fa" + new string('o', inputLength - 2)).AsSpan();
            var state = new ParseState<char>((_, x) => x, new SpanTokenStream<char>(ref input));

            Consume('f', ref state);

            state.PushBookmark();
            Consume('a' + new string('o', inputLength - 2), ref state);
            Assert.False(state.HasCurrent);

            state.Rewind();
            Consume('a', ref state);
        }

        private static void Consume(char expected, ref ParseState<char> state)
        {
            Assert.True(state.HasCurrent);
            Assert.Equal(expected, state.Current);
            state.Advance();
        }

        private static void Consume(string expected, ref ParseState<char> state)
        {
            AssertEqual(expected.AsSpan(), state.Peek(expected.Length));
            state.Advance(expected.Length);
        }

        private static void AssertEqual(ReadOnlySpan<char> expected, ReadOnlySpan<char> actual)
        {
            Assert.Equal(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        private static int ChunkSize
        {
            get
            {
                var input = "".AsSpan();
                return new SpanTokenStream<char>(ref input).ChunkSizeHint;
            }
        }

    }
}