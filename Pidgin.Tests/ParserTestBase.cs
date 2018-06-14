using System;
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

        protected void AssertFailure<TToken, T>(Result<TToken, T> result, ParseError<TToken> expectedError, bool consumedInput)
        {
            Assert.False(result.Success);
            Assert.Equal(expectedError, result.Error);
            Assert.Equal(consumedInput, result.ConsumedInput);
        }

        protected void AssertSuccess<TToken, T>(Result<TToken, T> result, string expected, bool consumedInput)
        {
            Assert.True(result.Success);
            Assert.Equal(expected, result.Value.ToString());
            Assert.Equal(consumedInput, result.ConsumedInput);
        }

        protected void AssertFailure<TToken, T>(Result<TToken, T> result, bool consumedInput)
        {
            Assert.False(result.Success);
            Assert.Equal(consumedInput, result.ConsumedInput);
        }
    }
}
