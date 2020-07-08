using System;

namespace Pidgin.Configuration
{
    /// <summary>
    /// A default configuration for textual input
    /// </summary>
    public class CharDefaultConfiguration : DefaultConfiguration<char>
    {
        /// <summary>
        /// The shared global instance of <see cref="CharDefaultConfiguration"/>
        /// </summary>
        public static new IConfiguration<char> Instance { get; } = new CharDefaultConfiguration();

        /// <summary>
        /// Handles newlines and tab stops
        /// </summary>
        public override Func<char, SourcePos, SourcePos> SourcePosCalculator { get; }
            = (token, previous) => token == '\n'
                ? previous.NewLine()
                : token == '\t'
                    ? new SourcePos(previous.Line, previous.Col + 4)
                    : previous.IncrementCol();
    }
}
