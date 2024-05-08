using System;

namespace Pidgin;

#pragma warning disable CA1032 // Implement standard exception constructors

/// <summary>
/// Thrown when a parse error is encountered during parsing.
/// </summary>
public class ParseException : Exception
{
    internal ParseException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Thrown when a parse error is encountered during parsing.
/// </summary>
/// <typeparam name="TToken">
/// The type of the tokens in the parser's input stream.
/// </typeparam>
public class ParseException<TToken> : ParseException
{
    /// <summary>
    /// The <see cref="ParseError{TToken}"/> that caused this exception.
    /// </summary>
    public ParseError<TToken> Error { get; }

    internal ParseException(ParseError<TToken> error)
        : base(error.RenderErrorMessage())
    {
        Error = error;
    }
}

#pragma warning restore CA1032 // Implement standard exception constructors
