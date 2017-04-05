using System.Collections.Generic;
using Xunit;

namespace Pidgin.Tests
{
    public class ParserTestBase
    {
        protected void AssertSuccess<TToken, T>(Result<TToken, T> result, T expected, bool consumedInput)
        {
            Assert.True(result.Success);
            Assert.Equal(expected, result.Value);
            Assert.Equal(consumedInput, result.ConsumedInput);
        }

        protected void AssertFailure<TToken, T>(Result<TToken, T> result, IEnumerable<Expected<TToken>> expected, SourcePos errorPos, bool consumedInput)
        {
            Assert.False(result.Success);
            Assert.Equal(expected, result.Error.Expected);
            Assert.Equal(errorPos, result.Error.ErrorPos);
            Assert.Equal(consumedInput, result.ConsumedInput);
        }
    }
}
