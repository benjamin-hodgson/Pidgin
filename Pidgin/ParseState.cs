using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Pidgin.TokenStreams;
using Pidgin.Configuration;
using Microsoft.Extensions.ObjectPool;

namespace Pidgin
{
    /// <summary>
    /// A mutable struct! Careful!
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    internal ref partial struct ParseState<TToken>
    {
        private static readonly bool _needsClear = RuntimeHelpers.IsReferenceOrContainsReferences<TToken>();

        public IConfiguration<TToken> Configuration { get; }
        private readonly Func<TToken, SourcePos, SourcePos> _sourcePosCalculator;
        private readonly ArrayPool<TToken>? _arrayPool;
        private readonly ITokenStream<TToken>? _stream;
        private readonly int _bufferChunkSize;

        private TToken[]? _buffer;
        private ReadOnlySpan<TToken> _span;
        private int _bufferStartLocation;  // how many tokens had been consumed up to the start of the buffer?
        private int _currentIndex;
        private int _bufferedCount;

        private int _lastSourcePosLocation;
        private SourcePos _lastSourcePos;

        // a monotonic stack of locations.
        // I know you'll forget this, so: you can't make this into a stack of _currentIndexes,
        // because dropping the buffer's prefix would invalidate the bookmarks
        private PooledList<int> _bookmarks;

        private ObjectPool<ExpectedCollector<TToken>> _expectedCollectorPool;

        public ParseState(IConfiguration<TToken> configuration, ReadOnlySpan<TToken> span)
        {
            Configuration = configuration;
            _sourcePosCalculator = Configuration.SourcePosCalculator;
            _arrayPool = null;
            _bookmarks = new PooledList<int>(Configuration.ArrayPoolProvider.GetArrayPool<int>(), 0);
            _stream = default;

            _bufferChunkSize = 0;
            _buffer = default;
            _span = span;
            _bufferStartLocation = 0;
            _currentIndex = 0;
            _bufferedCount = span.Length;

            _lastSourcePosLocation = 0;
            _lastSourcePos = new SourcePos(1,1);

            _eof = default;
            _unexpected = default;
            _errorLocation = default;
            _message = default;

            var pooledObjectedPolicy = new ExpectedCollector<TToken>.PooledObjectPolicy(
                configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>()
            );
            _expectedCollectorPool = configuration.ObjectPoolProvider.Create(pooledObjectedPolicy);
        }

        public ParseState(IConfiguration<TToken> configuration, ITokenStream<TToken> stream)
        {
            Configuration = configuration;
            _sourcePosCalculator = Configuration.SourcePosCalculator;
            _arrayPool = Configuration.ArrayPoolProvider.GetArrayPool<TToken>();
            _bookmarks = new PooledList<int>(Configuration.ArrayPoolProvider.GetArrayPool<int>());
            _stream = stream;

            _bufferChunkSize = stream.ChunkSizeHint;
            _buffer = _arrayPool.Rent(_bufferChunkSize);
            _span = _buffer.AsSpan();
            _bufferStartLocation = 0;
            _currentIndex = 0;
            _bufferedCount = 0;

            _lastSourcePosLocation = 0;
            _lastSourcePos = new SourcePos(1,1);

            _eof = default;
            _unexpected = default;
            _errorLocation = default;
            _message = default;

            var pooledObjectedPolicy = new ExpectedCollector<TToken>.PooledObjectPolicy(
                configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>()
            );
            _expectedCollectorPool = configuration.ObjectPoolProvider.Create(pooledObjectedPolicy);
            
            Buffer(0);
        }

        /// <summary>
        /// How many tokens have been consumed in total?
        /// </summary>
        /// <value></value>
        public int Location
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _bufferStartLocation + _currentIndex;
            }
        }

        public bool HasCurrent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _currentIndex < _bufferedCount;
            }
        }
        public TToken Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _span[_currentIndex];
            }
        }

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
            count -= bufferedCount;
        }

        // if it returns a span shorter than count it's because you reached the end of the input
        public ReadOnlySpan<TToken> LookAhead(int count)
        {
            Buffer(count);
            return _span.Slice(_currentIndex, Math.Min(_bufferedCount - _currentIndex, count));
        }
        // if it returns a span shorter than count it's because you looked further back than the buffer goes
        public ReadOnlySpan<TToken> LookBehind(int count)
        {
            var start = Math.Max(0, _currentIndex - count);
            return _span.Slice(start, _currentIndex - start);
        }

        // postcondition: bufferedLength >= _currentIndex + min(readAhead, AmountLeft(_stream))
        private void Buffer(int readAhead)
        {
            var readAheadTo = _currentIndex + readAhead;
            if (readAheadTo >= _bufferedCount && _stream != null)
            {
                // we're about to read past the end of the current chunk. Pull a new chunk from the stream
                var keepSeenLength = _bookmarks.Count > 0
                    ? Location - _bookmarks[0]
                    : 0;
                var keepFrom = _currentIndex - keepSeenLength;
                var keepLength = _bufferedCount - keepFrom;
                var amountToRead = Math.Max(_bufferChunkSize, readAheadTo - _bufferedCount);
                var newBufferLength = keepLength + amountToRead;

                //                  _currentIndex
                //                        |
                //                        | _bufferedCount
                //              keepFrom  |      |
                //                 |      |      | readAheadTo
                //                 |      |      |    |
                //              abcdefghijklmnopqrstuvwxyz
                //       readAhead        |-----------|
                //  keepSeenLength |------|
                //      keepLength |-------------|
                //    amountToRead               |----|
                // newBufferLength |------------------|


                UpdateLastSourcePos();

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
                _bufferedCount += _stream!.ReadInto(_buffer.AsSpan().Slice(_bufferedCount, amountToRead));
            }
        }
        
        public void PushBookmark()
        {
            _bookmarks.Add(Location);
        }

        public void PopBookmark()
        {
            _bookmarks.Pop();
        }

        public void Rewind()
        {
            var bookmark = _bookmarks.Pop();
            
            var delta = Location - bookmark;

            if (delta > _currentIndex)
            {
                throw new InvalidOperationException("Tried to rewind past the start of the input. Please report this as a bug in Pidgin!");
            }
            _currentIndex -= delta;
        }

        public SourcePos ComputeSourcePos()
        {
            UpdateLastSourcePos();
            return ComputeSourcePosAt(Location);
        }

        private void UpdateLastSourcePos()
        {
            var location = _bookmarks.Count > 0
                ? _bookmarks[0]
                : Location;

            _lastSourcePos = ComputeSourcePosAt(location);
            _lastSourcePosLocation = location;
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                _arrayPool!.Return(_buffer, _needsClear);
                _buffer = null;
            }
            _stream?.Dispose();
            _bookmarks.Clear();
        }
    }
}
