using Xunit;

namespace Pidgin.Tests
{
    public class PooledStringBuilderTests
    {
        [Fact]
        public void TerribleBugWhenAppendingString()
        {
            var expected = "longer than the capacity";
            var builder = new PooledStringBuilder(1);
            builder.Append(expected);
            Assert.Equal(expected, builder.GetStringAndClear());
        }
    }
}