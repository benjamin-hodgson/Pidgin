using System;
using System.Buffers;

namespace Pidgin
{
    /// <summary>
    /// A mutable struct! Careful!
    /// </summary>
    internal struct PooledStringBuilder
    {
        private const int InitialCapacity = 16;

        private char[] _buffer;
        private int _length;

        public PooledStringBuilder(int initialCapacity)
        {
            _buffer = ArrayPool<char>.Shared.Rent(initialCapacity);
            _length = 0;
        }

        public void Append(char c)
        {
            GrowIfNecessary(1);
            AddChar(c);
        }
        
        public void Append(string s)
        {
            GrowIfNecessary(s.Length);
            foreach (var c in s)
            {
                AddChar(c);
            }
        }

        public string GetStringAndClear()
        {
            if (_buffer == null)
            {
                return "";
            }
            var s = new string(_buffer, 0, _length);
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = null;
            _length = 0;
            return s;
        }

        private void GrowIfNecessary(int appendSize)
        {
            if (_buffer == null)
            {
                _buffer = ArrayPool<char>.Shared.Rent(InitialCapacity);
            }
            else if (_length == _buffer.Length)
            {
                var newBuffer = ArrayPool<char>.Shared.Rent(Math.Max(_buffer.Length * 2, _buffer.Length + appendSize));
                Array.Copy(_buffer, newBuffer, _buffer.Length);
                ArrayPool<char>.Shared.Return(_buffer);
                _buffer = newBuffer;
            }
        }

        private void AddChar(char c)
        {
            _buffer[_length] = c;
            _length++;
        }
    }
}