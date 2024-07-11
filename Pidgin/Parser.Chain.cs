using System;
using System.Diagnostics.CodeAnalysis;

using Pidgin.Configuration;

namespace Pidgin;

public partial class Parser<TToken, T>
{
    // todo: maybe just write some loops
    internal Parser<TToken, U> ChainAtLeastOnce<U, TChainer>(Func<IConfiguration<TToken>, TChainer> factory)
        where TChainer : struct, IChainer<T, U>
        => Accept(new ChainAtLeastOnceLParserFactory<TToken, T, U, TChainer>(factory));
}

internal class ChainAtLeastOnceLParserFactory<TToken, T, U, TChainer>(Func<IConfiguration<TToken>, TChainer> factory)
    : IUnboxer<TToken, T, BoxParser<TToken, U>>
    where TChainer : struct, IChainer<T, U>
{
    public BoxParser<TToken, U> Unbox<Next>(BoxParser<TToken, T>.Of<Next> box)
        where Next : IParser<TToken, T>
        => BoxParser<TToken, U>.Create(new ChainAtLeastOnceLParser<Next, TToken, T, U, TChainer>(box, factory));
}

internal interface IChainer<in T, out U>
{
    void Apply(T value);

    U GetResult();

    void OnError();
}

internal readonly struct ChainAtLeastOnceLParser<Next, TToken, T, U, TChainer> : IParser<TToken, U>
    where Next : IParser<TToken, T>
    where TChainer : struct, IChainer<T, U>
{
    private readonly BoxParser<TToken, T>.Of<Next> _parser;
    private readonly Func<IConfiguration<TToken>, TChainer> _factory;

    public ChainAtLeastOnceLParser(BoxParser<TToken, T>.Of<Next> parser, Func<IConfiguration<TToken>, TChainer> factory)
    {
        _parser = parser;
        _factory = factory;
    }

    public bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out U result)
    {
        if (!_parser.Value.TryParse(ref state, ref expecteds, out var result1))
        {
            // state.Error set by _parser
            result = default;
            return false;
        }

        var chainer = _factory(state.Configuration);
        chainer.Apply(result1);

        var lastStartLoc = state.Location;
        var childExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        while (_parser.Value.TryParse(ref state, ref childExpecteds, out var childResult))
        {
            var endLoc = state.Location;
            childExpecteds.Clear();
            if (endLoc <= lastStartLoc)
            {
                childExpecteds.Dispose();
                chainer.OnError();
                throw new InvalidOperationException("Many() used with a parser which consumed no input");
            }

            chainer.Apply(childResult);

            lastStartLoc = endLoc;
        }

        var lastParserConsumedInput = state.Location > lastStartLoc;
        if (lastParserConsumedInput)
        {
            expecteds.AddRange(childExpecteds.AsSpan());
        }

        childExpecteds.Dispose();

        if (lastParserConsumedInput)
        {
            // the most recent parser failed after consuming input.
            // state.Error was set by _parser
            chainer.OnError();
            result = default;
            return false;
        }

        result = chainer.GetResult();
        return true;
    }
}
