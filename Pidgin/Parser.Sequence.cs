using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pidgin;

public static partial class Parser<TToken>
{
    /// <summary>
    /// Creates a parser that parses and returns a literal sequence of tokens.
    /// </summary>
    /// <param name="tokens">A sequence of tokens.</param>
    /// <returns>A parser that parses a literal sequence of tokens.</returns>
    public static Parser<TToken, TToken[]> Sequence(params TToken[] tokens)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        return Sequence<TToken[]>(tokens);
    }

    /// <summary>
    /// Creates a parser that parses and returns a literal sequence of tokens.
    /// The input enumerable is enumerated and copied to a list.
    /// </summary>
    /// <typeparam name="TEnumerable">The type of tokens to parse.</typeparam>
    /// <param name="tokens">A sequence of tokens.</param>
    /// <returns>A parser that parses a literal sequence of tokens.</returns>
    public static Parser<TToken, TEnumerable> Sequence<TEnumerable>(TEnumerable tokens)
        where TEnumerable : IEnumerable<TToken>
    {
        if (tokens == null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        return new SequenceTokenParser<TToken, TEnumerable>(tokens);
    }

    /// <summary>
    /// Creates a parser that applies a sequence of parsers and collects the results.
    /// This parser fails if any of its constituent parsers fail.
    /// </summary>
    /// <typeparam name="T">The return type of the parsers.</typeparam>
    /// <param name="parsers">A sequence of parsers.</param>
    /// <returns>A parser that applies a sequence of parsers and collects the results.</returns>
    public static Parser<TToken, IEnumerable<T>> Sequence<T>(params Parser<TToken, T>[] parsers)
    {
        return Sequence(parsers.AsEnumerable());
    }

    /// <summary>
    /// Creates a parser that applies a sequence of parsers and collects the results.
    /// This parser fails if any of its constituent parsers fail.
    /// </summary>
    /// <typeparam name="T">The return type of the parsers.</typeparam>
    /// <param name="parsers">A sequence of parsers.</param>
    /// <returns>A parser that applies a sequence of parsers and collects the results.</returns>
    public static Parser<TToken, IEnumerable<T>> Sequence<T>(IEnumerable<Parser<TToken, T>> parsers)
    {
        if (parsers == null)
        {
            throw new ArgumentNullException(nameof(parsers));
        }

        var parsersArray = parsers.ToArray();
        if (parsersArray.Length == 1)
        {
            return parsersArray[0].Select(x => new[] { x }.AsEnumerable());
        }

        return new SequenceParser<TToken, T>(parsersArray);
    }
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal sealed class SequenceParser<TToken, T> : Parser<TToken, IEnumerable<T>>
{
    private readonly Parser<TToken, T>[] _parsers;

    public SequenceParser(Parser<TToken, T>[] parsers)
    {
        _parsers = parsers;
    }

    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out IEnumerable<T> result)
    {
        var ts = new T[_parsers.Length];

        for (var i = 0; i < _parsers.Length; i++)
        {
            var p = _parsers[i];

            var success = p.TryParse(ref state, ref expecteds, out ts[i]!);

            if (!success)
            {
                result = null;
                return false;
            }
        }

        result = ts;
        return true;
    }
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal sealed class SequenceTokenParser<TToken, TEnumerable> : Parser<TToken, TEnumerable>
    where TEnumerable : IEnumerable<TToken>
{
    private readonly TEnumerable _value;
    private readonly ImmutableArray<TToken> _valueTokens;

    public SequenceTokenParser(TEnumerable value)
    {
        _value = value;
        _valueTokens = value.ToImmutableArray();
    }

    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out TEnumerable result)
    {
        var span = state.LookAhead(_valueTokens.Length);  // span.Length <= _valueTokens.Length

        var errorPos = -1;
        for (var i = 0; i < span.Length; i++)
        {
            if (!EqualityComparer<TToken>.Default.Equals(span[i], _valueTokens[i]))
            {
                errorPos = i;
                break;
            }
        }

        if (errorPos != -1)
        {
            // strings didn't match
            state.Advance(errorPos);
            state.SetError(Maybe.Just(span[errorPos]), false, state.Location, null);
            expecteds.Add(new Expected<TToken>(_valueTokens));
            result = default;
            return false;
        }

        if (span.Length < _valueTokens.Length)
        {
            // strings matched but reached EOF
            state.Advance(span.Length);
            state.SetError(Maybe.Nothing<TToken>(), true, state.Location, null);
            expecteds.Add(new Expected<TToken>(_valueTokens));
            result = default;
            return false;
        }

        // OK
        state.Advance(_valueTokens.Length);
        result = _value;
        return true;
    }
}
