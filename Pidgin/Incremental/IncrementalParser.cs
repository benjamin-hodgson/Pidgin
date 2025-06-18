using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Pidgin.Configuration;

namespace Pidgin.Incremental;

/// <summary>
/// Extension methods for constructing and running <em>incremental</em> parsers
/// - parsers which reuse the results of previous parses.
/// </summary>
public static class IncrementalParser
{
    /// <summary>
    /// Creates a parser which runs <paramref name="parser"/> incrementally.
    /// </summary>
    /// <typeparam name="TToken">The type of tokens in the input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <param name="parser">The parser to run incrementally.</param>
    /// <returns>A parser which runs <paramref name="parser"/> incrementally.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="parser"/> is null.</exception>
    public static Parser<TToken, T> Incremental<TToken, T>(this Parser<TToken, T> parser)
        where T : class, IShiftable<T>
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        return new IncrementalParser<TToken, T>(parser);
    }

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>,
    /// reusing results from the supplied <paramref name="context"/>
    /// when possible.
    /// </summary>
    /// <example>
    /// <code>
    /// // First parse (no context)
    /// var (context, result) = parser.ParseIncrementally(input, null).Value;
    ///
    /// // ...user edits input, producing newInput and an Edit instance...
    /// var edit = new Edit(new LocationRange(5, 3), 2); // example edit
    /// var newContext = context.AddEdit(edit);
    ///
    /// // Incremental parse after edit
    /// var (context2, result2) = parser.ParseIncrementally(newInput, newContext);
    /// </code>
    /// </example>
    /// <typeparam name="TToken">The type of tokens in the input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <param name="parser">The parser to run incrementally.</param>
    /// <param name="input">The input token stream to parse.</param>
    /// <param name="context">
    /// The incremental parse context containing cached results from the previous parse.
    /// Can be null if this is the first time running this parser.
    /// </param>
    /// <param name="configuration">
    /// The parser configuration, or null to use the default configuration.
    /// </param>
    /// <returns>
    /// A <see cref="Result{TToken, TValue}"/> containing a tuple of the new <see cref="IncrementalParseContext"/> and the parsed value.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="parser"/> or <paramref name="input"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the result cache is unexpectedly null.</exception>
    // todo: ParseIncrementallyOrThrow, overloads for string, span, etc
    public static (IncrementalParseContext Context, T Value) ParseIncrementallyOrThrow<TToken, T>(
        this Parser<TToken, T> parser,
        ReadOnlySpan<TToken> input,
        IncrementalParseContext? context,
        IConfiguration<TToken>? configuration = null
    )
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        var state = new ParseState<TToken>(configuration ?? Configuration.Configuration.Default<TToken>(), input)
        {
            IncrementalParseContext = context,
            NewResultCache = new()
        };

        var result = parser.ParseOrThrow(ref state);

        if (state.NewResultCache == null)
        {
            throw new InvalidOperationException("NewResultCache was null. Please report this as a bug in Pidgin");
        }

        var newCtx = new IncrementalParseContext(ImmutableList<EditInfo>.Empty, state.NewResultCache.Build());

        return (newCtx, result);
    }

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>,
    /// reusing results from the supplied <paramref name="context"/>
    /// when possible.
    /// </summary>
    /// <example>
    /// <code>
    /// // First parse (no context)
    /// var (context, result) = parser.ParseIncrementally(input, null).Value;
    ///
    /// // ...user edits input, producing newInput and an Edit instance...
    /// var edit = new Edit(new LocationRange(5, 3), 2); // example edit
    /// var newContext = context.AddEdit(edit);
    ///
    /// // Incremental parse after edit
    /// var (context2, result2) = parser.ParseIncrementally(newInput, newContext);
    /// </code>
    /// </example>
    /// <typeparam name="TToken">The type of tokens in the input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <param name="parser">The parser to run incrementally.</param>
    /// <param name="input">The input token stream to parse.</param>
    /// <param name="context">
    /// The incremental parse context containing cached results from the previous parse.
    /// Can be null if this is the first time running this parser.
    /// </param>
    /// <param name="configuration">
    /// The parser configuration, or null to use the default configuration.
    /// </param>
    /// <returns>
    /// A <see cref="Result{TToken, TValue}"/> containing a tuple of the new <see cref="IncrementalParseContext"/> and the parsed value.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="parser"/> or <paramref name="input"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the result cache is unexpectedly null.</exception>
    // todo: ParseIncrementallyOrThrow, overloads for string, span, etc
    public static Result<TToken, (IncrementalParseContext Context, T Value)> ParseIncrementally<TToken, T>(
        this Parser<TToken, T> parser,
        ITokenStream<TToken> input,
        IncrementalParseContext context,
        IConfiguration<TToken>? configuration = null
    )
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var state = new ParseState<TToken>(configuration ?? Configuration.Configuration.Default<TToken>(), input)
        {
            IncrementalParseContext = context,
            NewResultCache = new()
        };

        var result = parser.Parse(ref state);

        if (state.NewResultCache == null)
        {
            throw new InvalidOperationException("NewResultCache was null. Please report this as a bug in Pidgin");
        }

        var newCtx = new IncrementalParseContext(ImmutableList<EditInfo>.Empty, state.NewResultCache.Build());

        return result.Select(t => (newCtx, t));
    }
}

internal class IncrementalParser<TToken, T>(Parser<TToken, T> parser) : Parser<TToken, T>
    where T : class, IShiftable<T>
{
    private readonly Parser<TToken, T> _parser = parser;

    public override bool TryParse(
        ref ParseState<TToken> state,
        ref PooledList<Expected<TToken>> expecteds,
        [MaybeNullWhen(false)] out T result
    )
    {
        var ctx = state.IncrementalParseContext;

        // if ctx is null, we're using an IncrementalParser in a non-incremental
        // context (either: first time calling ParseIncrementally, or we're not
        // in ParseIncrementally at all)
        if (ctx != null)
        {
            var unshifted = ctx.Unshift(state.Location);
            var found = ctx.ResultCache.TryGetValue(unshifted, this);

            if (found != null && ctx.IsValid(found.LookaroundRange))
            {
                // make the (old) found result align with the (new) current location
                var shiftedFound = found.ShiftBy(state.Location - found.ConsumedRange.Start);
                shiftedFound.ResolvePendingShifts<T>();
                if (shiftedFound.TryGetResult(out result))
                {
                    state.NewResultCache?.Add(this, shiftedFound);
                    state.Advance((int)shiftedFound.ConsumedRange.Length);
                    return true;
                }
            }
        }

        var builder = state.NewResultCache;

        if (builder == null)
        {
            // If NewResultCache is null, user is not using ParseIncrementally
            return _parser.TryParse(ref state, ref expecteds, out result);
        }

        var startLocation = state.Location;
        var maxLookahead = startLocation;
        void OnLookahead(long lookaheadTo)
        {
            maxLookahead = Math.Max(maxLookahead, lookaheadTo);
        }

        builder.Start();
        state.OnLookahead(OnLookahead);

        var success = _parser.TryParse(ref state, ref expecteds, out result);

        state.OffLookahead(OnLookahead);

        if (success)
        {
            // If we later backtrack over this parser,
            // its parse result will remain cached as if
            // it were a child of the parent parser. I think that's fine.
            maxLookahead = Math.Max(maxLookahead, state.Location);
            var lookaroundRange = new LocationRange(startLocation, maxLookahead - startLocation);
            var consumedRange = new LocationRange(startLocation, state.Location - startLocation);
            builder.End(this, consumedRange, lookaroundRange, result!);
        }
        else
        {
            builder.Discard();
        }

        return success;
    }
}
