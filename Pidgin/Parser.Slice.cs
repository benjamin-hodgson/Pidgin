using System;

namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Returns a parser which runs the current parser and applies a selector function.
    /// The selector function receives a <see cref="ReadOnlySpan{TToken}"/> as its first argument, and the result of the current parser as its second argument.
    /// The <see cref="ReadOnlySpan{TToken}"/> represents the sequence of input tokens which were consumed by the parser.
    ///
    /// This allows you to write "pattern"-style parsers which match a sequence of tokens and return a view of the part of the input stream which they matched.
    ///
    /// This function is an alternative name for <see cref="MapWithInput"/>.
    /// </summary>
    /// <param name="selector">
    /// A selector function which computes a result of type <typeparamref name="U"/>.
    /// The arguments of the selector function are a <see cref="ReadOnlySpan{TToken}"/> containing the sequence of input tokens which were consumed by this parser,
    /// and the result of this parser.
    /// </param>
    /// <typeparam name="U">The result type.</typeparam>
    /// <returns>A parser which runs the current parser and applies a selector function.</returns>
    public Parser<TToken, U> Slice<U>(ReadOnlySpanFunc<TToken, T, U> selector)
        => MapWithInput(selector);
}
