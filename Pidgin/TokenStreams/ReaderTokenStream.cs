using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
#if !NETCOREAPP2_1_OR_GREATER
using System.Buffers;
#endif

namespace Pidgin.TokenStreams;

/// <summary>
/// An <see cref="ITokenStream{TToken}"/> implementation based on a <see cref="TextReader"/>.
/// </summary>
[SuppressMessage(
    "naming",
    "CA1711:Rename type name so that it does not end in 'Stream'",
    Justification = "It's a TokenStream, not a System.IO.Stream"
)]
public class ReaderTokenStream : ITokenStream<char>
{
    /// <summary>Returns 4096.</summary>
    /// <returns>4096.</returns>
    public int ChunkSizeHint => 4096;

    private readonly TextReader _input;

    /// <summary>
    /// Creates an <see cref="ITokenStream{TToken}"/> implementation based on a <see cref="TextReader"/>.
    /// </summary>
    /// <param name="input">The <see cref="TextReader"/>.</param>
    public ReaderTokenStream(TextReader input)
    {
        _input = input;
    }

    /// <summary>
    /// Read up to <c>buffer.Length</c> tokens into <paramref name="buffer"/>.
    /// Return the actual number of tokens read, which may be fewer than
    /// the size of the buffer if the stream has reached the end.
    /// </summary>
    /// <param name="buffer">The buffer to read tokens into.</param>
    /// <returns>The actual number of tokens read.</returns>
    public int Read(Span<char> buffer)
    {
#if NETCOREAPP2_1_OR_GREATER
        return _input.Read(buffer);
#else
        var temp = ArrayPool<char>.Shared.Rent(buffer.Length);
        try
        {
            var read = _input.Read(temp, 0, buffer.Length);
            if (read > 0)
            {
                temp.AsSpan(0, read).CopyTo(buffer);
            }

            return read;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(temp);
        }
#endif
    }

#if !NETCOREAPP3_0_OR_GREATER
    /// <summary>
    /// Push some un-consumed tokens back into the stream.
    /// <see cref="Parser{TToken, T}"/>s call this method when they are finished parsing.
    ///
    /// <see cref="ITokenStream{TToken}"/> implementations may override this
    /// method if they want to implement resumable parsing.
    /// (See <see cref="ResumableTokenStream{TToken}"/>.)
    /// The default implementation does nothing and discards the <paramref name="leftovers"/>.
    /// </summary>
    /// <param name="leftovers">The leftovers to push back into the stream.</param>
    public void Return(ReadOnlySpan<char> leftovers)
    {
    }
#endif
}
