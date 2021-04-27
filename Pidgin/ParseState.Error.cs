using System.Collections.Immutable;

namespace Pidgin
{
    public partial struct ParseState<TToken>
    {
        private bool _eof;
        private Maybe<TToken> _unexpected;
        private int _errorLocation;
        private string? _message;
        /// <summary>Gets or sets the error</summary>
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
        /// <summary>
        /// Construct a <see cref="ParseError{TToken}"/> from the current <see cref="Error"/>
        /// and the supplied <paramref name="expecteds"/>.
        /// </summary>
        public ParseError<TToken> BuildError(ref PooledList<Expected<TToken>> expecteds)
        {
            var builder = ImmutableArray.CreateBuilder<Expected<TToken>>(expecteds.Count);
            foreach (var e in expecteds)
            {
                builder.Add(e);
            }
            return new ParseError<TToken>(_unexpected, _eof, builder.MoveToImmutable(), ComputeSourcePosDeltaAt(_errorLocation), _message);
        }
    }
}
