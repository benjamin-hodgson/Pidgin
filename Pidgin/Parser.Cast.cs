namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Cast the return value of the current parser to the specified result type.
    /// </summary>
    /// <typeparam name="U">The type to cast the return value to.</typeparam>
    /// <exception cref="System.InvalidCastException">Thrown when the return value is not an instance of <typeparamref name="U"/>.</exception>
    /// <returns>A parser which returns this parser's return value casted to <typeparamref name="U"/>.</returns>
    public Parser<TToken, U> Cast<U>() => Select(x => (U)(object)x!);
}
