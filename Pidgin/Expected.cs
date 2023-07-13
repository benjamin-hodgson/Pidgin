using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Pidgin;

/// <summary>
/// Represents a parsing expectation for error reporting.
/// Expected values are either a sequence of expected tokens (in which case <c>Label == null &amp;&amp; Tokens != null</c>),
/// a custom-named parser (<c>Label != null &amp;&amp; Tokens == null</c>),
/// or the end of the input stream (<c>Label == null &amp;&amp; Tokens == null</c>).
/// </summary>
/// <typeparam name="TToken">The type of tokens in the parser's input stream.</typeparam>
public readonly struct Expected<TToken> : IEquatable<Expected<TToken>>, IComparable<Expected<TToken>>
{
    /// <summary>
    /// The custom name of the parser that produced this error, or null if the expectation was a sequence of tokens.
    /// </summary>
    /// <returns>The label.</returns>
    public string? Label { get; }

    /// <summary>
    /// The sequence of tokens that were expected at the point of the error, null if the parser had a custom name.
    /// </summary>
    /// <returns>The sequence of tokens that were expected.</returns>
    public ImmutableArray<TToken> Tokens { get; }

    /// <summary>
    /// Did the parser expect the end of the input stream?.
    /// </summary>
    /// <returns>True if the parser expected the end of the input stream.</returns>
    public bool IsEof => Label == null && Tokens == null;

    internal Expected(string label)
    {
        Label = label;
        Tokens = default;
    }

    internal Expected(ImmutableArray<TToken> tokens)
    {
        Label = null;
        Tokens = tokens;
    }

    private static readonly bool _isChar = typeof(TToken).Equals(typeof(char));

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

        var tokens = Tokens!;
        sb.Append('"');
        if (_isChar)
        {
            sb.Append(UnsafeCastToChar(tokens.AsSpan()));
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
        && ((Tokens.IsDefault && Tokens.IsDefault)
            || (!Tokens.IsDefault && !Tokens.IsDefault && EnumerableExtensions.Equal(Tokens, other.Tokens))
        );

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is not null
        && obj is Expected<TToken> expected
        && Equals(expected);

    /// <summary>Equality operator.</summary>
    /// <param name="left">The left <see cref="Expected{TToken}"/>.</param>
    /// <param name="right">The right <see cref="Expected{TToken}"/>.</param>
    public static bool operator ==(Expected<TToken> left, Expected<TToken> right)
        => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    /// <param name="left">The left <see cref="Expected{TToken}"/>.</param>
    /// <param name="right">The right <see cref="Expected{TToken}"/>.</param>
    public static bool operator !=(Expected<TToken> left, Expected<TToken> right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Label, EnumerableExtensions.GetHashCode(Tokens));

    /// <inheritdoc/>
    public int CompareTo(Expected<TToken> other)
    {
        // Label < Tokens < EOF
        if (Label != null)
        {
            if (other.Label != null)
            {
                return string.Compare(Label, other.Label, StringComparison.Ordinal);
            }

            return -1;
        }

        if (!Tokens.IsDefault)
        {
            if (other.Label != null)
            {
                return 1;
            }

            if (!other.Tokens.IsDefault)
            {
                return EnumerableExtensions.Compare(Tokens, other.Tokens);
            }

            return -1;
        }

        if (other.Label == null && other.Tokens == null)
        {
            return 0;
        }

        return 1;
    }

    /// <summary>Comparison operator.</summary>
    /// <param name="left">The left <see cref="Expected{TToken}"/>.</param>
    /// <param name="right">The right <see cref="Expected{TToken}"/>.</param>
    public static bool operator >(Expected<TToken> left, Expected<TToken> right)
        => left.CompareTo(right) > 0;

    /// <summary>Comparison operator.</summary>
    /// <param name="left">The left <see cref="Expected{TToken}"/>.</param>
    /// <param name="right">The right <see cref="Expected{TToken}"/>.</param>
    public static bool operator <(Expected<TToken> left, Expected<TToken> right)
        => left.CompareTo(right) < 0;

    /// <summary>Comparison operator.</summary>
    /// <param name="left">The left <see cref="Expected{TToken}"/>.</param>
    /// <param name="right">The right <see cref="Expected{TToken}"/>.</param>
    public static bool operator >=(Expected<TToken> left, Expected<TToken> right)
        => left.CompareTo(right) >= 0;

    /// <summary>Comparison operator.</summary>
    /// <param name="left">The left <see cref="Expected{TToken}"/>.</param>
    /// <param name="right">The right <see cref="Expected{TToken}"/>.</param>
    public static bool operator <=(Expected<TToken> left, Expected<TToken> right)
        => left.CompareTo(right) <= 0;

    private static ReadOnlySpan<char> UnsafeCastToChar(ReadOnlySpan<TToken> span)
        => MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.As<TToken, char>(ref MemoryMarshal.GetReference(span)),
            span.Length
        );
}
