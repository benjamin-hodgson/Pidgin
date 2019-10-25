using System;
using System.IO;
using Pidgin.TokenStreams;
using Xunit;

namespace Pidgin.Tests
{
    public class ParseStateTests
    {
        [Fact]
        public void TestEmptyInput()
        {
            var input = "";
            var state = new ParseState<char>((_, x) => x.IncrementCol(), ToStream(input));

            Assert.Equal(new SourcePos(1, 1), state.ComputeSourcePos());
            Assert.False(state.HasCurrent);
        }

        [Fact]
        public void TestAdvance()
        {
            var input = "foo";
            var state = new ParseState<char>((_, x) => x.IncrementCol(), ToStream(input));

            Consume('f', ref state);
            Consume('o', ref state);
            Consume('o', ref state);

            Assert.False(state.HasCurrent);
        }

        [Fact]
        public void TestDiscardChunk()
        {
            var input = ('f' + new string('o', ChunkSize));  // Length == ChunkSize + 1
            var state = new ParseState<char>((_, x) => x.IncrementCol(), ToStream(input));

            Consume('f', ref state);
            Consume(new string('o', ChunkSize), ref state);
            Assert.False(state.HasCurrent);
            Assert.Equal(new SourcePos(1, input.Length + 1 /* because Col is 1-indexed */), state.ComputeSourcePos());
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
            var input = ('f' + new string('o', inputLength - 1));
            var state = new ParseState<char>((_, x) => x.IncrementCol(), ToStream(input));

            state.PushBookmark();

            Consume('f', ref state);
            Consume(new string('o', inputLength - 1), ref state);
            Assert.False(state.HasCurrent);
            Assert.Equal(new SourcePos(1, inputLength + 1), state.ComputeSourcePos());

            state.Rewind();
            Assert.Equal(new SourcePos(1, 1), state.ComputeSourcePos());
            Consume('f', ref state);
        }

        private static void UnalignedChunkTest(int inputLength)
        {
            var input = ("fa" + new string('o', inputLength - 2));
            var state = new ParseState<char>((_, x) => x.IncrementCol(), ToStream(input));

            Consume('f', ref state);

            state.PushBookmark();
            Consume('a' + new string('o', inputLength - 2), ref state);
            Assert.False(state.HasCurrent);
            Assert.Equal(new SourcePos(1, inputLength + 1), state.ComputeSourcePos());

            state.Rewind();
            Assert.Equal(new SourcePos(1, 2), state.ComputeSourcePos());
            Consume('a', ref state);
        }

        private static void Consume(char expected, ref ParseState<char> state)
        {
            var oldCol = state.ComputeSourcePos().Col;
            Assert.True(state.HasCurrent);
            Assert.Equal(expected, state.Current);
            state.Advance();
            Assert.Equal(oldCol + 1, state.ComputeSourcePos().Col);
        }

        private static void Consume(string expected, ref ParseState<char> state)
        {
            var oldCol = state.ComputeSourcePos().Col;
            AssertEqual(expected.AsSpan(), state.LookAhead(expected.Length));
            state.Advance(expected.Length);
            Assert.Equal(oldCol + expected.Length, state.ComputeSourcePos().Col);
        }

        private static void AssertEqual(ReadOnlySpan<char> expected, ReadOnlySpan<char> actual)
        {
            Assert.Equal(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        private static ITokenStream<char> ToStream(string input)
            => new ReaderTokenStream(new StringReader(input));

        private static int ChunkSize => ToStream("").ChunkSizeHint;
    }
}