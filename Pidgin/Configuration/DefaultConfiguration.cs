namespace Pidgin.Configuration
{
    /// <summary>
    /// A default configuration for any token type
    /// </summary>
    /// <typeparam name="TToken">The token type</typeparam>
    public class DefaultConfiguration<TToken> : IConfiguration<TToken>
    {
        /// <summary>
        /// The shared global instance of <see cref="DefaultConfiguration{TToken}"/>
        /// </summary>
        public static IConfiguration<TToken> Instance { get; } = new DefaultConfiguration<TToken>();

        /// <summary>
        /// Always increments the column by 1.
        /// </summary>
        public virtual SourcePos CalculateSourcePos(TToken token, SourcePos previous)
            => previous.IncrementCol();
    }
}
