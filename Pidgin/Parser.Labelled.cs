using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser equivalent to the current parser, with a custom label.
    /// The label will be reported in an error message if the parser fails, instead of the default error message.
    /// <seealso cref="ParseError{TToken}.Expected"/>
    /// <seealso cref="Expected{TToken}.Label"/>
    /// </summary>
    /// <param name="label">The custom label to apply to the current parser.</param>
    /// <returns>A parser equivalent to the current parser, with a custom label.</returns>
    public Parser<TToken, T> Labelled(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException(nameof(label));
        }

        return WithExpected(ImmutableArray.Create(new Expected<TToken>(label)));
    }

    internal Parser<TToken, T> WithExpected(ImmutableArray<Expected<TToken>> expected)
        => Accept(new WithExpectedParserFactory<TToken, T>(expected));
}

internal class WithExpectedParserFactory<TToken, T>(ImmutableArray<Expected<TToken>> expected)
    : IReboxer<TToken, T, T>
{
    public BoxParser<TToken, T> WithBox<Next>(BoxParser<TToken, T>.Of<Next> box)
        where Next : IParser<TToken, T>
        => BoxParser<TToken, T>.Create(new WithExpectedParser<Next, TToken, T>(box, expected));
}

internal readonly struct WithExpectedParser<Next, TToken, T> : IParser<TToken, T>
    where Next : IParser<TToken, T>
{
    private readonly BoxParser<TToken, T>.Of<Next> _parser;
    private readonly ImmutableArray<Expected<TToken>> _expected;

    public WithExpectedParser(BoxParser<TToken, T>.Of<Next> parser, ImmutableArray<Expected<TToken>> expected)
    {
        _parser = parser;
        _expected = expected;
    }

    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out T result)
    {
        var childExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        var success = _parser.Value.TryParse(ref state, ref childExpecteds, out result);
        if (!success)
        {
            expecteds.AddRange(_expected);
        }

        childExpecteds.Dispose();

        // result is not null here
        return success;
    }
}
