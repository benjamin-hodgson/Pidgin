using System;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

/// <summary>
/// An interface for streams of <typeparamref name="TToken"/>, which can be consumed by <see cref="Parser{TToken, T}"/>s.
/// </summary>
/// <typeparam name="TToken">The type of tokens the stream produces.</typeparam>
[SuppressMessage(
    "naming",
    "CA1711:Rename type name so that it does not end in 'Stream'",
    Justification = "It's a TokenStream, not a System.IO.Stream"
)]
public interface ITokenStream<TToken>
{
    /// <summary>
    /// Read up to <c>buffer.Length</c> tokens into <paramref name="buffer"/>.
    /// Return the actual number of tokens read, which may be fewer than
    /// the size of the buffer if the stream has reached the end.
    /// </summary>
    /// <param name="buffer">The buffer to read tokens into.</param>
    /// <returns>The actual number of tokens read.</returns>
    int Read(Span<TToken> buffer);

    /// <summary>
    /// Push some un-consumed tokens back into the stream.
    /// <see cref="Parser{TToken, T}"/>s call this method when they are finished parsing.
    ///
    /// <see cref="ITokenStream{TToken}"/> implementations may override this
    /// method if they want to implement resumable parsing.
    /// (See <see cref="TokenStreams.ResumableTokenStream{TToken}"/>.)
    /// The default implementation does nothing and discards the <paramref name="leftovers"/>.
    /// </summary>
    /// <param name="leftovers">The leftovers to push back into the stream.</param>
    [SuppressMessage(
        "naming",
        "CA1716:Rename member so that it no longer conflicts with a reserved language keyword",
        Justification = "Would be a breaking change"
    )]
    void Return(ReadOnlySpan<TToken> leftovers)
    {
    }

    /// <summary>
    /// A hint to the parser indicating a default number of tokens to request when calling <see cref="Read"/>.
    ///
    /// <see cref="ITokenStream{TToken}"/> implementations may override this
    /// property if there's an optimal amount of data to pull from the stream in a single chunk.
    /// For example, if your token stream has an internal buffer,
    /// then you might want to override <see cref="ChunkSizeHint"/> to return <c>buffer.Length</c>.
    ///
    /// The default is 1024.
    /// </summary>
    /// <returns>The default number of tokens to request when calling <see cref="Read"/>.</returns>
    int ChunkSizeHint => 1024;
}
