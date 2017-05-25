using System;

namespace Pidgin
{
    /// <summary>
    /// Constructor functions, extension methods and utilities for working with <see cref="Maybe{T}"/>;
    /// </summary>
    public static class Maybe
    {
        /// <summary>
        /// Creates a <see cref="Maybe{T}"/> containing a value
        /// </summary>
        /// <param name="value">The value of the new <see cref="Maybe{T}"/></param>
        /// <typeparam name="T">The type of the contained value</typeparam>
        /// <returns>A <see cref="Maybe{T}"/> containing the specified value</returns>
        public static Maybe<T> Just<T>(T value) => new Maybe<T>(value);

        /// <summary>
        /// Creates a <see cref="Maybe{T}"/> containing no value
        /// </summary>
        /// <typeparam name="T">The type of the absent value</typeparam>
        /// <returns>A <see cref="Maybe{T}"/> containing no</returns>
        public static Maybe<T> Nothing<T>() => new Maybe<T>();
    }

    /// <summary>
    /// Represents a single possibly absent value. Like <see cref="System.Nullable{T}"/> but works for reference types as well as value types.
    /// </summary>
    /// <typeparam name="T">The type of the contained value</typeparam>
    public struct Maybe<T> : IEquatable<Maybe<T>>
    {
        /// <summary>
        /// Does the <see cref="Maybe{T}"/> contains a value?
        /// </summary>
        /// <returns>True if and only if the <see cref="Maybe{T}"/> contains a value.</returns>
        public bool HasValue { get; }
        private readonly T _value;

        /// <summary>
        /// Create a <see cref="Maybe{T}"/> containing a value
        /// </summary>
        /// <param name="value">The value</param>
        public Maybe(T value)
        {
            _value = value;
            HasValue = true;
        }

        /// <summary>
        /// Get the value from the <see cref="Maybe{T}"/>.
        /// </summary>
        /// <returns>The value</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the <see cref="Maybe{T}"/> does not contain a value</exception>
        public T Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new InvalidOperationException("Maybe has no value");
                }
                return _value;
            }
        }
        /// <summary>
        /// Get the value from the <see cref="Maybe{T}"/>, or return a default value.
        /// </summary>
        /// <returns>The value if <see cref="HasValue"/> is true, or a default value.</returns>
        public T GetValueOrDefault() => _value;
        
        /// <summary>
        /// Get the value from the <see cref="Maybe{T}"/>, or return the specified default value.
        /// </summary>
        /// <returns>The value if <see cref="HasValue"/> is true, or the specified default value.</returns>
        public T GetValueOrDefault(T value) => HasValue ? _value : value;

        /// <summary>
        /// Get the value from the <see cref="Maybe{T}"/>, or return the result of calling the specified function.
        /// </summary>
        /// <returns>The value if <see cref="HasValue"/> is true, or the result of calling the specified function.</returns>
        public T GetValueOrDefault(Func<T> value) => HasValue ? _value : value();
        
        /// <summary>
        /// Tear down this <see cref="Maybe{T}"/> using a function for the two possible outcomes.
        /// If <see cref="HasValue"/> == true, <paramref name="just"/> will be called. Otherwise, <paramref name="nothing"/> will be called.
        /// </summary>
        /// <typeparam name="U">The return type</typeparam>
        /// <param name="just">Called when the result has a value</param>
        /// <param name="nothing">Called when the result does not have a value</param>
        /// <returns>The result of calling the <paramref name="just"/> or <paramref name="nothing"/> function</returns>
        public U Match<U>(Func<T, U> just, Func<U> nothing)
            => HasValue ? just(_value) : nothing();

        /// <summary>
        /// Project the value contained in the <see cref="Maybe{T}"/>
        /// </summary>
        /// <param name="selector">A transformation function to apply to the contained value</param>
        /// <typeparam name="U">The type of the resulting value</typeparam>
        /// <returns>The result of applying the transformation function to the contained value, or <see cref="Maybe.Nothing{U}()"/></returns>
        public Maybe<U> Select<U>(Func<T, U> selector)
            => HasValue ? Maybe.Just(selector(_value)) : Maybe.Nothing<U>();

        /// <summary>
        /// Filter a <see cref="Maybe{T}"/> according to a predicate.
        /// </summary>
        /// <param name="predicate">A predicate to apply to the value contained within the <see cref="Maybe{T}"/>.</param>
        /// <returns>A <see cref="Maybe{T}"/> containing the current <see cref="Maybe{T}"/>'s <see cref="Value"/>, if the <see cref="HasValue"/> property returns true and the value satisfies the predicate, or <see cref="Maybe.Nothing{T}()"/></returns>
        public Maybe<T> Where(Func<T, bool> predicate)
            => HasValue && predicate(_value) ? this : Maybe.Nothing<T>();
        
        /// <summary>
        /// Projects the element of the <see cref="Maybe{T}"/> into a possibly-absent value, and flattens the resulting value into a single <see cref="Maybe{T}"/>.
        /// </summary>
        /// <param name="selector">A transformation function to apply to the contained value</param>
        /// <typeparam name="U">The type of the resulting possibly-absent value</typeparam>
        /// <returns>The resulting <see cref="Maybe{U}"/>, or <see cref="Maybe.Nothing{U}()"/> if the <see cref="HasValue"/> property returns false or the selector returns an absent value</returns>
        public Maybe<U> SelectMany<U>(Func<T, Maybe<U>> selector)
            => SelectMany(selector, (t, u) => u);
        
        /// <summary>
        /// Projects the element of the <see cref="Maybe{T}"/> into a possibly-absent value, and flattens the resulting value into a single <see cref="Maybe{T}"/>, applying a result selector function to the two values.
        /// </summary>
        /// <param name="selector">A transformation function to apply to the contained value</param>
        /// <param name="result">A transformation function to apply to the contained value and the value contained in the selected <see cref="Maybe{U}"/></param>
        /// <typeparam name="U">The type of the value to select</typeparam>
        /// <typeparam name="R">The type of the resulting possibly-absent value</typeparam>
        /// <returns>The result of applying <paramref name="selector"/> to the contained value and <paramref name="result"/> to the intermediate values, or <see cref="Maybe.Nothing{R}()"/> if the <see cref="HasValue"/> property returns false or the selector returns an absent value</returns>
        public Maybe<R> SelectMany<U, R>(Func<T, Maybe<U>> selector, Func<T, U, R> result)
        {
            if (!HasValue)
            {
                return Maybe.Nothing<R>();
            }
            var mu = selector(_value);
            if (!mu.HasValue)
            {
                return Maybe.Nothing<R>();
            }
            return Maybe.Just(result(_value, mu._value));
        }

        /// <summary>
        /// Cast the value contained in the <see cref="Maybe{T}"/> to the specified result type
        /// </summary>
        /// <typeparam name="U">The type to cast the contained value to</typeparam>
        /// <exception cref="System.InvalidCastException">Thrown when the contained value is not an instance of <typeparamref name="U"/></exception>
        /// <returns>A <see cref="Maybe{U}"/> containing this <see cref="Maybe{T}"/>'s value casted to <typeparamref name="U"/>, if the <see cref="HasValue"/> property returns true, or <see cref="Maybe.Nothing{U}()"/>.</returns>
        public Maybe<U> Cast<U>()
            => HasValue ? Maybe.Just((U)(object)_value) : Maybe.Nothing<U>();
        
        /// <summary>
        /// Cast the value contained in the <see cref="Maybe{T}"/> to the specified result type, or return <see cref="Maybe.Nothing{U}()"/> if the contained value is not an instance of <typeparamref name="U"/>.
        /// </summary>
        /// <typeparam name="U">The type to cast the contained value to</typeparam>
        /// <returns>A <see cref="Maybe{U}"/> containing this <see cref="Maybe{T}"/>'s value casted to <typeparamref name="U"/>, if the <see cref="HasValue"/> property returns true and the contained value is an instance of <typeparamref name="U"/>, or <see cref="Maybe.Nothing{U}()"/>.</returns>
        public Maybe<U> OfType<U>()
            => HasValue && _value is U ? Maybe.Just((U)(object)_value) : Maybe.Nothing<U>();

        /// <inheritdoc/>
        public bool Equals(Maybe<T> other)
            => (this.HasValue && other.HasValue && object.Equals(this.Value, other.Value))
            || (!this.HasValue && !other.HasValue);

        /// <inheritdoc/>
        public override bool Equals(object other)
            => other is Maybe<T>
            && Equals((Maybe<T>)other);
        
        /// <inheritdoc/>
        public static bool operator ==(Maybe<T> left, Maybe<T> right)
            => left.Equals(right);
        /// <inheritdoc/>
        public static bool operator !=(Maybe<T> left, Maybe<T> right)
            => !left.Equals(right);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + HasValue.GetHashCode();
                hash = hash * 23 + Value?.GetHashCode() ?? 0;
                return hash;
            }
        }
    }
}
