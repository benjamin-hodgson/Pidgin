using System.Diagnostics.CodeAnalysis;

using Pidgin.Configuration;

namespace Pidgin;

public static partial class Parser<TToken>
{
    /// <summary>A parser which returns the current <see cref="IConfiguration{TToken}"/>.</summary>
    public static Parser<TToken, IConfiguration<TToken>> Configuration { get; } = new ConfigurationParser<TToken>();
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal class ConfigurationParser<TToken> : Parser<TToken, IConfiguration<TToken>>
{
    public override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out IConfiguration<TToken> result)
    {
        result = state.Configuration;
        return true;
    }
}
