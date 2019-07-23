using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin
{
    ///
    public abstract class Pattern<TToken>
    {
        // todo: public static Pattern<char> Parse(string pattern);
        
        ///
        public static Pattern<TToken> Token(Func<TToken, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return new TokenPattern(predicate);
        }

        ///
        public static Pattern<TToken> Token(TToken token)
            => Token(t => EqualityComparer<TToken>.Default.Equals(token, t));

        ///
        public Pattern<TToken> Then(Pattern<TToken> pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }
            if (this is SequencePattern s1)
            {
                return s1.AddRight(pattern);
            }
            if (pattern is SequencePattern s2)
            {
                return s2.AddLeft(this);
            }
            return new SequencePattern(ImmutableList.Create(this, pattern));
        }
        ///
        public Pattern<TToken> Repeat(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            return new SequencePattern(Enumerable.Repeat(this, count).ToImmutableList());
        }

        ///
        public Pattern<TToken> Or(Pattern<TToken> pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }
            if (this is OneOfPattern o1)
            {
                return o1.AddRight(pattern);
            }
            if (pattern is OneOfPattern o2)
            {
                return o2.AddLeft(this);
            }
            return new OneOfPattern(ImmutableList.Create(this, pattern));
        }

        ///
        public static Pattern<TToken> Try(Pattern<TToken> pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }
            return new TryPattern(pattern);
        }

        internal abstract (bool success, int consumed) Match(ref ParseState<TToken> state);

        internal sealed class TokenPattern : Pattern<TToken>
        {
            private readonly Func<TToken, bool> _predicate;
            public TokenPattern(Func<TToken, bool> predicate)
            {
                _predicate = predicate;
            }

            internal sealed override (bool success, int consumed) Match(ref ParseState<TToken> state)
            {
                if (!state.HasCurrent)
                {
                    state.Error = new InternalError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        state.SourcePos,
                        null
                    );
                    return (false, 0);
                }
                var token = state.Current;
                if (!_predicate(token))
                {
                    state.Error = new InternalError<TToken>(
                        Maybe.Just(token),
                        false,
                        state.SourcePos,
                        null
                    );
                    return (false, 0);
                }
                state.Advance();
                return (true, 1);
            }
        }

        internal sealed class SequencePattern : Pattern<TToken>
        {
            private readonly ImmutableList<Pattern<TToken>> _patterns;

            public SequencePattern(ImmutableList<Pattern<TToken>> patterns)
            {
                _patterns = patterns;
            }

            internal sealed override (bool success, int consumed) Match(ref ParseState<TToken> state)
            {
                var total = 0;
                foreach (var p in _patterns)
                {
                    var result = p.Match(ref state);
                    total += result.consumed;
                
                    if (!result.success)
                    {
                        return (false, total);
                    }
                }

                return (true, total);
            }

            internal Pattern<TToken> AddRight(Pattern<TToken> other)
            {
                if (other is SequencePattern s)
                {
                    return new SequencePattern(_patterns.AddRange(s._patterns));
                }
                return new SequencePattern(_patterns.Add(other));
            }
            internal Pattern<TToken> AddLeft(Pattern<TToken> other)
            {
                if (other is SequencePattern s)
                {
                    return new SequencePattern(s._patterns.AddRange(_patterns));
                }
                return new SequencePattern(_patterns.Insert(0, other));
            }
        }

        internal sealed class OneOfPattern : Pattern<TToken>
        {
            private readonly ImmutableList<Pattern<TToken>> _patterns;

            public OneOfPattern(ImmutableList<Pattern<TToken>> patterns)
            {
                _patterns = patterns;
            }

            // see comment about expecteds in ParseState.Error.cs
            internal sealed override (bool success, int consumed) Match(ref ParseState<TToken> state)
            {
                var firstTime = true;
                var err = new InternalError<TToken>(
                    Maybe.Nothing<TToken>(),
                    false,
                    state.SourcePos,
                    "OneOf had no arguments"
                );
                state.BeginExpectedTran();
                foreach (var p in _patterns)
                {
                    state.BeginExpectedTran();
                    var result = p.Match(ref state);
                    if (result.success)
                    {
                        // throw out all expecteds
                        state.EndExpectedTran(false);
                        state.EndExpectedTran(false);
                        return result;
                    }

                    // we'll usually return the error from the first parser that didn't backtrack,
                    // even if other parsers had a longer match.
                    // There is some room for improvement here.
                    if (result.consumed > 0)
                    {
                        // throw out all expecteds except this one
                        var expected = state.ExpectedTranState();
                        state.EndExpectedTran(false);
                        state.EndExpectedTran(false);
                        state.AddExpected(expected.AsSpan());
                        expected.Dispose();
                        return result;
                    }

                    state.EndExpectedTran(true);
                    // choose the longest match, preferring the left-most error in a tie,
                    // except the first time (avoid returning "OneOf had no arguments").
                    if (firstTime || state.Error.ErrorPos > err.ErrorPos)
                    {
                        err = state.Error;
                    }
                    firstTime = false;
                }
                state.Error = err;
                state.EndExpectedTran(true);
                return (false, 0);
            }

            internal Pattern<TToken> AddRight(Pattern<TToken> other)
            {
                if (other is OneOfPattern s)
                {
                    return new OneOfPattern(_patterns.AddRange(s._patterns));
                }
                return new OneOfPattern(_patterns.Add(other));
            }
            internal Pattern<TToken> AddLeft(Pattern<TToken> other)
            {
                if (other is OneOfPattern s)
                {
                    return new OneOfPattern(s._patterns.AddRange(_patterns));
                }
                return new OneOfPattern(_patterns.Insert(0, other));
            }
        }

        internal sealed class TryPattern : Pattern<TToken>
        {
            private readonly Pattern<TToken> _pattern;

            public TryPattern(Pattern<TToken> pattern)
            {
                _pattern = pattern;
            }

            internal sealed override (bool success, int consumed) Match(ref ParseState<TToken> state)
            {
                // start buffering the input
                state.PushBookmark();
                var result = _pattern.Match(ref state);
                if (!result.success)
                {
                    // return to the start of the buffer and discard the bookmark
                    state.Rewind();
                    return (false, 0);
                }

                // discard the buffer
                state.PopBookmark();
                return result;
            }
        }
    }
}