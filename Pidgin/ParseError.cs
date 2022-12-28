using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Pidgin;

/// <summary>
/// Represents an error encountered during parsing.
/// </summary>
/// <typeparam name="TToken">The type of tokens in the input stream.</typeparam>
public class ParseError<TToken> : IEquatable<ParseError<TToken>>
{
    /// <summary>
    /// Was the parse error due to encountering the end of the input stream while parsing?.
    /// </summary>
    /// <returns>True if and only if the parse error was due to encountering the end of the input stream while parsing.</returns>
    public bool EOF { get; }

    /// <summary>
    /// The token which caused the parse error.
    /// </summary>
    /// <returns>The token which caused the parse error, or <see cref="Maybe.Nothing{TToken}()"/> if the parse error was not caused by an unexpected token.</returns>
    public Maybe<TToken> Unexpected { get; }

    /// <summary>
    /// A collection of expected inputs.
    /// </summary>
    /// <returns>The collection of expected inputs.</returns>
    public IEnumerable<Expected<TToken>> Expected { get; }

    /// <summary>
    /// The offset in the input stream at which the parse error occurred.
    /// </summary>
    public int ErrorOffset { get; }

    /// <summary>
    /// The offset in the input stream at which the parse error occurred.
    /// </summary>
    public SourcePosDelta ErrorPosDelta { get; }

    /// <summary>
    /// The position in the input stream at which the parse error occurred.
    /// </summary>
    public SourcePos ErrorPos => new SourcePos(1, 1) + ErrorPosDelta;

    /// <summary>
    /// A custom error message.
    /// </summary>
    /// <returns>A custom error message, or null if the error was created without a custom error message.</returns>
    public string? Message { get; }

    internal ParseError(
        Maybe<TToken> unexpected,
        bool eof,
        ImmutableArray<Expected<TToken>> expected,
        int errorOffset,
        SourcePosDelta errorPosDelta,
        string? message
    )
    {
        Unexpected = unexpected;
        EOF = eof;
        Expected = expected;
        ErrorOffset = errorOffset;
        ErrorPosDelta = errorPosDelta;
        Message = message;
    }

    /// <summary>
    /// Render the parse error as a string.
    /// </summary>
    /// <returns>An error message.</returns>
    public override string ToString() => RenderErrorMessage();

    /// <summary>
    /// Render the parse error as a string.
    /// </summary>
    /// <param name="initialSourcePos">The <see cref="SourcePos"/> of the beginning of the parse.</param>
    /// <returns>An error message.</returns>
    public string ToString(SourcePos initialSourcePos) => RenderErrorMessage(initialSourcePos);

    /// <summary>
    /// Render the parse error as a string.
    /// </summary>
    /// <param name="initialSourcePos">The <see cref="SourcePos"/> of the beginning of the parse.</param>
    /// <returns>An error message.</returns>
    public string RenderErrorMessage(SourcePos? initialSourcePos = null)
    {
        var pos = (initialSourcePos ?? new SourcePos(1, 1)) + ErrorPosDelta;
        var sb = new StringBuilder();

        sb.Append("Parse error.");
        if (Message != null)
        {
            sb.Append(Environment.NewLine);
            sb.Append("    ");
            sb.Append(Message);
        }

        if (EOF || Unexpected.HasValue)
        {
            sb.Append(Environment.NewLine);
            sb.Append("    unexpected ");
            sb.Append(EOF ? "EOF" : Unexpected.Value!.ToString());
        }

        if (Expected?.Any(e => e.Tokens.IsDefault || e.Tokens.Length != 0) == true)
        {
            sb.Append(Environment.NewLine);
            sb.Append("    expected ");
            AppendExpectedString(Expected, sb);
        }

        sb.Append(Environment.NewLine);
        sb.Append("    at line ");
        sb.Append(pos.Line);
        sb.Append(", col ");
        sb.Append(pos.Col);

        return sb.ToString();
    }

    private static void AppendExpectedString(IEnumerable<Expected<TToken>> expected, StringBuilder sb)
    {
        var count = 0;
        var last = default(Expected<TToken>);
        foreach (var x in expected)
        {
            if (count >= 2)
            {
                sb.Append(", ");
            }

            if (count >= 1)
            {
                last.AppendTo(sb);
            }

            last = x;
            count++;
        }

        if (count >= 2)
        {
            sb.Append(", or ");
        }

        last.AppendTo(sb);
    }

    /// <inheritdoc/>
    public bool Equals(ParseError<TToken>? other)
        => Unexpected.Equals(other?.Unexpected)
        && EOF == other.EOF
        && ((Expected == null && other.Expected == null) || Expected!.SequenceEqual(other.Expected))
        && ErrorOffset.Equals(other.ErrorOffset)
        && ErrorPos.Equals(other.ErrorPos)
        && object.Equals(Message, other.Message);

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is ParseError<TToken> error
        && Equals(error);

    /// <summary>Equality operator.</summary>
    /// <param name="left">The left <see cref="ParseError{TToken}"/>.</param>
    /// <param name="right">The right <see cref="ParseError{TToken}"/>.</param>
    public static bool operator ==(ParseError<TToken> left, ParseError<TToken> right)
        => (left is null && right is null) || (left is not null && left.Equals(right));

    /// <summary>Inequality operator.</summary>
    /// <param name="left">The left <see cref="ParseError{TToken}"/>.</param>
    /// <param name="right">The right <see cref="ParseError{TToken}"/>.</param>
    public static bool operator !=(ParseError<TToken> left, ParseError<TToken> right)
        => !(left == right);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Unexpected, EOF, Expected, ErrorOffset, ErrorPos, Message);
}
