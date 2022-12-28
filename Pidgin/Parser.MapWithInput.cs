using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Returns a parser which runs the current parser and applies a selector function.
    /// The selector function receives a <see cref="ReadOnlySpan{TToken}"/> as its first argument, and the result of the current parser as its second argument.
    /// The <see cref="ReadOnlySpan{TToken}"/> represents the sequence of input tokens which were consumed by the parser.
    ///
    /// This allows you to write "pattern"-style parsers which match a sequence of tokens and return a view of the part of the input stream which they matched.
    ///
    /// This function is an alternative name for <see cref="Slice"/>.
    /// </summary>
    /// <param name="selector">
    /// A selector function which computes a result of type <typeparamref name="U"/>.
    /// The arguments of the selector function are a <see cref="ReadOnlySpan{TToken}"/> containing the sequence of input tokens which were consumed by this parser,
    /// and the result of this parser.
    /// </param>
    /// <typeparam name="U">The result type.</typeparam>
    /// <returns>A parser which runs the current parser and applies a selector function.</returns>
    public Parser<TToken, U> MapWithInput<U>(ReadOnlySpanFunc<TToken, T, U> selector)
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        return new MapWithInputParser<TToken, T, U>(this, selector);
    }
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal class MapWithInputParser<TToken, T, U> : Parser<TToken, U>
{
    private readonly Parser<TToken, T> _parser;
    private readonly ReadOnlySpanFunc<TToken, T, U> _selector;

    public MapWithInputParser(Parser<TToken, T> parser, ReadOnlySpanFunc<TToken, T, U> selector)
    {
        _parser = parser;
        _selector = selector;
    }

    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out U result)
    {
        var start = state.Location;

        var bookmark = state.Bookmark();  // don't discard input buffer

        if (!_parser.TryParse(ref state, ref expecteds, out var result1))
        {
            state.DiscardBookmark(bookmark);
            result = default;
            return false;
        }

        var delta = state.Location - start;
        result = _selector(state.LookBehind(delta), result1);

        state.DiscardBookmark(bookmark);

        return true;
    }
}
