using System.Buffers;

namespace Pidgin.Configuration;

/// <summary>
/// Always returns <see cref="ArrayPool{T}.Shared"/>.
/// </summary>
public class DefaultArrayPoolProvider : IArrayPoolProvider
{
    private DefaultArrayPoolProvider()
    {
    }

    /// <summary>
    /// Always returns <see cref="ArrayPool{T}.Shared"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array pool.</typeparam>
    /// <returns><see cref="ArrayPool{T}.Shared"/>.</returns>
    public ArrayPool<T> GetArrayPool<T>() => ArrayPool<T>.Shared;

    /// <summary>
    /// The shared global instance of <see cref="DefaultArrayPoolProvider"/>.
    /// </summary>
    public static IArrayPoolProvider Instance { get; } = new DefaultArrayPoolProvider();
}
