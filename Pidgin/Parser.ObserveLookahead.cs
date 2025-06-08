using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public partial class Parser<TToken, T>
{
    /// <summary>
    /// Wraps the current parser and returns a new parser that, in addition to
    /// parsing the value, also returns the amount of the input examined by the
    /// parser (lookahead).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This can be useful for scenarios like incremental parsing, where
    /// knowing the full range of input examined by a parser is important.
    /// </para>
    /// <para>
    /// The range of input examined by the parser is not the same as the
    /// range consumed by the parser. For example, the parser may read (look
    /// ahead at) a certain number of tokens, backtrack, and then consume a
    /// smaller number before returning a result. The returned <c>Amount</c>
    /// is the total amount of the input examined by the parser, including the
    /// input which was consumed.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A parser that returns a tuple containing the parsed value, the start
    /// position, and the amount of input examined by the parser.
    /// </returns>
    /// <example name="ObserveLookahead example">
    /// Note that a parser may look ahead at more tokens than it ends up
    /// consuming. In this example the <c>some string</c> parser looks ahead
    /// at 11 characters, even though it only actually consumes 8 characters
    /// before failing (and backtracking).
    /// <code doctest="true">
    /// var parser = Try(String("some string")).Or(String("some"))
    ///     .ObserveLookahead();
    /// Console.WriteLine(parser.ParseOrThrow("some strong"));
    /// // Output:
    /// // (some, 11)
    /// </code>
    /// </example>
    public Parser<TToken, (T Value, long Amount)> ObserveLookahead()
        => new ObserveLookaheadParser<TToken, T>(this);
}

internal class ObserveLookaheadParser<TToken, T>
    : Parser<TToken, (T Value, long Amount)>
{
    private readonly Parser<TToken, T> _parser;

    public ObserveLookaheadParser(Parser<TToken, T> parser)
    {
        _parser = parser;
    }

    public override bool TryParse(
        ref ParseState<TToken> state,
        ref PooledList<Expected<TToken>> expecteds,
        [MaybeNullWhen(false)] out (T Value, long Amount) result
    )
    {
        var start = state.Location;
        var end = state.Location;

        void OnLookahead(long location)
        {
            end = Math.Max(end, location);
        }

        state.PushOnLookaheadAction(OnLookahead);

        var success = _parser.TryParse(ref state, ref expecteds, out var value);

        state.PopOnLookaheadAction(OnLookahead);

        result = (value!, Math.Max(end, state.Location) - start);
        return success;
    }
}
