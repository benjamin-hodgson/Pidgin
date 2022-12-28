using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public static partial class Parser<TToken>
{
    /// <summary>
    /// Creates a parser which returns the specified value without consuming any input.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <typeparam name="T">The type of the value to return.</typeparam>
    /// <returns>A parser which returns the specified value without consuming any input.</returns>
    public static Parser<TToken, T> Return<T>(T value)
        => new ReturnParser<TToken, T>(value);
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal sealed class ReturnParser<TToken, T> : Parser<TToken, T>
{
    private readonly T _value;

    public ReturnParser(T value)
    {
        _value = value;
    }

    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, out T result)
    {
        result = _value;
        return true;
    }
}
