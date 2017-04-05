using System;
using System.Collections.Generic;

namespace Pidgin
{
    internal static class Result
    {
        internal static Result<TToken, T> Success<TToken, T>(T value, bool consumedInput)
            => new Result<TToken, T>(value, consumedInput);
        internal static Result<TToken, T> Failure<TToken, T>(
            ParseError<TToken> error,
            bool consumedInput
        ) => new Result<TToken, T>(error, consumedInput);
    }

    /// <summary>
    /// Represents the result of parsing.
    /// A parse result may be successful (<see cref="Result{TToken, T}.Success"/> == true), in which case it contains a value, or it may be a failure, in which case it contains an error
    /// </summary>
    /// <typeparam name="TToken"></typeparam>
    /// <typeparam name="T"></typeparam>
    public struct Result<TToken, T>
    {
        /// <summary>
        /// Did the parse succeed?
        /// </summary>
        /// <returns>A value indicating whether the parse was successful</returns>
        public bool Success { get; }
        internal bool ConsumedInput { get; }
        private readonly T _value;
        private readonly ParseError<TToken> _error;

        internal Result(T value, bool consumedInput)
        {
            Success = true;
            ConsumedInput = consumedInput;
            _value = value;
            _error = default(ParseError<TToken>);
        }
        internal Result(ParseError<TToken> error, bool consumedInput)
        {
            Success = false;
            ConsumedInput = consumedInput;
            _value = default(T);
            _error = error;
        }

        /// <summary>
        /// The parser's return value
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when the result was not a successful one</exception>
        /// <returns>The parser's return value</returns>
        public T Value
        {
            get
            {
                if (!Success)
                {
                    throw new InvalidOperationException("Can't get the value of an unsuccessful result");
                }
                return _value;
            }
        }
        /// <summary>
        /// The parse error
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown when the result was a successful one</exception>
        /// <returns>The parse error</returns>
        public ParseError<TToken> Error
        {
            get
            {
                if (Success)
                {
                    throw new InvalidOperationException("Can't get the error of a successful result");
                }
                return _error;
            }
        }

        /// <summary>
        /// Get the value, or return a default value.
        /// </summary>
        /// <returns>The value if <see cref="Success"/> is true, or a default value.</returns>
        public T GetValueOrDefault() => _value;

        /// <summary>
        /// Get the value, or return the specified default value.
        /// </summary>
        /// <returns>The value if <see cref="Success"/> is true, or the specified default value.</returns>
        public T GetValueOrDefault(T @default) => Success ? _value : @default;

        /// <summary>
        /// Get the value, or return the result of calling the specified function.
        /// </summary>
        /// <returns>The value if <see cref="Success"/> is true, or the result of calling the specified function.</returns>
        public T GetValueOrDefault(Func<T> value) => Success ? _value : value();

        internal Result<TToken, T> WithExpected(IEnumerable<Expected<TToken>> expected)
            => Success
                ? this
                : Result.Failure<TToken, T>(
                    this.Error.WithExpected(expected),
                    this.ConsumedInput
                );
        
        /// <summary>
        /// Tear down this parse result using a function for the two possible outcomes.
        /// If <see cref="Success"/> == true, <paramref name="success"/> will be called. Otherwise, <paramref name="failure"/> will be called.
        /// </summary>
        /// <typeparam name="U">The return type</typeparam>
        /// <param name="success">Called when the result has a value</param>
        /// <param name="failure">Called when the result does not have a value</param>
        /// <returns>The result of calling the <paramref name="success"/> or <paramref name="failure"/> function</returns>
        public U Match<U>(Func<T, U> success, Func<ParseError<TToken>, U> failure)
            => Success
                ? success(_value)
                : failure(Error);

        /// <summary>
        /// Project the value contained in the result
        /// </summary>
        /// <param name="selector">A transformation function to apply to the contained value</param>
        /// <typeparam name="U">The type of the resulting value</typeparam>
        /// <returns>The result of applying the transformation function to the contained value</returns>
        public Result<TToken, U> Select<U>(Func<T, U> selector)
            => Success
                ? Result.Success<TToken, U>(selector(_value), ConsumedInput)
                : Result.Failure<TToken, U>(Error, ConsumedInput);

        /// <summary>
        /// Projects the value of the result into a result, and flattens the resulting value into a single result.
        /// </summary>
        /// <param name="selector">A transformation function to apply to the contained value</param>
        /// <typeparam name="U">The type of the resulting possibly-absent value</typeparam>
        /// <returns>The final result</returns>
        public Result<TToken, U> SelectMany<U>(Func<T, Result<TToken, U>> selector)
            => SelectMany(selector, (t, u) => u);
        
        /// <summary>
        /// Projects the value of the result into a result, and flattens the resulting value into a single result, applying a result selector function to the two values.
        /// </summary>
        /// <param name="selector">A transformation function to apply to the contained value</param>
        /// <param name="result">A transformation function to apply to the contained value and the value contained in the selected <see cref="Maybe{U}"/></param>
        /// <typeparam name="U">The type of the value to select</typeparam>
        /// <typeparam name="R">The type of the resulting possibly-absent value</typeparam>
        /// <returns>The result of applying <paramref name="selector"/> to the contained value and <paramref name="result"/> to the intermediate values</returns>
        public Result<TToken, R> SelectMany<U, R>(Func<T, Result<TToken, U>> selector, Func<T, U, R> result)
        {
            if (!Success)
            {
                return Result.Failure<TToken, R>(Error, ConsumedInput);
            }
            var ru = selector(_value);
            var consumedInput = ConsumedInput || ru.ConsumedInput;
            if (!ru.Success)
            {
                return Result.Failure<TToken, R>(ru.Error, consumedInput);
            }
            return Result.Success<TToken, R>(result(_value, ru._value), consumedInput);
        }

        /// <summary>
        /// Choose the first successful result
        /// </summary>
        /// <param name="result">A fallback result if this one has an error</param>
        /// <returns>This result, if <see cref="Success"/> == true, or the result of calling <paramref name="result"/></returns>
        public Result<TToken, T> Or(Func<Result<TToken, T>> result)
            => !Success && !ConsumedInput ? result() : this;

        /// <summary>
        /// Choose the first successful result
        /// </summary>
        /// <param name="result">A fallback result if this one has an error</param>
        /// <returns>This result, if <see cref="Success"/> == true, or <paramref name="result"/></returns>
        public Result<TToken, T> Or(Result<TToken, T> result)
            => !Success && !ConsumedInput ? result : this;

        /// <summary>
        /// Cast the value contained in the result to the specified output type
        /// </summary>
        /// <typeparam name="U">The type to cast the contained value to</typeparam>
        /// <exception cref="System.InvalidCastException">Thrown when the contained value is not an instance of <typeparamref name="U"/></exception>
        /// <returns>A result containing this result's value casted to <typeparamref name="U"/></returns>
        public Result<TToken, U> Cast<U>()
            => Success
                ? Result.Success<TToken, U>((U)(object)_value, ConsumedInput)
                : Result.Failure<TToken, U>(Error, ConsumedInput);
    }
}
