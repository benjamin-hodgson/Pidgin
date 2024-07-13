using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser which applies this parser zero or more times
    /// until <paramref name="terminator"/> succeeds.
    /// Fails if this parser fails or if <paramref name="terminator"/>
    /// fails after consuming input.
    /// The return value of <paramref name="terminator"/> is ignored.
    /// </summary>
    /// <remarks>
    /// <c>p.Until(q)</c> is equivalent to
    /// <c>p.ManyThen(q).Select(t => t.Item1)</c>.
    /// </remarks>
    /// <typeparam name="U">
    /// The return type of <paramref name="terminator"/>.
    /// </typeparam>
    /// <param name="terminator">A parser to parse a terminator.</param>
    /// <returns>A parser which applies this parser repeatedly until <paramref name="terminator"/> succeeds.</returns>
    public Parser<TToken, IEnumerable<T>> Until<U>(Parser<TToken, U> terminator)
    {
        if (terminator == null)
        {
            throw new ArgumentNullException(nameof(terminator));
        }

        return ManyThen(terminator).Select((Func<(IEnumerable<T>, U), IEnumerable<T>>)(tup => tup.Item1));
    }

    /// <summary>
    /// Creates a parser which applies this parser zero or more times
    /// until <paramref name="terminator"/> succeeds.
    /// Fails if this parser fails or if <paramref name="terminator"/>
    /// fails after consuming input.
    /// </summary>
    /// <typeparam name="U">The return type of <paramref name="terminator"/>.</typeparam>
    /// <param name="terminator">A parser to parse a terminator.</param>
    /// <returns>A parser which applies this parser repeatedly until <paramref name="terminator"/> succeeds.</returns>
    public Parser<TToken, (IEnumerable<T>, U)> ManyThen<U>(Parser<TToken, U> terminator)
    {
        if (terminator == null)
        {
            throw new ArgumentNullException(nameof(terminator));
        }

        return terminator.Select(t => (Enumerable.Empty<T>(), t))
            .Or(AtLeastOnceThen(terminator));
    }

    /// <summary>
    /// Creates a parser which applies this parser one or more times until
    /// <paramref name="terminator"/> succeeds.
    /// Fails if this parser fails or if <paramref name="terminator"/>
    /// fails after consuming input.
    /// The return value of <paramref name="terminator"/> is ignored.
    /// </summary>
    /// <remarks>
    /// <c>p.AtLeastOnceUntil(q)</c> is equivalent to
    /// <c>p.AtLeastOnceThen(q).Select(t => t.Item1)</c>.
    /// </remarks>
    /// <param name="terminator">A parser to parse a terminator.</param>
    /// <typeparam name="U">The return type of the <paramref name="terminator"/> parser.</typeparam>
    /// <returns>
    /// A parser which applies this parser repeatedly
    /// until <paramref name="terminator"/> succeeds.
    /// </returns>
    public Parser<TToken, IEnumerable<T>> AtLeastOnceUntil<U>(Parser<TToken, U> terminator)
    {
        if (terminator == null)
        {
            throw new ArgumentNullException(nameof(terminator));
        }

        return AtLeastOnceThen(terminator).Select(new Func<(IEnumerable<T>, U), IEnumerable<T>>(tup => tup.Item1));
    }

    /// <summary>
    /// Creates a parser which applies this parser one or more times
    /// until <paramref name="terminator"/> succeeds.
    /// Fails if this parser fails or if <paramref name="terminator"/>
    /// fails after consuming input.
    /// </summary>
    /// <param name="terminator">A parser to parse a terminator.</param>
    /// <typeparam name="U">The return type of the <paramref name="terminator"/> parser.</typeparam>
    /// <returns>
    /// A parser which applies this parser repeatedly
    /// until <paramref name="terminator"/> succeeds.
    /// </returns>
    public Parser<TToken, (IEnumerable<T>, U)> AtLeastOnceThen<U>(Parser<TToken, U> terminator)
    {
        if (terminator == null)
        {
            throw new ArgumentNullException(nameof(terminator));
        }

        return new AtLeastOnceThenParserFactory1<TToken, T, U>(true)
            .Unbox(this)
            .Unbox(terminator)!;
    }

    /// <summary>
    /// Creates a parser which applies this parser zero or more times
    /// until <paramref name="terminator"/> succeeds,
    /// discarding the results. This is more efficient than
    /// <see cref="Until{U}(Parser{TToken, U})"/> if you don't
    /// need the results.
    /// Fails if this parser fails or if <paramref name="terminator"/>
    /// fails after consuming input.
    /// The return value of <paramref name="terminator"/> is ignored.
    /// </summary>
    /// <remarks>
    /// <c>p.SkipUntil(q)</c> is equivalent to
    /// <c>p.SkipManyThen(q).ThenReturn(Unit.Value)</c>.
    /// </remarks>
    /// <typeparam name="U">The return type of <paramref name="terminator"/>.</typeparam>
    /// <param name="terminator">A parser to parse a terminator.</param>
    /// <returns>A parser which applies this parser repeatedly until <paramref name="terminator"/> succeeds, discarding the results.</returns>
    public Parser<TToken, Unit> SkipUntil<U>(Parser<TToken, U> terminator)
    {
        if (terminator == null)
        {
            throw new ArgumentNullException(nameof(terminator));
        }

        return SkipManyThen(terminator).Then(ReturnUnit);
    }

    /// <summary>
    /// Creates a parser which applies this parser zero or more times
    /// until <paramref name="terminator"/> succeeds,
    /// discarding the results. This is more efficient than
    /// <see cref="ManyThen{U}(Parser{TToken, U})"/> if you don't
    /// need the results.
    /// Fails if this parser fails or if <paramref name="terminator"/>
    /// fails after consuming input.
    /// </summary>
    /// <typeparam name="U">The return type of <paramref name="terminator"/>.</typeparam>
    /// <param name="terminator">A parser to parse a terminator.</param>
    /// <returns>
    /// A parser which applies this parser repeatedly until
    /// <paramref name="terminator"/> succeeds, discarding the results.
    /// </returns>
    public Parser<TToken, U> SkipManyThen<U>(Parser<TToken, U> terminator)
    {
        if (terminator == null)
        {
            throw new ArgumentNullException(nameof(terminator));
        }

        return terminator.Or(SkipAtLeastOnceThen(terminator));
    }

    /// <summary>
    /// Creates a parser which applies this parser one or more times
    /// until <paramref name="terminator"/> succeeds,
    /// discarding the results. This is more efficient than
    /// <see cref="AtLeastOnceUntil{U}(Parser{TToken, U})"/>
    /// if you don't need the results.
    /// Fails if this parser fails or if <paramref name="terminator"/>
    /// fails after consuming input.
    /// The return value of <paramref name="terminator"/> is ignored.
    /// </summary>
    /// <remarks>
    /// <c>p.SkipAtLeastOnceUntil(q)</c> is equivalent to
    /// <c>p.SkipAtLeastOnceThen(q).ThenReturn(Unit.Value)</c>.
    /// </remarks>
    /// <param name="terminator">A parser to parse a terminator.</param>
    /// <typeparam name="U">The return type of the <paramref name="terminator"/> parser.</typeparam>
    /// <returns>
    /// A parser which applies this parser repeatedly until
    /// <paramref name="terminator"/> succeeds, discarding the results.
    /// </returns>
    public Parser<TToken, Unit> SkipAtLeastOnceUntil<U>(Parser<TToken, U> terminator)
    {
        if (terminator == null)
        {
            throw new ArgumentNullException(nameof(terminator));
        }

        return SkipAtLeastOnceThen(terminator).Then(ReturnUnit);
    }

    /// <summary>
    /// Creates a parser which applies this parser one or more times
    /// until <paramref name="terminator"/> succeeds,
    /// discarding the results. This is more efficient than
    /// <see cref="AtLeastOnceThen{U}(Parser{TToken, U})"/>
    /// if you don't need the results. Fails if this parser fails or if
    /// <paramref name="terminator"/> fails after consuming input.
    /// </summary>
    /// <param name="terminator">A parser to parse a terminator.</param>
    /// <typeparam name="U">The return type of the <paramref name="terminator"/> parser.</typeparam>
    /// <returns>
    /// A parser which applies this parser repeatedly until
    /// <paramref name="terminator"/> succeeds, discarding the results.
    /// </returns>
    public Parser<TToken, U> SkipAtLeastOnceThen<U>(Parser<TToken, U> terminator)
    {
        if (terminator == null)
        {
            throw new ArgumentNullException(nameof(terminator));
        }

        return new AtLeastOnceThenParserFactory1<TToken, T, U>(false)
            .Unbox(this)
            .Unbox(terminator)
            .Select(tup => tup.Item2);
    }
}

internal sealed class AtLeastOnceThenParserFactory1<TToken, T, U>(bool keepResults)
    : IUnboxer<TToken, T, IUnboxer<TToken, U, Parser<TToken, (IEnumerable<T>?, U)>>>
{
    public IUnboxer<TToken, U, Parser<TToken, (IEnumerable<T>?, U)>> Unbox<Next1>(BoxParser<TToken, T>.Of<Next1> box)
        where Next1 : IParser<TToken, T>
        => new AtLeastOnceThenParserFactory2<TToken, T, Next1, U>(keepResults, box);
}

internal sealed class AtLeastOnceThenParserFactory2<TToken, T, Next1, U>(
    bool keepResults,
    BoxParser<TToken, T>.Of<Next1> parser
) : IUnboxer<TToken, U, Parser<TToken, (IEnumerable<T>?, U)>>
    where Next1 : IParser<TToken, T>
{
    public Parser<TToken, (IEnumerable<T>?, U)> Unbox<Next2>(BoxParser<TToken, U>.Of<Next2> box)
        where Next2 : IParser<TToken, U>
        => BoxParser<TToken, (IEnumerable<T>?, U)>.Create(
            new AtLeastOnceThenParser<TToken, T, Next1, U, Next2>(
                parser,
                box,
                keepResults
            )
        );
}

internal readonly struct AtLeastOnceThenParser<TToken, T, Next1, U, Next2>
    : IParser<TToken, (IEnumerable<T>?, U)>
    where Next1 : IParser<TToken, T>
    where Next2 : IParser<TToken, U>
{
    private readonly BoxParser<TToken, T>.Of<Next1> _parser;
    private readonly BoxParser<TToken, U>.Of<Next2> _terminator;
    private readonly bool _keepResults;

    public AtLeastOnceThenParser(BoxParser<TToken, T>.Of<Next1> parser, BoxParser<TToken, U>.Of<Next2> terminator, bool keepResults)
    {
        _parser = parser;
        _terminator = terminator;
        _keepResults = keepResults;
    }

    // see comment about expecteds in ParseState.Error.cs
    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out (IEnumerable<T>?, U) result)
    {
        var ts = _keepResults ? new List<T>() : null;

        var firstItemStartLoc = state.Location;

        if (!_parser.TryParse(ref state, ref expecteds, out var result1))
        {
            // state.Error set by _parser
            result = (null, default!);
            return false;
        }

        if (state.Location <= firstItemStartLoc)
        {
            throw new InvalidOperationException("Until() used with a parser which consumed no input");
        }

        ts?.Add(result1);

        var terminatorExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        var itemExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        while (true)
        {
            var terminatorStartLoc = state.Location;
            var terminatorSuccess = _terminator.TryParse(ref state, ref terminatorExpecteds, out var terminatorResult);
            if (terminatorSuccess)
            {
                terminatorExpecteds.Dispose();
                itemExpecteds.Dispose();
                result = (ts!, terminatorResult!);
                return true;
            }

            if (state.Location > terminatorStartLoc)
            {
                // state.Error set by _terminator
                expecteds.AddRange(terminatorExpecteds.AsSpan());
                terminatorExpecteds.Dispose();
                itemExpecteds.Dispose();
                result = (null, default!);
                return false;
            }

            var itemStartLoc = state.Location;
            var itemSuccess = _parser.TryParse(ref state, ref itemExpecteds, out var itemResult);
            var itemConsumedInput = state.Location > itemStartLoc;
            if (!itemSuccess)
            {
                if (!itemConsumedInput)
                {
                    // get the expected from both _terminator and _parser
                    expecteds.AddRange(terminatorExpecteds.AsSpan());
                    expecteds.AddRange(itemExpecteds.AsSpan());
                }
                else
                {
                    // throw out the _terminator expecteds and keep only _parser
                    expecteds.AddRange(itemExpecteds.AsSpan());
                }

                terminatorExpecteds.Dispose();
                itemExpecteds.Dispose();
                result = (null, default!);
                return false;
            }

            // throw out both sets of expecteds
            terminatorExpecteds.Clear();
            itemExpecteds.Clear();
            if (!itemConsumedInput)
            {
                throw new InvalidOperationException("Until() used with a parser which consumed no input");
            }

            ts?.Add(itemResult!);
        }
    }
}
