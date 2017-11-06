using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin
{
    /// <summary>
    /// Represents a parsing expectation for error reporting.
    /// Expected values are either a sequence of expected tokens (Label == null &amp;&amp; Tokens != null),
    /// a custom-named parser (Label != null &amp;&amp; Tokens == null),
    /// or the end of the input stream (Label == null &amp;&amp; Tokens == null)
    /// </summary>
    public struct Expected<TToken> : IEquatable<Expected<TToken>>, IComparable<Expected<TToken>>
    {
        /// <summary>
        /// The custom name of the parser that produced this error, or null if the expectation was a sequence of tokens.
        /// </summary>
        /// <returns>The label</returns>
        public string Label { get; }
        /// <summary>
        /// The sequence of tokens that were expected at the point of the error, null if the parser had a custom name.
        /// </summary>
        /// <returns>The sequence of tokens that were expected</returns>
        public IEnumerable<TToken> Tokens => InternalTokens?.ToImmutableArray();
        internal Rope<TToken> InternalTokens { get; }
        /// <summary>
        /// Did the parser expect the end of the input stream?
        /// </summary>
        /// <returns>The sequence of tokens that were expected</returns>
        public bool IsEof => Label == null && InternalTokens == null;
        
        internal Expected(string label)
        {
            Label = label;
            InternalTokens = null;
        }
        internal Expected(Rope<TToken> tokens)
        {
            Label = null;
            InternalTokens = tokens;
        }

        private static readonly bool IsChar = typeof(TToken).Equals(typeof(char));
        internal string Render()
        {
            var tokens = Tokens;
            return IsEof
                ? "end of input"
                : Label != null
                    ? Label
                    : IsChar
                        ? string.Concat('"', string.Concat(tokens), '"')
                        : string.Concat('"', string.Join(", ", tokens), '"');
        }

        /// <inheritdoc/>
        public override string ToString()
            => IsEof
                ? "EOF"
                : (Label != null ? "Label: " : "Tokens: ") + Render();

        /// <inheritdoc/>
        public bool Equals(Expected<TToken> other)
            => object.Equals(Label, other.Label)
            && ((ReferenceEquals(null, InternalTokens) && ReferenceEquals(null, other.InternalTokens))
                || (!ReferenceEquals(null, InternalTokens) && !ReferenceEquals(null, other.InternalTokens) && InternalTokens.Equals(other.InternalTokens))
            );

        /// <inheritdoc/>
        public override bool Equals(object other)
            => !ReferenceEquals(null, other)
            && other is Expected<TToken>
            && Equals((Expected<TToken>)other);

        /// <inheritdoc/>
        public static bool operator ==(Expected<TToken> left, Expected<TToken> right)
            => left.Equals(right);
        /// <inheritdoc/>
        public static bool operator !=(Expected<TToken> left, Expected<TToken> right)
            => !left.Equals(right);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Label?.GetHashCode() ?? 0;
                hash = hash * 23 + InternalTokens?.GetHashCode() ?? 0;
                return hash;
            }
        }

        /// <inheritdoc/>
        public int CompareTo(Expected<TToken> other)
        {
            // Label < Tokens < EOF
            if (Label != null)
            {
                if (other.Label != null)
                {
                    return string.Compare(Label, other.Label);
                }
                return -1;
            }
            if (InternalTokens != null)
            {
                if (other.Label != null)
                {
                    return 1;
                }
                if (other.InternalTokens != null)
                {
                    return InternalTokens.CompareTo(other.InternalTokens);
                }
                return -1;
            }
            if (other.Label == null && other.InternalTokens == null)
            {
                return 0;
            }
            return 1;
        }

        /// <inheritdoc/>
        public static bool operator >(Expected<TToken> left, Expected<TToken> right)
            => left.CompareTo(right) > 0;
        /// <inheritdoc/>
        public static bool operator <(Expected<TToken> left, Expected<TToken> right)
            => left.CompareTo(right) < 0;
        /// <inheritdoc/>
        public static bool operator >=(Expected<TToken> left, Expected<TToken> right)
            => left.CompareTo(right) >= 0;
        /// <inheritdoc/>
        public static bool operator <=(Expected<TToken> left, Expected<TToken> right)
            => left.CompareTo(right) <= 0;
    }
}