
using System;

using Xunit;

namespace Pidgin.Tests;

public class ParserTestBase
{
    protected static void AssertSuccess<TToken, T>(Result<TToken, T> result, T expected, bool consumedInput)
    {
        Assert.True(result.Success);
        AssertValue(expected, result.Value);
        Assert.Equal(consumedInput, result.ConsumedInput);
    }

    protected static void AssertFailure<TToken, T>(Result<TToken, T> result, ParseError<TToken> expectedError, bool consumedInput)
    {
        Assert.False(result.Success);
        AssertValue(expectedError, result.Error);
        Assert.Equal(consumedInput, result.ConsumedInput);
    }

    private static void AssertValue<T>(T expected, T actual)
    {
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ValueTuple<,>))
        {
            var item1 = typeof(T).GetField("Item1")!;
            var item2 = typeof(T).GetField("Item2")!;
            Assert.Equal(item1.GetValue(expected), item1.GetValue(actual));
            Assert.Equal(item2.GetValue(expected), item2.GetValue(actual));
        }
        else
        {
            Assert.Equal(expected, actual);
        }
    }
}
