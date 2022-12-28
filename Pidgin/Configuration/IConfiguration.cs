using System;

namespace Pidgin.Configuration;

/// <summary>
/// Represents a parser configuration.
/// </summary>
/// <typeparam name="TToken">The type of tokens.</typeparam>
public interface IConfiguration<TToken>
{
    /// <summary>
    /// A function which can compute a <see cref="SourcePosDelta"/> representing the change in position from consuming a token.
    /// </summary>
    Func<TToken, SourcePosDelta> SourcePosCalculator { get; }

    /// <summary>
    /// The <see cref="IArrayPoolProvider"/>.
    /// </summary>
    IArrayPoolProvider ArrayPoolProvider { get; }
}
