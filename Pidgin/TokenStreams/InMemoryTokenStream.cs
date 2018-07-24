using System;

namespace Pidgin.TokenStreams
{
    internal abstract class InMemoryTokenStream<TToken> : ITokenStream<TToken>
    {
        private readonly int _length;
        protected int _index = -1;

        public InMemoryTokenStream(int length)
        {
            _length = length;
        }

        public abstract TToken Current { get; }
        public bool MoveNext()
        {
            _index = Math.Min(_length, _index + 1);
            return _index < _length;
        }
        public bool RewindBy(int count)
        {
            if (count > _index)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Please report this as a bug in Pidgin!");
            }
            _index -= count;
            return _index < _length;
        }

        public virtual void Dispose() { }
        public void StartBuffering() { }
        public void StopBuffering() { }
    }
}
