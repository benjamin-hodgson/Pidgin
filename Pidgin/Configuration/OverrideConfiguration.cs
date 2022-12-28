using System;

namespace Pidgin.Configuration;

internal class OverrideConfiguration<TToken> : IConfiguration<TToken>
{
    public Func<TToken, SourcePosDelta> SourcePosCalculator { get; }

    public IArrayPoolProvider ArrayPoolProvider { get; }

    public OverrideConfiguration(
        IConfiguration<TToken> next,
        Func<TToken, SourcePosDelta>? posCalculator = null,
        IArrayPoolProvider? arrayPoolProvider = null
    )
    {
        SourcePosCalculator = posCalculator ?? next.SourcePosCalculator;
        ArrayPoolProvider = arrayPoolProvider ?? next.ArrayPoolProvider;
    }
}
