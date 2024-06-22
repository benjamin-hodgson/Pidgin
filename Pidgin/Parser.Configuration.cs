using System.Diagnostics.CodeAnalysis;

using Pidgin.Configuration;

namespace Pidgin;

public static partial class Parser<TToken>
{
    /// <summary>A parser which returns the current <see cref="IConfiguration{TToken}"/>.</summary>
    public static Parser<TToken, IConfiguration<TToken>> Configuration { get; }
        = BoxParser<TToken, IConfiguration<TToken>>.Create(default(ConfigurationParser<TToken>));
}

internal readonly struct ConfigurationParser<TToken> : IParser<TToken, IConfiguration<TToken>>
{
    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out IConfiguration<TToken> result)
    {
        result = state.Configuration;
        return true;
    }
}
