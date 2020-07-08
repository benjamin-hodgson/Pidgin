using System;
using System.Buffers;
using Xunit;

namespace Pidgin.Tests
{
    public class PooledListTests
    {
        [Fact]
        public void TerribleBugWhenAppendingString()
        {
            var expected = "longer than the capacity";
            var builder = new PooledList<char>(ArrayPool<char>.Shared, 1);
            builder.AddRange(expected.AsSpan());
            Assert.Equal(expected, builder.AsEnumerable());
        }
    }
}
