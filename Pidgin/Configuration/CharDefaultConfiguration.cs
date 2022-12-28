using System;

namespace Pidgin.Configuration;

/// <summary>
/// A default configuration for textual input.
/// </summary>
public class CharDefaultConfiguration : DefaultConfiguration<char>
{
    /// <summary>
    /// The shared global instance of <see cref="CharDefaultConfiguration"/>.
    /// </summary>
    public static new IConfiguration<char> Instance { get; } = new CharDefaultConfiguration();

    /// <summary>
    /// Handles newlines and tab stops.
    /// </summary>
    public override Func<char, SourcePosDelta> SourcePosCalculator { get; }
        = token => token == '\n'
            ? SourcePosDelta.NewLine
            : token == '\t'
                ? new SourcePosDelta(0, 4)
                : SourcePosDelta.OneCol;
}
