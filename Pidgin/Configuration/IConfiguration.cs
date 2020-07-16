using System;

namespace Pidgin.Configuration
{
    /// <summary>
    /// Represents a parser configuration
    /// </summary>
    /// <typeparam name="TToken">The type of tokens</typeparam>
    public interface IConfiguration<TToken>
    {
        /// <summary>
        /// A function which can compute a new <see cref="SourcePos"/> representing the position after consuming a token.
        /// </summary>
        Func<TToken, SourcePos, SourcePos> SourcePosCalculator { get; }

        /// <summary>
        /// The <see cref="IArrayPoolProvider"/>
        /// </summary>
        IArrayPoolProvider ArrayPoolProvider { get; }
    }
}
