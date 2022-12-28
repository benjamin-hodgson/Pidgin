using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Pidgin.TokenStreams;

/// <summary>
/// An <see cref="ITokenStream{TToken}"/> implementation based on an <see cref="IList{TToken}"/>.
/// </summary>
/// <typeparam name="TToken">The type of tokens in the list.</typeparam>
[SuppressMessage(
    "naming",
    "CA1711:Rename type name so that it does not end in 'Stream'",
    Justification = "It's a TokenStream, not a System.IO.Stream"
)]
public sealed class ListTokenStream<TToken> : ITokenStream<TToken>
{
    /// <summary>Returns 16.</summary>
    /// <returns>16.</returns>
    public int ChunkSizeHint => 16;

    private readonly IList<TToken> _input;
    private int _index = 0;

    /// <summary>
    /// Creates an <see cref="ITokenStream{TToken}"/> implementation based on an <see cref="IList{TToken}"/>.
    /// </summary>
    /// <param name="input">The <see cref="IList{TToken}"/>.</param>
    public ListTokenStream(IList<TToken> input)
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
    public int Read(Span<TToken> buffer)
    {
        var actualLength = Math.Min(_input.Count - _index, buffer.Length);
        for (var i = 0; i < actualLength; i++)
        {
            buffer[i] = _input[_index];
            _index++;
        }

        return actualLength;
    }
}
