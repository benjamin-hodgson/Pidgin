using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin
{
    internal partial struct ParseState<TToken>
    {
        private bool _eof;
        private Maybe<TToken> _unexpected;
        private int _errorLocation;
        private string? _message;
        public InternalError<TToken> Error
        {
            get
            {
                return new InternalError<TToken>(_unexpected, _eof, _errorLocation, _message);
            }
            set
            {
                _unexpected = value.Unexpected;
                _eof = value.EOF;
                _errorLocation = value.ErrorLocation;
                _message = value.Message;
            }
        }
        public ParseError<TToken> BuildError(ICollection<Expected<TToken>> expecteds)
            => new ParseError<TToken>(_unexpected, _eof, expecteds.ToImmutableArray(), ComputeSourcePosAt(_errorLocation), _message);

        public ExpectedCollector<TToken> GetExpectedCollector()
            => _expectedCollectorPool.Get();
        public void ReturnExpectedCollector(ExpectedCollector<TToken> collector)
            => _expectedCollectorPool.Return(collector);
    }
}
