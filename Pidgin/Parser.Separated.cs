using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser which applies the current parser repeatedly, interleaved with a specified parser.
    /// The resulting parser ignores the return value of the separator parser.
    /// </summary>
    /// <typeparam name="U">The return type of the separator parser.</typeparam>
    /// <param name="separator">A parser which parses a separator to be interleaved with the current parser.</param>
    /// <returns>A parser which applies the current parser repeatedly, interleaved by <paramref name="separator"/>.</returns>
    public Parser<TToken, IEnumerable<T>> Separated<U>(Parser<TToken, U> separator)
    {
        if (separator == null)
        {
            throw new ArgumentNullException(nameof(separator));
        }

        return SeparatedAtLeastOnce(separator)
            .Or(ReturnEmptyEnumerable);
    }

    /// <summary>
    /// Creates a parser which applies the current parser at least once, interleaved with a specified parser.
    /// The resulting parser ignores the return value of the separator parser.
    /// </summary>
    /// <typeparam name="U">The return type of the separator parser.</typeparam>
    /// <param name="separator">A parser which parses a separator to be interleaved with the current parser.</param>
    /// <returns>A parser which applies the current parser at least once, interleaved by <paramref name="separator"/>.</returns>
    public Parser<TToken, IEnumerable<T>> SeparatedAtLeastOnce<U>(Parser<TToken, U> separator)
    {
        if (separator == null)
        {
            throw new ArgumentNullException(nameof(separator));
        }

        return SeparatedAtLeastOnceParserFactory1<TToken, T>.Instance
            .Unbox(this)
            .Unbox(separator.Then(this));
    }

    /// <summary>
    /// Creates a parser which applies the current parser repeatedly, interleaved and terminated with a specified parser.
    /// The resulting parser ignores the return value of the separator parser.
    /// </summary>
    /// <typeparam name="U">The return type of the separator parser.</typeparam>
    /// <param name="separator">A parser which parses a separator to be interleaved with the current parser.</param>
    /// <returns>A parser which applies the current parser repeatedly, interleaved and terminated by <paramref name="separator"/>.</returns>
    public Parser<TToken, IEnumerable<T>> SeparatedAndTerminated<U>(Parser<TToken, U> separator)
    {
        if (separator == null)
        {
            throw new ArgumentNullException(nameof(separator));
        }

        return Before(separator).Many();
    }

    /// <summary>
    /// Creates a parser which applies the current parser at least once, interleaved and terminated with a specified parser.
    /// The resulting parser ignores the return value of the separator parser.
    /// </summary>
    /// <typeparam name="U">The return type of the separator parser.</typeparam>
    /// <param name="separator">A parser which parses a separator to be interleaved with the current parser.</param>
    /// <returns>A parser which applies the current parser at least once, interleaved and terminated by <paramref name="separator"/>.</returns>
    public Parser<TToken, IEnumerable<T>> SeparatedAndTerminatedAtLeastOnce<U>(Parser<TToken, U> separator)
    {
        if (separator == null)
        {
            throw new ArgumentNullException(nameof(separator));
        }

        return Before(separator).AtLeastOnce();
    }

    /// <summary>
    /// Creates a parser which applies the current parser repeatedly, interleaved and optionally terminated with a specified parser.
    /// The resulting parser ignores the return value of the separator parser.
    /// </summary>
    /// <typeparam name="U">The return type of the separator parser.</typeparam>
    /// <param name="separator">A parser which parses a separator to be interleaved with the current parser.</param>
    /// <returns>A parser which applies the current parser repeatedly, interleaved and optionally terminated by <paramref name="separator"/>.</returns>
    public Parser<TToken, IEnumerable<T>> SeparatedAndOptionallyTerminated<U>(Parser<TToken, U> separator)
    {
        if (separator == null)
        {
            throw new ArgumentNullException(nameof(separator));
        }

        return SeparatedAndOptionallyTerminatedAtLeastOnce(separator)
            .Or(ReturnEmptyEnumerable);
    }

    /// <summary>
    /// Creates a parser which applies the current parser at least once, interleaved and optionally terminated with a specified parser.
    /// The resulting parser ignores the return value of the separator parser.
    /// </summary>
    /// <typeparam name="U">The return type of the separator parser.</typeparam>
    /// <param name="separator">A parser which parses a separator to be interleaved with the current parser.</param>
    /// <returns>A parser which applies the current parser at least once, interleaved and optionally terminated by <paramref name="separator"/>.</returns>
    public Parser<TToken, IEnumerable<T>> SeparatedAndOptionallyTerminatedAtLeastOnce<U>(Parser<TToken, U> separator)
    {
        if (separator == null)
        {
            throw new ArgumentNullException(nameof(separator));
        }

        return SeparatedAndOptionallyTerminatedAtLeastOnceParserFactory1<TToken, T, U>.Instance
            .Unbox(this)
            .Unbox(separator);
    }
}

internal sealed class SeparatedAtLeastOnceParserFactory1<TToken, T> : IUnboxer<TToken, T, IUnboxer<TToken, T, Parser<TToken, IEnumerable<T>>>>
{
    public IUnboxer<TToken, T, Parser<TToken, IEnumerable<T>>> Unbox<Next1>(BoxParser<TToken, T>.Of<Next1> box)
        where Next1 : IParser<TToken, T>
        => new SeparatedAtLeastOnceParserFactory2<TToken, T, Next1>(box);

    public static IUnboxer<TToken, T, IUnboxer<TToken, T, Parser<TToken, IEnumerable<T>>>> Instance { get; }
        = new SeparatedAtLeastOnceParserFactory1<TToken, T>();
}

internal sealed class SeparatedAtLeastOnceParserFactory2<TToken, T, Next1>(
    BoxParser<TToken, T>.Of<Next1> parser
) : IUnboxer<TToken, T, Parser<TToken, IEnumerable<T>>>
    where Next1 : IParser<TToken, T>
{
    public Parser<TToken, IEnumerable<T>> Unbox<Next2>(BoxParser<TToken, T>.Of<Next2> box)
        where Next2 : IParser<TToken, T>
        => BoxParser<TToken, IEnumerable<T>>.Create(new SeparatedAtLeastOnceParser<TToken, T, Next1, Next2>(parser, box));
}

internal readonly struct SeparatedAtLeastOnceParser<TToken, T, Next1, Next2> : IParser<TToken, IEnumerable<T>>
    where Next1 : IParser<TToken, T>
    where Next2 : IParser<TToken, T>
{
    private readonly BoxParser<TToken, T>.Of<Next1> _parser;

    // _remainerParser should be separator.Then(parser)
    private readonly BoxParser<TToken, T>.Of<Next2> _remainderParser;

    public SeparatedAtLeastOnceParser(BoxParser<TToken, T>.Of<Next1> parser, BoxParser<TToken, T>.Of<Next2> remainder)
    {
        _parser = parser;
        _remainderParser = remainder;
    }

    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out IEnumerable<T> result)
    {
        if (!_parser.Value.TryParse(ref state, ref expecteds, out var result1))
        {
            // state.Error set by _parser
            result = null;
            return false;
        }

        var list = new List<T> { result1 };
        if (!Rest(ref state, ref expecteds, list))
        {
            result = null;
            return false;
        }

        result = list;
        return true;
    }

    private bool Rest(
        ref ParseState<TToken> state,
        ref PooledList<Expected<TToken>> expecteds,
        List<T> ts)
    {
        var lastStartingLoc = state.Location;
        var childExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        while (_remainderParser.Value.TryParse(ref state, ref childExpecteds, out var result))
        {
            var endingLoc = state.Location;
            childExpecteds.Clear();

            if (endingLoc <= lastStartingLoc)
            {
                childExpecteds.Dispose();
                throw new InvalidOperationException("Many() used with a parser which consumed no input");
            }

            ts.Add(result);

            lastStartingLoc = endingLoc;
        }

        var lastParserConsumedInput = state.Location > lastStartingLoc;
        if (lastParserConsumedInput)
        {
            expecteds.AddRange(childExpecteds.AsSpan());
        }

        childExpecteds.Dispose();

        // we fail if the most recent parser failed after consuming input.
        // it sets state.Error for us
        return !lastParserConsumedInput;
    }
}

internal sealed class SeparatedAndOptionallyTerminatedAtLeastOnceParserFactory1<TToken, T, U>
    : IUnboxer<TToken, T, IUnboxer<TToken, U, Parser<TToken, IEnumerable<T>>>>
{
    public IUnboxer<TToken, U, Parser<TToken, IEnumerable<T>>> Unbox<Next1>(BoxParser<TToken, T>.Of<Next1> box)
        where Next1 : IParser<TToken, T>
        => new SeparatedAndOptionallyTerminatedAtLeastOnceParserFactory2<TToken, T, Next1, U>(box);

    public static IUnboxer<TToken, T, IUnboxer<TToken, U, Parser<TToken, IEnumerable<T>>>> Instance { get; }
        = new SeparatedAndOptionallyTerminatedAtLeastOnceParserFactory1<TToken, T, U>();
}

internal sealed class SeparatedAndOptionallyTerminatedAtLeastOnceParserFactory2<TToken, T, Next1, U>(
    BoxParser<TToken, T>.Of<Next1> parser
) : IUnboxer<TToken, U, Parser<TToken, IEnumerable<T>>>
    where Next1 : IParser<TToken, T>
{
    public Parser<TToken, IEnumerable<T>> Unbox<Next2>(BoxParser<TToken, U>.Of<Next2> box)
        where Next2 : IParser<TToken, U>
        => BoxParser<TToken, IEnumerable<T>>.Create(new SeparatedAndOptionallyTerminatedAtLeastOnceParser<TToken, T, U, Next1, Next2>(parser, box));
}

internal readonly struct SeparatedAndOptionallyTerminatedAtLeastOnceParser<TToken, T, U, Next1, Next2> : IParser<TToken, IEnumerable<T>>
    where Next1 : IParser<TToken, T>
    where Next2 : IParser<TToken, U>
{
    private readonly BoxParser<TToken, T>.Of<Next1> _parser;
    private readonly BoxParser<TToken, U>.Of<Next2> _separator;

    public SeparatedAndOptionallyTerminatedAtLeastOnceParser(BoxParser<TToken, T>.Of<Next1> parser, BoxParser<TToken, U>.Of<Next2> separator)
    {
        _parser = parser;
        _separator = separator;
    }

    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out IEnumerable<T> result)
    {
        if (!_parser.Value.TryParse(ref state, ref expecteds, out var result1))
        {
            // state.Error set by _parser
            result = null;
            return false;
        }

        var ts = new List<T> { result1 };

        var childExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        while (true)
        {
            var sepStartLoc = state.Location;
            var sepSuccess = _separator.Value.TryParse(ref state, ref childExpecteds, out var _);
            var sepConsumedInput = state.Location > sepStartLoc;

            if (!sepSuccess && sepConsumedInput)
            {
                expecteds.AddRange(childExpecteds.AsSpan());
            }

            childExpecteds.Clear();

            if (!sepSuccess)
            {
                childExpecteds.Dispose();
                if (sepConsumedInput)
                {
                    // state.Error set by _separator
                    result = null;
                    return false;
                }

                result = ts;
                return true;
            }

            var itemStartLoc = state.Location;
            var itemSuccess = _parser.Value.TryParse(ref state, ref childExpecteds, out var itemResult);
            var itemConsumedInput = state.Location > itemStartLoc;

            if (!itemSuccess && itemConsumedInput)
            {
                expecteds.AddRange(childExpecteds.AsSpan());
            }

            childExpecteds.Clear();

            if (!itemSuccess)
            {
                childExpecteds.Dispose();
                if (itemConsumedInput)
                {
                    // state.Error set by _parser
                    result = null;
                    return false;
                }

                result = ts;
                return true;
            }

            ts.Add(itemResult!);
        }
    }
}
