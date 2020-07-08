namespace Pidgin.Configuration
{
    /// <summary>
    /// Represents a parser configuration
    /// </summary>
    /// <typeparam name="TToken">The type of tokens</typeparam>
    public interface IConfiguration<TToken>
    {
        /// <summary>
        /// Returns a new <see cref="SourcePos"/> representing the position after consuming <paramref name="token"/>.
        /// </summary>
        /// <param name="token">The token which was consumed</param>
        /// <param name="previous">The source position before consuming <paramref name="token"/>.</param>
        /// <returns>The position after consuming <paramref name="token"/>.</returns>
        SourcePos CalculateSourcePos(TToken token, SourcePos previous);
    }
}
