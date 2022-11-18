using System;

namespace Pidgin;

/// <summary>
/// Represents the result of parsing.
/// A parse result may be successful (<see cref="Result{TToken, T}.Success"/> == true), in which case it contains a value, or it may be a failure, in which case it contains an error.
/// </summary>
/// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
/// <typeparam name="T">The type of the value returned by the parser.</typeparam>
public class Result<TToken, T>
{
    /// <summary>
    /// Did the parse succeed?.
    /// </summary>
    /// <returns>A value indicating whether the parse was successful.</returns>
    public bool Success { get; }

    private readonly T _value;

    // for testing
    internal ParseError<TToken>? ErrorOrDefault { get; }

    internal Result(T value)
    {
        Success = true;
        _value = value;
        ErrorOrDefault = default;
    }

    internal Result(ParseError<TToken> error)
    {
        Success = false;
        _value = default!;
        ErrorOrDefault = error;
    }

    /// <summary>
    /// The parser's return value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result was not a successful one.</exception>
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
    /// The parse error.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result was a successful one.</exception>
    public ParseError<TToken>? Error
    {
        get
        {
            if (Success)
            {
                throw new InvalidOperationException("Can't get the error of a successful result");
            }

            return ErrorOrDefault;
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
    /// <param name="default">The default value.</param>
    /// <returns>The value if <see cref="Success"/> is true, or the specified default value.</returns>
    public T GetValueOrDefault(T @default) => Success ? _value : @default;

    /// <summary>
    /// Get the value, or return the result of calling the specified function.
    /// </summary>
    /// <param name="value">A function which computes a default value.</param>
    /// <returns>The value if <see cref="Success"/> is true, or the result of calling the specified function.</returns>
    public T GetValueOrDefault(Func<T> value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return Success ? _value : value();
    }

    /// <summary>
    /// Tear down this parse result using a function for the two possible outcomes.
    /// If <see cref="Success"/> == true, <paramref name="success"/> will be called. Otherwise, <paramref name="failure"/> will be called.
    /// </summary>
    /// <typeparam name="U">The return type.</typeparam>
    /// <param name="success">Called when the result has a value.</param>
    /// <param name="failure">Called when the result does not have a value.</param>
    /// <returns>The result of calling the <paramref name="success"/> or <paramref name="failure"/> function.</returns>
    public U Match<U>(Func<T, U> success, Func<ParseError<TToken>, U> failure)
    {
        if (success == null)
        {
            throw new ArgumentNullException(nameof(success));
        }

        if (failure == null)
        {
            throw new ArgumentNullException(nameof(failure));
        }

        return Success
            ? success(_value)
            : failure(Error!);
    }

    /// <summary>
    /// Project the value contained in the result.
    /// </summary>
    /// <param name="selector">A transformation function to apply to the contained value.</param>
    /// <typeparam name="U">The type of the resulting value.</typeparam>
    /// <returns>The result of applying the transformation function to the contained value.</returns>
    public Result<TToken, U> Select<U>(Func<T, U> selector)
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        return Success
            ? new Result<TToken, U>(selector(_value))
            : new Result<TToken, U>(Error!);
    }

    /// <summary>
    /// Projects the value of the result into a result, and flattens the resulting value into a single result.
    /// </summary>
    /// <param name="selector">A transformation function to apply to the contained value.</param>
    /// <typeparam name="U">The type of the resulting possibly-absent value.</typeparam>
    /// <returns>The final result.</returns>
    public Result<TToken, U> SelectMany<U>(Func<T, Result<TToken, U>> selector)
        => SelectMany(selector, (t, u) => u);

    /// <summary>
    /// Projects the value of the result into a result, and flattens the resulting value into a single result, applying a result selector function to the two values.
    /// </summary>
    /// <param name="selector">A transformation function to apply to the contained value.</param>
    /// <param name="result">A transformation function to apply to the contained value and the value contained in the selected <see cref="Maybe{U}"/>.</param>
    /// <typeparam name="U">The type of the value to select.</typeparam>
    /// <typeparam name="R">The type of the resulting possibly-absent value.</typeparam>
    /// <returns>The result of applying <paramref name="selector"/> to the contained value and <paramref name="result"/> to the intermediate values.</returns>
    public Result<TToken, R> SelectMany<U, R>(Func<T, Result<TToken, U>> selector, Func<T, U, R> result)
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (!Success)
        {
            return new Result<TToken, R>(Error!);
        }

        var ru = selector(_value);
        if (!ru.Success)
        {
            return new Result<TToken, R>(ru.Error!);
        }

        return new Result<TToken, R>(result(_value, ru._value));
    }

    /// <summary>
    /// Choose the first successful result.
    /// </summary>
    /// <param name="result">A fallback result if this one has an error.</param>
    /// <returns>This result, if <see cref="Success"/> == true, or the result of calling <paramref name="result"/>.</returns>
    public Result<TToken, T> Or(Func<Result<TToken, T>> result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return !Success ? result() : this;
    }

    /// <summary>
    /// Choose the first successful result.
    /// </summary>
    /// <param name="result">A fallback result if this one has an error.</param>
    /// <returns>This result, if <see cref="Success"/> == true, or <paramref name="result"/>.</returns>
    public Result<TToken, T> Or(Result<TToken, T> result)
        => !Success ? result : this;

    /// <summary>
    /// Cast the value contained in the result to the specified output type.
    /// </summary>
    /// <typeparam name="U">The type to cast the contained value to.</typeparam>
    /// <exception cref="InvalidCastException">Thrown when the contained value is not an instance of <typeparamref name="U"/>.</exception>
    /// <returns>A result containing this result's value casted to <typeparamref name="U"/>.</returns>
    public Result<TToken, U> Cast<U>()
        => Success
            ? new Result<TToken, U>((U)(object)_value!)
            : new Result<TToken, U>(Error!);
}
