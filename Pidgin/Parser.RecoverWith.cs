using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser which runs the current parser, running <paramref name="errorHandler" /> on failure.
    /// </summary>
    /// <param name="errorHandler">A function which returns a parser to apply when the current parser fails.</param>
    /// <returns>A parser which runs the current parser, running <paramref name="errorHandler" /> on failure.</returns>
    public Parser<TToken, T> RecoverWith(Func<ParseError<TToken>, Parser<TToken, T>> errorHandler)
    {
        if (errorHandler == null)
        {
            throw new ArgumentNullException(nameof(errorHandler));
        }

        return new RecoverWithParser<TToken, T>(this, errorHandler);
    }
}

[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1402:FileMayOnlyContainASingleType",
    Justification = "This class belongs next to the accompanying API method"
)]
internal sealed class RecoverWithParser<TToken, T> : Parser<TToken, T>
{
    private readonly Parser<TToken, T> _parser;
    private readonly Func<ParseError<TToken>, Parser<TToken, T>> _errorHandler;

    public RecoverWithParser(Parser<TToken, T> parser, Func<ParseError<TToken>, Parser<TToken, T>> errorHandler)
    {
        _parser = parser;
        _errorHandler = errorHandler;
    }

    // see comment about expecteds in ParseState.Error.cs
    public sealed override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out T result)
    {
        var childExpecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());
        if (_parser.TryParse(ref state, ref childExpecteds, out result))
        {
            childExpecteds.Dispose();
            return true;
        }

        var recoverParser = _errorHandler(state.BuildError(ref childExpecteds));

        childExpecteds.Dispose();

        return recoverParser.TryParse(ref state, ref expecteds, out result);
    }
}
