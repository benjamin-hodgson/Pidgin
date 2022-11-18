using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using static Pidgin.Parser<char>;

namespace Pidgin;

public static partial class Parser
{
    /// <summary>
    /// A parser that parses and returns a single whitespace character.
    /// </summary>
    public static Parser<char, char> Whitespace { get; }
        = Token(char.IsWhiteSpace).Labelled("whitespace");

    /// <summary>
    /// A parser that parses and returns a sequence of whitespace characters.
    /// </summary>
    public static Parser<char, IEnumerable<char>> Whitespaces { get; }
        = Whitespace.Many().Labelled("whitespace");

    /// <summary>
    /// A parser that parses and returns a sequence of whitespace characters packed into a string.
    /// </summary>
    public static Parser<char, string> WhitespaceString { get; }
        = Whitespace.ManyString().Labelled("whitespace");

    /// <summary>
    /// A parser that discards a sequence of whitespace characters.
    /// </summary>
    public static Parser<char, Unit> SkipWhitespaces { get; }
        = new SkipWhitespacesParser();
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal class SkipWhitespacesParser : Parser<char, Unit>
{
    public override unsafe bool TryParse(ref ParseState<char> state, ref PooledList<Expected<char>> expecteds, out Unit result)
    {
        const long space = ' ';
        const long fourSpaces = space | space << 16 | space << 32 | space << 48;
        result = Unit.Value;

        var chunk = state.LookAhead(32);
        while (chunk.Length == 32)
        {
            fixed (char* ptr = chunk)
            {
                for (var i = 0; i < 8; i++)
                {
                    if (*(long*)(ptr + (i * 4)) != fourSpaces)
                    {
                        // there's a non-' ' character somewhere in this group of four
                        for (var j = 0; j < 4; j++)
                        {
                            if (!char.IsWhiteSpace(chunk[(i * 4) + j]))
                            {
                                state.Advance((i * 4) + j);
                                return true;
                            }
                        }
                    }
                }
            }

            state.Advance(32);
            chunk = state.LookAhead(32);
        }

        var remainingGroupsOfFour = chunk.Length / 4;
        fixed (char* ptr = chunk)
        {
            for (var i = 0; i < remainingGroupsOfFour; i++)
            {
                if (*(long*)(ptr + (i * 4)) != fourSpaces)
                {
                    for (var j = 0; j < 4; j++)
                    {
                        if (!char.IsWhiteSpace(chunk[(i * 4) + j]))
                        {
                            state.Advance((i * 4) + j);
                            return true;
                        }
                    }
                }
            }
        }

        for (var i = remainingGroupsOfFour * 4; i < chunk.Length; i++)
        {
            if (!char.IsWhiteSpace(chunk[i]))
            {
                state.Advance(i);
                return true;
            }
        }

        state.Advance(chunk.Length);
        return true;
    }
}
