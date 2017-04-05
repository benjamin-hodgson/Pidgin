using System;
using Pidgin.ParseStates;

namespace Pidgin
{
    public abstract partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser that applies a transformation function to the return value of the current parser.
        /// The transformation function dynamically chooses a second parser, which is applied after applying the current parser.
        /// </summary>
        /// <param name="selector">A transformation function which returns a parser to apply after applying the current parser</param>
        /// <typeparam name="U">The type of the return value of the second parser</typeparam>
        /// <returns>A parser which applies the current parser before applying the result of the <paramref name="selector"/> function.</returns>
        public Parser<TToken, U> Bind<U>(Func<T, Parser<TToken, U>> selector)
            => Bind(selector, (t, u) => u);
        /// <summary>
        /// Creates a parser that applies a transformation function to the return value of the current parser.
        /// The transformation function dynamically chooses a second parser, which is applied after applying the current parser.
        /// </summary>
        /// <param name="selector">A transformation function which returns a parser to apply after applying the current parser</param>
        /// <param name="result">A function to apply to the return values of the two parsers</param>
        /// <typeparam name="U">The type of the return value of the second parser</typeparam>
        /// <typeparam name="R">The type of the return value of the resulting parser</typeparam>
        /// <returns>A parser which applies the current parser before applying the result of the <paramref name="selector"/> function</returns>
        public Parser<TToken, R> Bind<U, R>(Func<T, Parser<TToken, U>> selector, Func<T, U, R> result)
            => new BindParser<U, R>(this, selector, result);

        private sealed class BindParser<U, R> : Parser<TToken, R>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Func<T, Parser<TToken, U>> _func;
            private readonly Func<T, U, R> _result;

            public BindParser(Parser<TToken, T> parser, Func<T, Parser<TToken, U>> func, Func<T, U, R> result) : base(parser.Expected)
            {
                _parser = parser;
                _func = func;
                _result = result;
            }

            internal sealed override Result<TToken, R> Parse(IParseState<TToken> state)
            {
                var result = _parser.Parse(state);
                if (!result.Success)
                {
                    return Result.Failure<TToken, R>(result.Error, result.ConsumedInput);
                }
                var result2 = _func(result.Value).Parse(state);
                if (!result2.Success)
                {
                    return Result.Failure<TToken, R>(result2.Error, result.ConsumedInput || result2.ConsumedInput);
                }
                return Result.Success<TToken, R>(_result(result.Value, result2.Value), result.ConsumedInput || result2.ConsumedInput);
            }
        }
    }
}