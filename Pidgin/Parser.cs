using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

/// <summary>
/// Constructor functions, extension methods and utilities for <see cref="Parser{TToken, T}"/>.
/// This class is intended to be imported statically ("using static Pidgin.Parser").
/// </summary>
public static partial class Parser
{
}

/// <summary>
/// Constructor functions, extension methods and utilities for <see cref="Parser{TToken, T}"/>
/// This class is intended to be imported statically, with the type parameter set to the type of tokens in your input stream ("using static Pidgin.Parser&lt;char&gt;").
/// </summary>
/// <typeparam name="TToken">The type of the tokens in the input stream for parsers created by methods in this class.</typeparam>
[SuppressMessage(
    "design",
    "CA1000:Do not declare static members on generic types",
    Justification = "This type is designed to be imported statically"
)]
public static partial class Parser<TToken>
{
}

/// <summary>
/// Represents a parser which consumes a stream of values of type <typeparamref name="TToken"/> and returns a value of type <typeparamref name="T"/>.
/// A parser can either succeed, and return a value of type <typeparamref name="T"/>, or fail and return a <see cref="ParseError{TToken}"/>.
/// </summary>
/// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
/// <typeparam name="T">The type of the value returned by the parser.</typeparam>
/// <remarks>This type is not intended to be subclassed by users of the library.</remarks>
public abstract partial class Parser<TToken, T>
{
    // invariant: state.Error is populated with the error that caused the failure
    // if the result was not successful

    // Why pass the error by reference?
    // I previously passed Result around directly, which has an Error property,
    // but copying it around turned out to be too expensive because ParseError is a large struct

    /// <summary>
    /// Override this method to implement a custom parser.
    /// Use this if you can't do what you need using the base parser combinators.
    ///
    /// If your parser fails it should return false and call <see cref="ParseState{TToken}.SetError(Maybe{TToken}, bool, int, string?)"/>.
    ///
    /// WARNING: This API is <strong>unstable</strong>
    /// and subject to change in future versions of the library.
    /// </summary>
    /// <param name="state">The parser's state.</param>
    /// <param name="expecteds">A list to which the parser can add its expected tokens when it fails.</param>
    /// <param name="result">The result.</param>
    /// <returns>True if the parser succeeded, false if it failed.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public abstract bool TryParse(
        ref ParseState<TToken> state,
        ref PooledList<Expected<TToken>> expecteds,
        [MaybeNullWhen(false)] out T result
    );
}
