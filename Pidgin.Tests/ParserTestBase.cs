using System;
using System.Collections.Generic;

using Xunit;

namespace Pidgin.Tests;

public class ParserTestBase
{
    protected static void AssertFullParse<T>(Parser<char, T> parser, string input, T expected)
    {
        AssertPartialParse(parser, input, expected, input.Length);
    }

    protected static void AssertPartialParse<T>(Parser<char, T> parser, string input, T expected, int consumed)
    {
        var p = Parser.Map(
            (x, end) => (value: x, consumed: end),
            parser,
            Parser<char>.CurrentOffset
        );
        var result = p.Parse(input);
        Assert.True(result.Success, $"Parse should have succeeded but failed with '{result.ErrorOrDefault}'");
        AssertValue(expected, result.Value.value);
        Assert.Equal(consumed, result.Value.consumed);
    }

    protected static void AssertSuccess<TToken, T>(Result<TToken, T> result, T expected)
    {
        Assert.True(result.Success, $"Parse should have succeeded but failed with '{result.ErrorOrDefault}'");
        AssertValue(expected, result.Value);
    }

    protected static void AssertFailure<TToken, T>(Parser<TToken, T> parser, IEnumerable<TToken> input, ParseError<TToken> expectedError)
    {
        var result = parser.Parse(input);
        AssertFailure(result, expectedError);
    }

    protected static void AssertFailure<TToken, T>(Result<TToken, T> result, ParseError<TToken> expectedError)
    {
        Assert.False(result.Success, "Parse should have failed");
        AssertValue(expectedError, result.Error);
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
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (Exception)
            {
                Console.WriteLine(actual!.GetType());
                throw;
            }
        }
    }
}
