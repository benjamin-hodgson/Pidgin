using System.Buffers;

namespace Pidgin.Configuration;

/// <summary>An object which can get <see cref="ArrayPool{T}"/> instances for an arbitrary type.</summary>
public interface IArrayPoolProvider
{
    /// <summary>
    /// Gets an <see cref="ArrayPool{T}"/> instance for elements of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array pool.</typeparam>
    /// <returns>An <see cref="ArrayPool{T}"/> instance for elements of type <typeparamref name="T"/>.</returns>
    ArrayPool<T> GetArrayPool<T>();
}
