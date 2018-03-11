using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using LExpression = System.Linq.Expressions.Expression;

namespace Pidgin
{
    /// <summary>
    /// Represents a parsing expectation for error reporting.
    /// Expected values are either a sequence of expected tokens (in which case <c>Label == null &amp;&amp; Tokens != null</c>),
    /// a custom-named parser (<c>Label != null &amp;&amp; Tokens == null</c>),
    /// or the end of the input stream (<c>Label == null &amp;&amp; Tokens == null</c>)
    /// </summary>
    public readonly struct Expected<TToken> : IEquatable<Expected<TToken>>, IComparable<Expected<TToken>>
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
        /// <returns>True if the parser expected the end of the input stream</returns>
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
        internal void AppendTo(StringBuilder sb)
        {
            if (IsEof)
            {
                sb.Append("end of input");
                return;
            }
            if (Label != null)
            {
                sb.Append(Label);
                return;
            }

            var tokens = Tokens;
            sb.Append('"');
            if (IsChar)
            {
                foreach (var token in tokens)
                {
                    var chr = CastToChar(token);
                    sb.Append(chr);
                }
            }
            else
            {
                var notFirst = false;
                foreach (var token in tokens)
                {
                    if (notFirst)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(token);
                    notFirst = true;
                }
            }
            sb.Append('"');
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (IsEof)
            {
                return "EOF";
            }
            var sb = new StringBuilder();
            sb.Append(Label != null ? "Label: " : "Tokens: ");
            AppendTo(sb);
            return sb.ToString();
        }

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

        private static Func<TToken, char> CastToChar { get; } = GetCastToCharMethod();
        private static Func<TToken, char> GetCastToCharMethod()
        {
            var param = LExpression.Parameter(typeof(TToken));
            return LExpression.Lambda<Func<TToken, char>>(param, param).Compile();
        }
    }
}