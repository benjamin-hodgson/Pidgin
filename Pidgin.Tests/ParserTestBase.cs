
using Xunit;

namespace Pidgin.Tests;

public class ParserTestBase
{
    protected static void AssertSuccess<TToken, T>(Result<TToken, T> result, T expected, bool consumedInput)
    {
        Assert.True(result.Success);
        Assert.Equal(expected, result.Value);
        Assert.Equal(consumedInput, result.ConsumedInput);
    }

    protected static void AssertFailure<TToken, T>(Result<TToken, T> result, ParseError<TToken> expectedError, bool consumedInput)
    {
        Assert.False(result.Success);
        Assert.Equal(expectedError, result.Error);
        Assert.Equal(consumedInput, result.ConsumedInput);
    }
}
