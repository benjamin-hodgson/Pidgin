using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

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
    public int Read(Span<char> buffer) => _input.Read(buffer);
}
