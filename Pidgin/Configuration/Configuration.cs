using System;

namespace Pidgin.Configuration
{
    /// <summary>
    /// Methods for creating and updating <see cref="IConfiguration{TToken}"/>s.
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Returns the default configuration for the token type <typeparamref name="TToken"/>.
        /// </summary>
        /// <typeparam name="TToken">The token type</typeparam>
        /// <returns>The default configuration for the token type <typeparamref name="TToken"/>.</returns>
        public static IConfiguration<TToken> Default<TToken>()
            => typeof(TToken) == typeof(char)
                ? (IConfiguration<TToken>)(object)CharDefaultConfiguration.Instance
                : DefaultConfiguration<TToken>.Instance;

        /// <summary>
        /// Override the <see cref="IConfiguration{TToken}.CalculateSourcePos(TToken, SourcePos)"/> method.
        /// </summary>
        /// <typeparam name="TToken">The token type</typeparam>
        /// <param name="configuration">The configuration</param>
        /// <param name="posCalculator">The new <see cref="IConfiguration{TToken}.CalculateSourcePos(TToken, SourcePos)"/> method.</param>
        /// <returns>
        /// A copy of <paramref name="configuration"/> with its <see cref="IConfiguration{TToken}.CalculateSourcePos(TToken, SourcePos)"/> method overridden.
        /// </returns>
        public static IConfiguration<TToken> WithPosCalculator<TToken>(
            this IConfiguration<TToken> configuration,
            Func<TToken, SourcePos, SourcePos> posCalculator
        ) => new OverrideConfiguration<TToken>(configuration, posCalculator: posCalculator);
    }
}
