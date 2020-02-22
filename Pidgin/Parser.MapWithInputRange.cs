using System;
using System.ComponentModel;

namespace Pidgin
{
    #if NETSTANDARD2_0 || NETFRAMEWORK451
    public struct Range : IEquatable<Range>
    {
        public int Start { get; }
        public int End { get; }

        public Range(int start, int end) {
            Start = start;
            End = end;
        }

        public bool Equals(Range other) =>
            Start == other.Start && End == other.End;
    }
    #else
    using Range = System.Range;
    #endif

    public readonly ref struct InputRange<T>
    {
        public readonly ReadOnlySpan<T> Span;
        public readonly Range Range;

        public InputRange(ReadOnlySpan<T> span, Range range)
        {
            Span = span;
            Range = range;
        }

        public override string ToString() => $"[{Range}] {Span.ToString()}";
    }

    /// <summary>
    /// A function which computes a result from a <see cref="InputRange{TToken}"/> and an additional argument.
    /// </summary>
    /// <param name="span">The input span</param>
    /// <param name="param">An additional argument</param>
    /// <typeparam name="T">The type of elements of the span</typeparam>
    /// <typeparam name="TParam">The type of the additional argument</typeparam>
    /// <typeparam name="TReturn">The type of the result computed by the function</typeparam>
    /// <returns>The result</returns>
    public delegate TReturn InputRangeFunc<T, in TParam, out TReturn>(InputRange<T> span, TParam param);

    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Returns a parser which runs the current parser and applies a selector function.
        /// The selector function receives a <see cref="InputRange{TToken}"/> as its first argument, and the result of the current parser as its second argument.
        /// The <see cref="InputRange{TToken}"/> represents the sequence of input tokens which were consumed by the parser.
        ///
        /// This allows you to write "pattern"-style parsers which match a sequence of tokens and return a view of the part of the input stream which they matched.
        /// </summary>
        /// <param name="selector">
        /// A selector function which computes a result of type <typeparamref name="U"/>.
        /// The arguments of the selector function are a <see cref="InputRange{TToken}"/> containing the sequence of input tokens which were consumed by this parser,
        /// and the result of this parser.
        /// </param>
        /// <typeparam name="U">The result type</typeparam>
        /// <returns>A parser which runs the current parser and applies a selector function.</returns>
        public Parser<TToken, U> MapWithInputRange<U>(InputRangeFunc<TToken, T, U> selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }
            return new MapWithInputRangeParser<TToken, T, U>(this, selector);
        }
    }



    internal class MapWithInputRangeParser<TToken, T, U> : Parser<TToken, U>
    {
        private Parser<TToken, T> _parser;
        private InputRangeFunc<TToken, T, U> _selector;

        public MapWithInputRangeParser(Parser<TToken, T> parser, InputRangeFunc<TToken, T, U> selector)
        {
            _parser = parser;
            _selector = selector;
        }

        public override InternalResult<U> Parse(ref ParseState<TToken> state)
        {
            var start = state.Location;

            state.PushBookmark();    // don't discard input buffer
            var result = _parser.Parse(ref state);


            if (!result.Success)
            {
                state.PopBookmark();
                return InternalResult.Failure<U>(result.ConsumedInput);
            }


            var delta = state.Location - start;
            var inputRange = new InputRange<TToken>(state.LookBehind(delta), new Range(start, state.Location));
            var val = _selector(inputRange, result.Value);

            state.PopBookmark();

            return InternalResult.Success<U>(val, result.ConsumedInput);
        }
    }

}
