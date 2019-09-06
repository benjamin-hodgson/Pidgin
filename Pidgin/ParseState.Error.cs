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
        private string _message;
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
        public ParseError<TToken> BuildError()
            => BuildError(_expecteds.AsEnumerable());
        public ParseError<TToken> BuildError(IEnumerable<Expected<TToken>> expecteds)
            => new ParseError<TToken>(_unexpected, _eof, expecteds.Distinct().ToArray(), ComputeSourcePosAt(_errorLocation), _message);

        // I'm basically using _expecteds as a set builder.
        // When a parser fails (and has an expected) it calls AddExpected to store the expected.
        // At the end of parsing, if there was an error we bounce the PooledList out to a set
        // (which takes care of dupes).
        //
        // This is complicated by the fact that certain individual parsers need to
        // manipulate the expecteds depending on the exact nature of the error.
        // For example -
        //     OneOf(Fail(), Any.Then(Fail))
        // - the second parser consumes some input before failing, so the reported error should
        // be the second parser's error, and should *not* include any of the first parser's expecteds.
        //
        // I manage this using a "transaction" model. Parsers which need to manipulate
        // expecteds run their child parsers inside a transaction. They can then commit the
        // transaction, if we want to report the child parsers' errors, or roll it back.
        // Nested transactions are supported, because...
        //
        // In the OneOf example, the parent parser needs to be able to drop all
        // but the most recent parser's expecteds. It does this using the "transaction state",
        // the list of expecteds that were added by the currently-running transaction.
        // Get a copy of the transaction state of the second parser, drop *all* of the parsers'
        // expecteds, and then reapply the delta from the second parser.
        // See Parser.OneOf.cs for the code.
        //
        // This expecteds infrastructure works by using a pair of stacks (PooledLists).
        // _expecteds contains the current set of expecteds (both committed and new ones)
        // in the order that they were added. _expectedBookmarks contains a history of the length of
        // _expecteds when each transaction was started. When committing a transaction we simply pop that bookmark;
        // when rolling back a transaction, we pop the bookmark and then dump out all
        // of the _expecteds taller than that height. Note that expecteds that were
        // committed may still be rolled back, if we're running in a nested transaction.
        // (When read bottom to top, _expectedBookmarks is monotonically increasing.)
        // 
        // That's why I can't just use ImmutableSortedSet.Builder here:
        // I need to retain the order in which expecteds were added, so that I can drop them if necessary.
        //
        // Previously each parser would return a set of expecteds along with its error,
        // and OneOf would pick one of those sets to return (or merge them).
        // That turned out to be slow (allocations), especially in the merge case, hence this imperative implementation on top of pooled memory
        private PooledList<Expected<TToken>> _expecteds;
        private PooledList<int> _expectedBookmarks;

        public void BeginExpectedTran()
        {
            _expectedBookmarks.Add(_expecteds.Count);
        }
        public void AddExpected(Expected<TToken> expected)
        {
            _expecteds.Add(expected);
        }
        public void AddExpected(ImmutableArray<Expected<TToken>> expected)
        {
            _expecteds.AddRange(expected);
        }
        public void AddExpected(ReadOnlySpan<Expected<TToken>> expected)
        {
            _expecteds.AddRange(expected);
        }
        public void EndExpectedTran(bool commit)
        {
            var newCount = _expectedBookmarks.Pop();
            if (!commit)
            {
                _expecteds.Shrink(newCount);
            }
        }
        public PooledArray<Expected<TToken>> ExpectedTranState()
            => PooledArray<Expected<TToken>>.From(
                _expecteds
                    .AsSpan()
                    .Slice(_expectedBookmarks[_expectedBookmarks.Count - 1])
            );
    }
}
