using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using static Pidgin.Parser<char>;

namespace Pidgin;

public static partial class Parser
{
    /// <summary>
    /// Creates a parser which parses and returns a single character.
    /// </summary>
    /// <param name="character">The character to parse.</param>
    /// <returns>A parser which parses and returns a single character.</returns>
    [SuppressMessage(
        "design",
        "CA1720:Identifier contains type name",
        Justification = "Would be a breaking change"
    )]
    public static Parser<char, char> Char(char character) => Token(character);

    /// <summary>
    /// Creates a parser which parses and returns a single character, in a case insensitive manner.
    /// The parser returns the actual character parsed.
    /// </summary>
    /// <param name="character">The character to parse.</param>
    /// <returns>A parser which parses and returns a single character.</returns>
    public static Parser<char, char> CIChar(char character)
    {
        var theChar = char.ToLowerInvariant(character);
        var expected = ImmutableArray.Create(
            new Expected<char>(ImmutableArray.Create(char.ToUpperInvariant(character))),
            new Expected<char>(ImmutableArray.Create(char.ToLowerInvariant(character)))
        );
        return Token(c => char.ToLowerInvariant(c) == theChar)
            .WithExpected(expected);
    }

    /// <summary>
    /// Creates a parser which parses and returns a character if it is not one of the specified characters.
    /// When the character is one of the given characters, the parser fails without consuming input.
    /// </summary>
    /// <param name="chars">A sequence of characters that should not be matched.</param>
    /// <returns>A parser which parses and returns a character that does not match one of the specified characters.</returns>
    public static Parser<char, char> AnyCharExcept(params char[] chars)
    {
        if (chars == null)
        {
            throw new ArgumentNullException(nameof(chars));
        }

        return AnyCharExcept(chars.AsEnumerable());
    }

    /// <summary>
    /// Creates a parser which parses and returns a character if it is not one of the specified characters.
    /// When the character is one of the given characters, the parser fails without consuming input.
    /// </summary>
    /// <param name="chars">A sequence of characters that should not be matched.</param>
    /// <returns>A parser which parses and returns a character that does not match one of the specified characters.</returns>
    public static Parser<char, char> AnyCharExcept(IEnumerable<char> chars)
    {
        if (chars == null)
        {
            throw new ArgumentNullException(nameof(chars));
        }

        var cs = chars.ToArray();
        return Parser<char>.Token(c => Array.IndexOf(cs, c) == -1);
    }

    /// <summary>
    /// A parser that parses and returns a single digit character (0-9).
    /// </summary>
    /// <returns>A parser that parses and returns a single digit character.</returns>
    public static Parser<char, char> Digit { get; } = Token(c => char.IsDigit(c)).Labelled("digit");

    /// <summary>
    /// A parser that parses and returns a single letter character.
    /// </summary>
    public static Parser<char, char> Letter { get; } = Token(c => char.IsLetter(c)).Labelled("letter");

    /// <summary>
    /// A parser that parses and returns a single letter or digit character.
    /// </summary>
    public static Parser<char, char> LetterOrDigit { get; } = Token(c => char.IsLetterOrDigit(c)).Labelled("letter or digit");

    /// <summary>
    /// A parser that parses and returns a single lowercase letter character.
    /// </summary>
    public static Parser<char, char> Lowercase { get; } = Token(c => char.IsLower(c)).Labelled("lowercase letter");

    /// <summary>
    /// A parser that parses and returns a single uppercase letter character.
    /// </summary>
    public static Parser<char, char> Uppercase { get; } = Token(c => char.IsUpper(c)).Labelled("uppercase letter");

    /// <summary>
    /// A parser that parses and returns a single Unicode punctuation character.
    /// </summary>
    public static Parser<char, char> Punctuation { get; } = Token(c => char.IsPunctuation(c)).Labelled("punctuation");

    /// <summary>
    /// A parser that parses and returns a single Unicode symbol character.
    /// </summary>
    public static Parser<char, char> Symbol { get; } = Token(c => char.IsSymbol(c)).Labelled("symbol");

    /// <summary>
    /// A parser that parses and returns a single Unicode separator character.
    /// </summary>
    public static Parser<char, char> Separator { get; } = Token(c => char.IsSeparator(c)).Labelled("separator");
}
