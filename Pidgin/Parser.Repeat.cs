using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pidgin;

public static partial class Parser
{
    /// <summary>
    /// Creates a parser which applies <paramref name="parser"/> <paramref name="count"/> times,
    /// packing the resulting <c>char</c>s into a <c>string</c>.
    ///
    /// <para>
    /// Equivalent to <c>parser.Repeat(count).Select(string.Concat)</c>.
    /// </para>
    /// </summary>
    /// <typeparam name="TToken">The type of tokens in the parser's input stream.</typeparam>
    /// <param name="parser">The parser.</param>
    /// <param name="count">The number of times to apply the parser.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> was less than 0.</exception>
    /// <returns>
    /// A parser which applies <paramref name="parser"/> <paramref name="count"/> times,
    /// packing the resulting <c>char</c>s into a <c>string</c>.
    /// </returns>
    public static Parser<TToken, string> RepeatString<TToken>(this Parser<TToken, char> parser, int count)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        }

        return parser.Accept(new RepeatStringParserFactory<TToken>(count));
    }
}

internal class RepeatStringParserFactory<TToken>(int count)
    : IUnboxer<TToken, char, BoxParser<TToken, string>>
{
    public BoxParser<TToken, string> Unbox<Next>(BoxParser<TToken, char>.Of<Next> box)
        where Next : IParser<TToken, char>
        => BoxParser<TToken, string>.Create(new RepeatStringParser<Next, TToken>(box, count));
}

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser which applies the current parser <paramref name="count"/> times.
    /// </summary>
    /// <param name="count">The number of times to apply the current parser.</param>
    /// <exception cref="InvalidOperationException"><paramref name="count"/> is less than 0.</exception>
    /// <returns>A parser which applies the current parser <paramref name="count"/> times.</returns>
    public Parser<TToken, IEnumerable<T>> Repeat(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        }

        return Parser<TToken>.Sequence(Enumerable.Repeat(this, count));
    }
}

internal readonly struct RepeatStringParser<Next, TToken> : IParser<TToken, string>
    where Next : IParser<TToken, char>
{
    private readonly BoxParser<TToken, char>.Of<Next> _parser;
    private readonly int _count;

    public RepeatStringParser(BoxParser<TToken, char>.Of<Next> parser, int count)
    {
        _parser = parser;
        _count = count;
    }

    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out string result)
    {
        var builder = new InplaceStringBuilder(_count);

        for (var i = 0; i < _count; i++)
        {
            var success = _parser.Value.TryParse(ref state, ref expecteds, out var result1);

            if (!success)
            {
                result = null;
                return false;
            }

            builder.Append(result1);
        }

        result = builder.ToString();
        return true;
    }
}
