using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Pidgin.Configuration;

namespace Pidgin;

/// <summary>
/// Represents the state of a parsing process.
/// Includes functionality managing and buffering the input stream,
/// reporting errors, and computing source positions.
///
/// For efficiency, this object is implemented as a mutable struct
/// and is intended to be passed by reference.
///
/// WARNING: This API is <strong>unstable</strong>
/// and subject to change in future versions of the library.
/// </summary>
/// <typeparam name="TToken">The type of tokens consumed by the parser.</typeparam>
[StructLayout(LayoutKind.Auto)]
[EditorBrowsable(EditorBrowsableState.Advanced)]
public ref partial struct ParseState<TToken>
{
    private static readonly bool _needsClear = RuntimeHelpers.IsReferenceOrContainsReferences<TToken>();

    /// <summary>Gets the parser configuration.</summary>
    public IConfiguration<TToken> Configuration { get; }

    private readonly Func<TToken, SourcePosDelta> _sourcePosCalculator;
    private readonly ArrayPool<TToken>? _arrayPool;
    private readonly ITokenStream<TToken>? _stream;
    private readonly int _bufferChunkSize;

    private TToken[]? _buffer;  // to return to the pool
    private ReadOnlySpan<TToken> _span;
    private long _keepFromLocation;  // leftmost bookmark which hasn't been discarded
    private long _bufferStartLocation;  // how many tokens had been consumed up to the start of the buffer?
    private int _currentIndex;  // index into the _span
    private int _bufferedCount;

    private int _numberOfBookmarks;

    private long _lastSourcePosDeltaLocation;
    private SourcePosDelta _lastSourcePosDelta;

    internal ParseState(IConfiguration<TToken> configuration, ReadOnlySpan<TToken> span)
    {
        Configuration = configuration;
        _sourcePosCalculator = Configuration.SourcePosCalculator;
        _arrayPool = null;
        _keepFromLocation = -1;
        _stream = default;

        _bufferChunkSize = 0;
        _buffer = default;
        _span = span;
        _bufferStartLocation = 0;
        _currentIndex = 0;
        _bufferedCount = span.Length;

        _lastSourcePosDeltaLocation = 0;
        _lastSourcePosDelta = SourcePosDelta.Zero;

        _numberOfBookmarks = 0;

        _eof = default;
        _unexpected = default;
        ErrorLocation = default;
        _message = default;
    }

    internal ParseState(IConfiguration<TToken> configuration, ITokenStream<TToken> stream)
    {
        Configuration = configuration;
        _sourcePosCalculator = Configuration.SourcePosCalculator;
        _arrayPool = Configuration.ArrayPoolProvider.GetArrayPool<TToken>();
        _keepFromLocation = -1;
        _stream = stream;

        _bufferChunkSize = stream.ChunkSizeHint;
        _buffer = _arrayPool.Rent(_bufferChunkSize);
        _span = _buffer.AsSpan();
        _bufferStartLocation = 0;
        _currentIndex = 0;
        _bufferedCount = 0;

        _lastSourcePosDeltaLocation = 0;
        _lastSourcePosDelta = SourcePosDelta.Zero;

        _numberOfBookmarks = 0;

        _eof = default;
        _unexpected = default;
        ErrorLocation = default;
        _message = default;

        Buffer(0);
    }

    /// <summary>
    /// Returns the total number of tokens which have been consumed.
    /// In other words, the current absolute offset of the input stream.
    /// </summary>
    public long Location
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return _bufferStartLocation + _currentIndex;
        }
    }

    /// <summary>
    /// Returns true if the parser has not reached the end of the input.
    /// </summary>
    public bool HasCurrent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return _currentIndex < _bufferedCount;
        }
    }

    /// <summary>
    /// Returns the current token.
    /// </summary>
    public TToken Current
    {
        get
        {
            return LookAhead(1)[0];
        }
    }

    // if it returns a span shorter than count it's because you reached the end of the input

    /// <summary>
    /// Returns a <see cref="Span{TToken}"/> containing the next <paramref name="count"/> tokens.
    ///
    /// This method may return a span shorter than <paramref name="count"/>,
    /// if the parser reaches the end of the input stream.
    /// </summary>
    /// <param name="count">The number of tokens to advance.</param>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> containing the tokens.</returns>
    public ReadOnlySpan<TToken> LookAhead(int count)
    {
        Buffer(count);

        // may have buffered fewer than `count` tokens
        var span = _span.Slice(_currentIndex, Math.Min(_bufferedCount - _currentIndex, count));

        if (_onLookaheadActions?.Count > 0)
        {
            var lookAheadToLocation = Location + span.Length;
            foreach (var action in _onLookaheadActions)
            {
                action(lookAheadToLocation);
            }
        }

        return span;
    }

    private Stack<Action<long>>? _onLookaheadActions;

    internal void PushOnLookaheadAction(Action<long> action)
    {
        if (_onLookaheadActions == null)
        {
            _onLookaheadActions = new Stack<Action<long>>();
        }

        _onLookaheadActions.Push(action);
    }

    internal void PopOnLookaheadAction(Action<long> action)
    {
        if (_onLookaheadActions == null || _onLookaheadActions.Count == 0)
        {
            throw new InvalidOperationException("Tried to pop an action from an empty stack. Please report this as a bug in Pidgin!");
        }

        if (_onLookaheadActions.Peek() != action)
        {
            throw new InvalidOperationException("Tried to pop an action that doesn't match the top of the stack. Please report this as a bug in Pidgin!");
        }

        _onLookaheadActions.Pop();
    }

    // if it returns a span shorter than count it's because you looked further back than the buffer goes
    internal ReadOnlySpan<TToken> LookBehind(int count)
    {
        var start = Math.Max(0, _currentIndex - count);
        return _span[start.._currentIndex];
    }

    /// <summary>
    /// Advance the input stream by <paramref name="count"/> tokens.
    /// </summary>
    /// <param name="count">The number of tokens to advance.</param>
    public void Advance(int count = 1)
    {
        if (_stream == null)
        {
            // reading from a span, so advance is just a pointer bump
            _currentIndex = Math.Min(_currentIndex + count, _span.Length);
            return;
        }

        var alreadyBufferedCount = Math.Min(count, _bufferedCount - _currentIndex);
        _currentIndex += alreadyBufferedCount;
        count -= alreadyBufferedCount;

        Buffer(count);

        var bufferedCount = Math.Min(count, _bufferedCount - _currentIndex);
        _currentIndex += bufferedCount;
    }

    // postcondition: bufferedLength >= _currentIndex + min(readAhead, AmountLeft(_stream))
    private void Buffer(int readAhead)
    {
        var readAheadTo = _currentIndex + readAhead;
        if (readAheadTo >= _bufferedCount && _stream != null)
        {
            // we're about to read past the end of the current chunk. Pull a new chunk from the stream

            /*                  _currentIndex
            *                        |
            *                        | _bufferedCount
            *              keepFrom  |      |
            *                 |      |      | readAheadTo
            *                 |      |      |    |
            * input:       abcdefghijklmnopqrstuvwxyz
            *       readAhead        |-----------|
            *  keepSeenLength |------|
            *      keepLength |-------------|
            *    amountToRead               |----|
            * newBufferLength |------------------|
            */

            // expect keepSeenLength to be small enough to fit in an int since we're subtracting
            var keepSeenLength = _keepFromLocation >= 0
                ? (int)(Location - _keepFromLocation)
                : 0;
            var keepFrom = _currentIndex - keepSeenLength;
            var keepLength = _bufferedCount - keepFrom;
            var amountToRead = Math.Max(_bufferChunkSize, readAheadTo - _bufferedCount);
            var newBufferLength = keepLength + amountToRead;
            UpdateLastSourcePosDelta();

            if (newBufferLength > _buffer!.Length)
            {
                // grow the buffer
                var newBuffer = _arrayPool!.Rent(Math.Max(newBufferLength, _buffer.Length * 2));

                Array.Copy(_buffer, keepFrom, newBuffer, 0, keepLength);

                _arrayPool.Return(_buffer, _needsClear);
                _buffer = newBuffer;
                _span = _buffer.AsSpan();
            }
            else if (keepFrom != 0 && keepLength != 0)
            {
                // move the buffer's contents to the start

                // todo: find out how expensive this Copy tends to be.
                // Could prevent it by using a ring buffer, but might make reads slower
                Array.Copy(_buffer, keepFrom, _buffer, 0, keepLength);
            }

            _bufferStartLocation += keepFrom;
            _currentIndex = keepSeenLength;
            _bufferedCount = keepLength;
            _bufferedCount += _stream!.Read(_buffer.AsSpan().Slice(_bufferedCount, amountToRead));
        }
    }

    /// <summary>Start buffering the input.</summary>
    /// <returns>The location of the bookmark.</returns>
    public long Bookmark()
    {
        if (_keepFromLocation < 0)
        {
            _keepFromLocation = Location;
        }

        _numberOfBookmarks++;

        return Location;
    }

    /// <summary>Stop buffering the input.</summary>
    /// <param name="bookmark">The location of the bookmark.</param>
    public void DiscardBookmark(long bookmark)
    {
        if (bookmark < _keepFromLocation || bookmark > Location || _numberOfBookmarks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bookmark), bookmark, "Tried to discard a bookmark with invalid state. Please report this as a bug in Pidgin!");
        }

        _numberOfBookmarks--;

        if (_numberOfBookmarks == 0)
        {
            _keepFromLocation = -1;
        }
    }

    /// <summary>Return to a bookmark previously obtained from <see cref="Bookmark"/> and discard it.</summary>
    /// <param name="bookmark">The location of the bookmark.</param>
    public void Rewind(long bookmark)
    {
        // expect delta to be small enough to fit in an int since we're subtracting
        var delta = (int)(Location - bookmark);

        if (delta > _currentIndex)
        {
            throw new InvalidOperationException("Tried to rewind past the start of the input. Please report this as a bug in Pidgin!");
        }

        _currentIndex -= delta;
        DiscardBookmark(bookmark);
    }

    internal SourcePosDelta ComputeSourcePosDelta()
    {
        UpdateLastSourcePosDelta();
        return ComputeSourcePosDeltaAt(Location);
    }

    private void UpdateLastSourcePosDelta()
    {
        var location = _keepFromLocation >= 0
            ? _keepFromLocation
            : Location;

        _lastSourcePosDelta = ComputeSourcePosDeltaAt(location);
        _lastSourcePosDeltaLocation = location;
    }

    internal void Dispose()
    {
        if (_buffer != null)
        {
            _stream!.Return(_buffer.AsSpan()[_currentIndex.._bufferedCount]);
            _arrayPool!.Return(_buffer, _needsClear);
            _buffer = null;
        }
    }
}
