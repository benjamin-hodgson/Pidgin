using System.Collections.Generic;
using Pidgin.ParseStates;

namespace Pidgin
{
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
    /// <typeparam name="TToken">The type of the tokens in the input stream for parsers created by methods in this class</typeparam>
    public static partial class Parser<TToken>
    {
    }
    /// <summary>
    /// Represents a parser which consumes a stream of values of type <typeparamref name="TToken"/> and returns a value of type <typeparamref name="T"/>.
    /// A parser can either succeed, and return a value of type <typeparamref name="T"/>, or fail and return a <see cref="ParseError{TToken}"/>.
    /// </summary>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream</typeparam>
    /// <typeparam name="TToken">The type of the value returned by the parser</typeparam>
    /// <remarks>This type is not intended to be subclassed by users of the library</remarks>
    public abstract partial class Parser<TToken, T>
    {
        internal SortedSet<Expected<TToken>> Expected { get; }
        internal Parser(SortedSet<Expected<TToken>> expected)
        {
            Expected = expected;
        }

        internal abstract Result<TToken, T> Parse(IParseState<TToken> state);
    }
}
