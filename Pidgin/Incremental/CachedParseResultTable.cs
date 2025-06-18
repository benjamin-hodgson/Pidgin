using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pidgin.Incremental;

internal class CachedParseResultTable
{
    private readonly ImmutableArray<Tree> _children;

    public CachedParseResultTable(ImmutableArray<Tree> children)
    {
        _children = children;
    }

    public Tree? TryGetValue<TToken, T>(long oldLocation, Parser<TToken, T> key)
        where T : class, IShiftable<T>
    {
        var result = Tree.Search(_children, oldLocation, key);
        result?.ResolvePendingShifts<T>();
        return result;
    }

    public class Tree : IShiftable<Tree>
    {
        // Target: Parser<TToken, T>
        // Dependent: T
        private readonly ConditionalWeakReference _kvp;

        // Children of the tree should be nodes within the ConsumedRange
        private LocationRange _consumedRange;
        private LocationRange _lookaroundRange;
        private ImmutableArray<Tree> _children;
        private long _shift;

        public Tree(
            ConditionalWeakReference kvp,
            LocationRange consumedRange,
            LocationRange lookaroundRange,
            ImmutableArray<Tree> children,
            long shift
        )
        {
            _kvp = kvp;
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
            // so it must be alive. If _kvp has become invalid,
            // something very strange has happened.
            return (T)_kvp!.Dependent!;
        }

        // todo: thread safety?
        public void ResolvePendingShifts<T>()
            where T : class, IShiftable<T>
        {
            if (_shift == 0)
            {
                return;
            }

            if (_kvp != null)
            {
                _kvp.Dependent = ((T?)_kvp.Dependent)?.ShiftBy(_shift);
            }

            _consumedRange = _consumedRange.ShiftBy(_shift);
            _lookaroundRange = _lookaroundRange.ShiftBy(_shift);
            _children = _children.Select(t => t.ShiftBy(_shift)).ToImmutableArray();

            _shift = 0;
        }

        // Should it eagerly shift oneself but lazily shift one's children?
        // Right now, once a subtree has been found, we shift it and then
        // immediately call ResolvePendingShifts, kind of ugly
        public Tree ShiftBy(long amount)
            => amount == 0
                ? this
                : new(_kvp, _consumedRange, _lookaroundRange, _children, _shift + amount);

        public Tree? WithKey<TToken, T>(Parser<TToken, T> newKey)
        {
            var kvp = _kvp;
            if (kvp == null)
            {
                return null;
            }

            var (target, dependent) = kvp.TargetAndDependent;

            if (target == null)
            {
                return null;
            }

            if (ReferenceEquals(target, newKey))
            {
                return this;
            }

            return new(new(newKey, dependent), _consumedRange, _lookaroundRange, _children, _shift);
        }

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

            if (_consumedRange.Start == location && ReferenceEquals(_kvp?.Target, key))
            {
                return this;
            }

            return Search(_children, location, key);
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

        public void Add<TToken, T>(Parser<TToken, T> key, Tree subtree)
        {
            var newSubtree = subtree.WithKey(key);

            if (newSubtree != null)
            {
                _stack.Peek().Add(newSubtree);
            }
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
