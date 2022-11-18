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
        => new WithExpectedParser<TToken, T>(this, expected);
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal sealed class WithExpectedParser<TToken, T> : Parser<TToken, T>
{
    private readonly Parser<TToken, T> _parser;
    private readonly ImmutableArray<Expected<TToken>> _expected;

    public WithExpectedParser(Parser<TToken, T> parser, ImmutableArray<Expected<TToken>> expected)
    {
        _parser = parser;
        _expected = expected;
    }

    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out T result)
    {
        var childExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        var success = _parser.TryParse(ref state, ref childExpecteds, out result);
        if (!success)
        {
            expecteds.AddRange(_expected);
        }

        childExpecteds.Dispose();

        // result is not null here
#pragma warning disable CS8762  // Parameter 'result' must have a non-null value when exiting with 'true'.
        return success;
#pragma warning restore CS8762
    }
}
