using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Pidgin.TokenStreams;

/// <summary>
/// An <see cref="ITokenStream{TToken}"/> implementation which wraps another <see cref="ITokenStream{TToken}"/>
/// and adds support for resumable parsing.
/// </summary>
/// <typeparam name="TToken">The type of tokens returned by the wrapped <see cref="ITokenStream{TToken}"/>.</typeparam>
[SuppressMessage(
    "naming",
    "CA1711:Rename type name so that it does not end in 'Stream'",
    Justification = "It's a TokenStream, not a System.IO.Stream"
)]
public class ResumableTokenStream<TToken> : ITokenStream<TToken>, IDisposable
{
    private static readonly bool _needsClear = RuntimeHelpers.IsReferenceOrContainsReferences<TToken>();
    private readonly ArrayPool<TToken> _pool;
    private readonly ITokenStream<TToken> _next;
    private TToken[]? _buffer = null;
    private int _bufferStart = 0;  // amount of empty space at left-hand end of _buffer, aka index of first value

    /// <summary>
    /// Creates an <see cref="ITokenStream{TToken}"/> implementation which
    /// adds support for resumable parsing to <paramref name="next"/>.
    /// </summary>
    /// <param name="next">The <see cref="ITokenStream{TToken}"/> to wrap.</param>
    /// <param name="pool">
    /// An <see cref="ArrayPool{TToken}"/> to use for the internal buffer.
    /// Defaults to <see cref="ArrayPool{TToken}.Shared"/>.
    /// </param>
    public ResumableTokenStream(ITokenStream<TToken> next, ArrayPool<TToken>? pool = null)
    {
        if (next == null)
        {
            throw new ArgumentNullException(nameof(next));
        }

        _next = next;
        _pool = pool ?? ArrayPool<TToken>.Shared;
    }

    /// <summary>
    /// Read up to <c>buffer.Length</c> tokens into <paramref name="buffer"/>.
    /// Return the actual number of tokens read, which may be fewer than
    /// the size of the buffer if the stream has reached the end.
    /// </summary>
    /// <param name="buffer">The buffer to read tokens into.</param>
    /// <returns>The actual number of tokens read.</returns>
    public int Read(Span<TToken> buffer)
    {
        var bufferedCount = 0;
        if (_buffer != null && _bufferStart < _buffer.Length)
        {
            bufferedCount = Math.Min(_buffer.Length - _bufferStart, buffer.Length);
            _buffer.AsSpan().Slice(_bufferStart, bufferedCount).CopyTo(buffer);
            _bufferStart += bufferedCount;
        }

        return bufferedCount + _next.Read(buffer[bufferedCount..]);
    }

    /// <summary>
    /// Push some un-consumed tokens back into the stream.
    /// <see cref="Parser{TToken, T}"/>s call this method when they are finished parsing.
    /// </summary>
    /// <param name="leftovers">The leftovers to push back into the stream.</param>
    public void Return(ReadOnlySpan<TToken> leftovers)
    {
        if (leftovers.Length == 0)
        {
            return;
        }

        if (_buffer == null)
        {
            _buffer = _pool.Rent(leftovers.Length);
            _bufferStart = _buffer.Length;
        }

        if (_bufferStart < leftovers.Length)
        {
            var bufferedCount = _buffer.Length - _bufferStart;
            var newBuffer = _pool.Rent(bufferedCount + leftovers.Length);
            var newBufferStart = newBuffer.Length - bufferedCount;

            Array.Copy(_buffer, _bufferStart, newBuffer, newBufferStart, bufferedCount);

            _pool.Return(_buffer, _needsClear);
            _buffer = newBuffer;
            _bufferStart = newBufferStart;
        }

        _bufferStart -= leftovers.Length;
        leftovers.CopyTo(_buffer.AsSpan()[_bufferStart..]);
    }

    /// <summary>Return any buffers to the <see cref="ArrayPool{TToken}"/>.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Return any buffers to the <see cref="ArrayPool{TToken}"/>.</summary>
    /// <param name="disposing">True if this method is being called by <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _buffer != null)
        {
            _pool.Return(_buffer, _needsClear);
            _bufferStart = 0;
        }
    }
}
