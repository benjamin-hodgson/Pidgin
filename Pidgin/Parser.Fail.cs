using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public static partial class Parser<TToken>
{
    /// <summary>
    /// Creates a parser which always fails without consuming any input.
    /// </summary>
    /// <param name="message">A custom error message.</param>
    /// <typeparam name="T">The return type of the resulting parser.</typeparam>
    /// <returns>A parser which always fails.</returns>
    public static Parser<TToken, T> Fail<T>(string message = "Failed")
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return new FailParser<TToken, T>(message);
    }
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal sealed class FailParser<TToken, T> : Parser<TToken, T>
{
    private static readonly Expected<TToken> _expected
        = new(ImmutableArray<TToken>.Empty);

    private readonly string _message;

    public FailParser(string message)
    {
        _message = message;
    }

    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out T result)
    {
        state.SetError(Maybe.Nothing<TToken>(), false, state.Location, _message);
        expecteds.Add(_expected);
        result = default;
        return false;
    }
}
