using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Pidgin
{
    /// <summary>
    /// Represents an error encountered during parsing.
    /// </summary>
    /// <typeparam name="TToken">The type of tokens in the input stream</typeparam>
    public class ParseError<TToken> : IEquatable<ParseError<TToken>>
    {
        /// <summary>
        /// Was the parse error due to encountering the end of the input stream while parsing?
        /// </summary>
        /// <returns>True if and only if the parse error was due to encountering the end of the input stream while parsing</returns>
        public bool EOF { get; }

        /// <summary>
        /// The token which caused the parse error.
        /// </summary>
        /// <returns>The token which caused the parse error, or <see cref="Maybe.Nothing{TToken}()"/> if the parse error was not caused by an unexpected token.</returns>
        public Maybe<TToken> Unexpected { get; }

        /// <summary>
        /// A collection of expected inputs
        /// </summary>
        /// <returns>The collection of expected inputs</returns>
        public IEnumerable<Expected<TToken>> Expected { get; }

        /// <summary>
        /// The offset in the input stream at which the parse error occurred
        /// </summary>
        /// <returns>The offset in the input stream at which the parse error occurred</returns>
        public SourcePosDelta ErrorPosDelta { get; }

        /// <summary>
        /// The position in the input stream at which the parse error occurred
        /// </summary>
        /// <returns>The position in the input stream at which the parse error occurred</returns>
        public SourcePos ErrorPos => new SourcePos(1, 1) + ErrorPosDelta;

        /// <summary>
        /// A custom error message
        /// </summary>
        /// <returns>A custom error message, or null if the error was created without a custom error message</returns>
        public string? Message { get; }

        internal ParseError(Maybe<TToken> unexpected, bool eof, ImmutableArray<Expected<TToken>> expected, SourcePosDelta errorPosDelta, string? message)
        {
            Unexpected = unexpected;
            EOF = eof;
            Expected = expected;
            ErrorPosDelta = errorPosDelta;
            Message = message;
        }

        /// <summary>
        /// Render the parse error as a string
        /// </summary>
        /// <returns>An error message</returns>
        public override string ToString() => RenderErrorMessage();

        /// <summary>
        /// Render the parse error as a string
        /// </summary>
        /// <returns>An error message</returns>
        public string ToString(SourcePos initialSourcePos) => RenderErrorMessage(initialSourcePos);

        /// <summary>
        /// Render the parse error as a string
        /// </summary>
        /// <returns>An error message</returns>
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
            && (Expected == null && other.Expected == null || Expected!.SequenceEqual(other.Expected))
            && ErrorPos.Equals(other.ErrorPos)
            && object.Equals(Message, other.Message);

        /// <inheritdoc/>
        public override bool Equals(object? other)
            => other is ParseError<TToken> error
            && Equals(error);

        /// <inheritdoc/>
        public static bool operator ==(ParseError<TToken> left, ParseError<TToken> right)
            => left.Equals(right);
        /// <inheritdoc/>
        public static bool operator !=(ParseError<TToken> left, ParseError<TToken> right)
            => !left.Equals(right);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + Unexpected.GetHashCode();
                hash = hash * 23 + EOF.GetHashCode();
                hash = hash * 23 + Expected.GetHashCode();
                hash = hash * 23 + ErrorPos.GetHashCode();
                hash = hash * 23 + Message?.GetHashCode() ?? 0;
                return hash;
            }
        }
    }
}
