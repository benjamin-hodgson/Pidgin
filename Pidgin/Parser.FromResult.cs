namespace Pidgin;

public static partial class Parser<TToken>
{
    /// <summary>
    /// Creates a parser which returns the specified value without consuming any input.
    /// </summary>
    /// <param name="result">The value to return.</param>
    /// <typeparam name="T">The type of the value to return.</typeparam>
    /// <returns>A parser which returns the specified value without consuming any input.</returns>
    public static Parser<TToken, T> FromResult<T>(T result) => Return(result);
}
