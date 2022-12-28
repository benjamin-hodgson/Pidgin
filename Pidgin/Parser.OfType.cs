namespace Pidgin;

public abstract partial class Parser<TToken, T>
{
    /// <summary>
    /// Creates a parser which casts the return value of the current parser to the specified result type, or fails if the return value is not an instance of <typeparamref name="U"/>.
    /// </summary>
    /// <typeparam name="U">The type to cast the return value of the current parser to.</typeparam>
    /// <returns>A parser which returns the current parser's return value casted to <typeparamref name="U"/>, if the value is an instance of <typeparamref name="U"/>.</returns>
    public Parser<TToken, U> OfType<U>()
        =>
            Assert(x => x is U, x => $"Expected a {typeof(U).Name} but got a {x!.GetType().Name}")
            .Cast<U>()
            .Labelled($"result of type {typeof(U).Name}");
}
