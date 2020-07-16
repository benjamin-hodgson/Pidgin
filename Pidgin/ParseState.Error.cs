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
        public ParseError<TToken> BuildError(ref PooledList<Expected<TToken>> expecteds)
        {
            var builder = ImmutableArray.CreateBuilder<Expected<TToken>>(expecteds.Count);
            foreach (var e in expecteds)
            {
                builder.Add(e);
            }
            return new ParseError<TToken>(_unexpected, _eof, builder.MoveToImmutable(), ComputeSourcePosAt(_errorLocation), _message);
        }
    }
}
