using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin.Incremental;

internal class CachedParseResultTable
{
    private readonly ImmutableArray<Tree> _children;

    public CachedParseResultTable(ImmutableArray<Tree> children)
    {
        _children = children;
    }

    public Tree? FindSubtree<TToken, T>(long oldLocation, Parser<TToken, T> key)
        where T : class, IShiftable<T>
    {
        var result = Tree.Search(_children, oldLocation, key);
        return result?.ResolvePendingShifts<T>();
    }

    public class Tree
    {
        // Avoid retaining parse results for parsers that have been GCed.
        // Target: Parser<TToken, T>
        // Dependent: T
        private ConditionalWeakReference? _cwr;

        // Children of the tree should be nodes within the ConsumedRange
        private readonly LocationRange _consumedRange;
        private readonly LocationRange _lookaroundRange;

        // NB: it doesn't make sense to put the children behind the CWR.
        // We want to be able to reuse results from small parsers even
        // when the big parser has been recycled.
        private readonly ImmutableArray<Tree> _children;
        private readonly long _shift;

        public Tree(
            ConditionalWeakReference? cwr,
            LocationRange consumedRange,
            LocationRange lookaroundRange,
            ImmutableArray<Tree> children,
            long shift
        )
        {
            _cwr = cwr;
            _consumedRange = consumedRange;
            _lookaroundRange = lookaroundRange;
            _children = children;
            _shift = shift;
        }

        public LocationRange ConsumedRange
        {
            get
            {
                CheckNoPendingShifts();
                return _consumedRange;
            }
        }

        public LocationRange LookaroundRange
        {
            get
            {
                CheckNoPendingShifts();
                return _lookaroundRange;
            }
        }

        public T GetResult<T>()
        {
            CheckNoPendingShifts();

            // GetResult() is called by the parser instance
            // that's the target of the ConditionalWeakReference,
            // so it must be alive. If the CWR has become invalid,
            // something very strange has happened.
            return (T)_cwr!.Dependent!;
        }

        public Tree ResolvePendingShifts<T>()
            where T : class, IShiftable<T>
            => ShiftBy<T>(0);

        // Eagerly shift oneself, but lazily shift the children.
        // Resolve any pending shifts while we're at it.
        public Tree ShiftBy<T>(long amount)
            where T : class, IShiftable<T>
        {
            // if both shift and amount are 0 (not their sum), there's nothing to do.
            if (_shift == 0 && amount == 0)
            {
                return this;
            }

            ConditionalWeakReference? GetNewCwr()
            {
                var (target, dependent) = GetTargetAndDependent();
                if (target == null)
                {
                    return null;
                }

                var newDependent = ((T?)dependent)?.ShiftBy(_shift + amount);

                // DependentHandles are expensive,
                // don't allocate a new one if we don't need to
                return ReferenceEquals(dependent, newDependent)
                    ? _cwr
                    : new(target, newDependent);
            }

            return new(
                GetNewCwr(),
                _consumedRange.ShiftBy(_shift + amount),
                _lookaroundRange.ShiftBy(_shift + amount),
                _children.Select(t => t.LazyShiftBy(_shift + amount)).ToImmutableArray(),
                0
            );
        }

        // We could check whether _cwr is still valid here
        private Tree LazyShiftBy(long amount)
            => amount == 0
                ? this
                : new(_cwr, _consumedRange, _lookaroundRange, _children, _shift + amount);

        public Tree? Search<TToken, T>(long location, Parser<TToken, T> key)
        {
            // Adjust the location and use the non-shifted _consumedRange.
            // This saves us from calling ResolvePendingShifts (rebuilds
            // the tree) while searching.
            location -= _shift;
            if (!_consumedRange.Contains(location))
            {
                return null;
            }

            if (_consumedRange.Start == location && ReferenceEquals(GetTarget(), key))
            {
                return this;
            }

            return Search(_children, location, key);
        }

        private object? GetTarget()
        {
            var cwr = _cwr;
            if (cwr == null)
            {
                return null;
            }

            var target = cwr.Target;
            if (target == null)
            {
                _cwr = null;
                return null;
            }

            return target;
        }

        private (object?, object?) GetTargetAndDependent()
        {
            var cwr = _cwr;
            if (cwr == null)
            {
                return (null, null);
            }

            var (target, dependent) = cwr.TargetAndDependent;
            if (target == null)
            {
                _cwr = null;
                return (null, null);
            }

            return (target, dependent);
        }

        private void CheckNoPendingShifts()
        {
            if (_shift != 0)
            {
                throw new InvalidOperationException("Tried to read a table entry with pending shifts. Please report this as a bug in Pidgin");
            }
        }

        public static Tree? Search<TToken, T>(ImmutableArray<Tree> trees, long location, Parser<TToken, T> key)
            => trees
                .Select(t => t.Search(location, key))
                .FirstOrDefault(t => t != null);
    }

    public class Builder
    {
        private readonly Stack<ImmutableArray<Tree>.Builder> _stack = new();

        public Builder()
        {
            _stack.Push(ImmutableArray.CreateBuilder<Tree>());
        }

        // todo: come up with a better name than Start/End
        public void Start()
        {
            _stack.Push(ImmutableArray.CreateBuilder<Tree>());
        }

        public void Add(Tree subtree)
        {
            _stack.Peek().Add(subtree);
        }

        public void Discard()
        {
            _stack.Pop();
        }

        public void End<TToken, T>(Parser<TToken, T> parser, LocationRange consumedRange, LocationRange lookaroundRange, T result)
        {
            var children = _stack.Pop().ToImmutable();
            _stack.Peek().Add(new(new(parser, result), consumedRange, lookaroundRange, children, 0));
        }

        public CachedParseResultTable Build()
        {
            if (_stack.Count != 1)
            {
                throw new InvalidOperationException("Couldn't build the CachedParseResultTable. Please report this as a bug in Pidgin.");
            }

            var children = _stack.Pop().ToImmutable();
            return new CachedParseResultTable(children);
        }
    }
}
