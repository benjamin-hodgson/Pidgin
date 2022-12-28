namespace Pidgin;

public static partial class Parser
{
    /// <summary>
    /// A parser that parses and returns either the literal string "\r\n" or the literal string "\n".
    /// </summary>
    public static Parser<char, string> EndOfLine { get; }
        = String("\r\n")
            .Or(String("\n"))
            .Labelled("end of line");
}
