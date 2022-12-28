using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser that fails if the value returned by the current parser fails to satisfy a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to apply to the value returned by the current parser.</param>
    /// <returns>A parser that fails if the value returned by the current parser fails to satisfy <paramref name="predicate"/>.</returns>
    public Parser<TToken, T> Assert(Func<T, bool> predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return Assert(predicate, "Assertion failed");
    }

    /// <summary>
    /// Creates a parser that fails if the value returned by the current parser fails to satisfy a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to apply to the value returned by the current parser.</param>
    /// <param name="message">A custom error message to return when the value returned by the current parser fails to satisfy the predicate.</param>
    /// <returns>A parser that fails if the value returned by the current parser fails to satisfy <paramref name="predicate"/>.</returns>
    public Parser<TToken, T> Assert(Func<T, bool> predicate, string message)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return Assert(predicate, _ => message);
    }

    /// <summary>
    /// Creates a parser that fails if the value returned by the current parser fails to satisfy a predicate.
    /// </summary>
    /// <param name="predicate">The predicate to apply to the value returned by the current parser.</param>
    /// <param name="message">A function to produce a custom error message to return when the value returned by the current parser fails to satisfy the predicate.</param>
    /// <returns>A parser that fails if the value returned by the current parser fails to satisfy <paramref name="predicate"/>.</returns>
    public Parser<TToken, T> Assert(Func<T, bool> predicate, Func<T, string> message)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return new AssertParser<TToken, T>(this, predicate, message);
    }
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal sealed class AssertParser<TToken, T> : Parser<TToken, T>
{
    private static readonly Expected<TToken> _expected
        = new("result satisfying assertion");

    private readonly Parser<TToken, T> _parser;
    private readonly Func<T, bool> _predicate;
    private readonly Func<T, string> _message;

    public AssertParser(Parser<TToken, T> parser, Func<T, bool> predicate, Func<T, string> message)
    {
        _parser = parser;
        _predicate = predicate;
        _message = message;
    }

    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out T result)
    {
        var childExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());

        var success = _parser.TryParse(ref state, ref childExpecteds, out result);

        if (success)
        {
            expecteds.AddRange(childExpecteds.AsSpan());
        }

        childExpecteds.Dispose();

        if (!success)
        {
            return false;
        }

        // result is not null hereafter
        if (!_predicate(result!))
        {
            state.SetError(Maybe.Nothing<TToken>(), false, state.Location, _message(result!));
            expecteds.Add(_expected);

            result = default;
            return false;
        }

#pragma warning disable CS8762  // Parameter 'result' must have a non-null value when exiting with 'true'.
        return true;
#pragma warning restore CS8762
    }
}
