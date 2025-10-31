using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Pidgin;

public static partial class Parser
{
    /// <summary>
    /// Creates a parser that parses and returns a literal string.
    /// </summary>
    /// <param name="str">The string to parse.</param>
    /// <returns>A parser that parses and returns a literal string.</returns>
    ///
    [SuppressMessage("design", "CA1720:Identifier contains type name", Justification = "Would be a breaking change")]
    public static Parser<char, string> String(string str)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        return Parser<char>.Sequence(str);
    }

    /// <summary>
    /// Creates a parser that parses and returns a literal string, in a case insensitive manner.
    /// The parser returns the actual string parsed.
    /// </summary>
    /// <param name="str">The string to parse.</param>
    /// <returns>A parser that parses and returns a literal string, in a case insensitive manner.</returns>
    public static Parser<char, string> CIString(string str)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        return new CIStringParser(str);
    }
}

internal sealed class CIStringParser : Parser<char, string>
{
    private readonly string _value;
    private Expected<char> _expected;

    private Expected<char> Expected
    {
        get
        {
            if (_expected.Tokens.IsDefault)
            {
                _expected = new Expected<char>(_value.ToImmutableArray());
            }

            return _expected;
        }
    }

    public CIStringParser(string value)
    {
        _value = value;
    }

    // Consume the two inputs one code point at a time.
    // Read the codepoints into a span (so, each span
    // will either be a single char or a valid surrogate
    // pair). Then use `Span<char>.Equals` with a
    // StringComparison to do the case insensitive comparison.
    //
    // todo: take a fast path for long sequences of ascii in
    // the input.
    public sealed override bool TryParse(ref ParseState<char> state, ref PooledList<Expected<char>> expecteds, [MaybeNullWhen(false)] out string result)
    {
        var resultBuilder = new StringBuilder(_value.Length);
        var i = 0;
        while (i < _value.Length && state.HasCurrent)
        {
            ReadOnlySpan<char> span;
            ReadOnlySpan<char> valueSpan;

            if (char.IsHighSurrogate(state.Current))
            {
                span = state.LookAhead(2);
                if (span.Length < 2 || !char.IsLowSurrogate(span[1]))
                {
                    state.SetError(Maybe.Just(state.Current), false, state.Location, "invalid unicode in input");
                    expecteds.Add(Expected);
                    result = null;
                    return false;
                }
            }
            else
            {
                span = state.LookAhead(1);
            }

            if (char.IsHighSurrogate(_value[i]))
            {
                if (_value.Length < i + 2 || !char.IsLowSurrogate(_value[i + 1]))
                {
                    // programmer error - invalid CIString usage
                    throw new InvalidOperationException("Invalid unicode in CIString");
                }

                valueSpan = _value.AsSpan()[i .. (i + 2)];
            }
            else
            {
                valueSpan = _value.AsSpan()[i .. (i + 1)];
            }

            if (!span.Equals(valueSpan, StringComparison.InvariantCultureIgnoreCase))
            {
                state.SetError(Maybe.Just(state.Current), false, state.Location, null);
                expecteds.Add(Expected);
                result = null;
                return false;
            }

            resultBuilder.Append(span);

            state.Advance(span.Length);
            i += valueSpan.Length;
        }

        if (i < _value.Length)
        {
            // strings matched but reached EOF
            state.SetError(Maybe.Nothing<char>(), true, state.Location, null);
            expecteds.Add(Expected);
            result = null;
            return false;
        }

        result = resultBuilder.ToString();
        return true;
    }
}
