using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Pidgin
{
    internal static class Rope
    {
        public static Rope<T> Create<T>()
            => Rope<T>.Empty;
        public static Rope<T> Create<T>(T item)
            => Rope<T>.Create(ImmutableArray.Create(item));
        public static Rope<T> CreateRange<T>(IEnumerable<T> items)
            => Rope<T>.Create(items.ToImmutableArray());
    }
    internal abstract class Rope<T> : IEnumerable<T>, IEquatable<Rope<T>>, IComparable<Rope<T>>
    {
        public static Rope<T> Empty { get; } = Create(ImmutableArray<T>.Empty);
        public int Length { get; }

        protected Rope(int length)
        {
            Length = length;
        }

        public Rope<T> Concat(Rope<T> other)
        {
            var totalLength = this.Length + other.Length;
            
            if (this is Leaf t && other is Leaf o && totalLength < 64)
            {
                var builder = ImmutableArray.CreateBuilder<T>(totalLength);
                builder.AddRange(t.Value);
                builder.AddRange(o.Value);
                return new Leaf(builder.MoveToImmutable());
            }

            return new Branch(this, other, totalLength);
        }

        public static Rope<T> Create(ImmutableArray<T> input)
            => new Leaf(input);

        public virtual ImmutableArray<T> ToImmutableArray()
        {
            var builder = ImmutableArray.CreateBuilder<T>(Length);
            AddTo(builder);
            return builder.MoveToImmutable();
        }
        protected abstract void AddTo(ImmutableArray<T>.Builder builder);


        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            foreach (var x in this)
            {
                yield return x;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => this.AsEnumerable().GetEnumerator();


        public override bool Equals(object obj)
            => ReferenceEquals(this, obj)
            || (!ReferenceEquals(null, obj) && obj is Rope<T> r && this.Equals(r));

        public bool Equals(Rope<T> other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }


            var thisEnumerator = this.GetEnumerator();
            var otherEnumerator = other.GetEnumerator();

            while (thisEnumerator.MoveNext())
            {
                if (!otherEnumerator.MoveNext() || !EqualityComparer<T>.Default.Equals(thisEnumerator.Current, otherEnumerator.Current))
                {
                    return false;
                }
            }

            return !otherEnumerator.MoveNext();
        }

        public override int GetHashCode()
        {
            // https://stackoverflow.com/a/15176541/1523776
            unchecked
            {
                var hash1 = 5381;
                var hash2 = hash1;

                var enumerator = this.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    hash1 = ((hash1 << 5) + hash1) ^ enumerator.Current.GetHashCode();
                    if (!enumerator.MoveNext())
                    {
                        break;
                    } 
                    hash2 = ((hash2 << 5) + hash2) ^ enumerator.Current.GetHashCode();
                }
                return hash1 + (hash2 * 1566083941);
            }
        }

        public int CompareTo(Rope<T> other)
        {
            if (ReferenceEquals(null, other))
            {
                return 1;
            }
            if (ReferenceEquals(this, other))
            {
                return 0;
            }
            
            var thisEnumerator = this.GetEnumerator();
            var otherEnumerator = other.GetEnumerator();

            while (thisEnumerator.MoveNext())
            {
                if (!otherEnumerator.MoveNext())
                {
                    return 1;
                }
                var compare = Comparer<T>.Default.Compare(thisEnumerator.Current, otherEnumerator.Current);
                if (compare != 0)
                {
                    return compare;
                }
            }

            return otherEnumerator.MoveNext() ? -1 : 0;
        }

        private sealed class Branch : Rope<T>
        {
            public Rope<T> Left { get; }
            public Rope<T> Right { get; }
            
            public Branch(Rope<T> left, Rope<T> right, int totalLength) : base(totalLength)
            {
                if (left == null)
                {
                    throw new ArgumentNullException(nameof(left));
                }
                if (right == null)
                {
                    throw new ArgumentNullException(nameof(right));
                }
                Left = left;
                Right = right;
            }

            protected override void AddTo(ImmutableArray<T>.Builder builder)
            {
                Left.AddTo(builder);
                Right.AddTo(builder);
            }
        }

        private sealed class Leaf : Rope<T>
        {
            public ImmutableArray<T> Value { get; }

            public Leaf(ImmutableArray<T> value) : base(value.Length)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                Value = value;
            }

            public override ImmutableArray<T> ToImmutableArray() => Value;

            protected override void AddTo(ImmutableArray<T>.Builder builder)
            {
                builder.AddRange(Value);
            }
        }

        public struct Enumerator
        {
            private readonly Stack<Rope<T>> _rights;
            private ImmutableArray<T>.Enumerator _currentLeafEnumerator;

            internal Enumerator(Rope<T> rope)
            {
                _rights = new Stack<Rope<T>>();
                _currentLeafEnumerator = DescendToLeftmostLeaf(_rights, rope);
            }

            public T Current => _currentLeafEnumerator.Current;

            public bool MoveNext()
            {
                if (_currentLeafEnumerator.MoveNext())
                {
                    return true;
                }
                if (_rights.Count == 0)
                {
                    return false;
                }
                var branch = _rights.Pop();
                DescendToLeftmostLeaf(branch);
                return _currentLeafEnumerator.MoveNext();
            }


            private void DescendToLeftmostLeaf(Rope<T> rope)
            {
                _currentLeafEnumerator = DescendToLeftmostLeaf(_rights, rope);
            }

            private static ImmutableArray<T>.Enumerator DescendToLeftmostLeaf(Stack<Rope<T>> stack, Rope<T> rope)
            {
                var x = rope;
                while (x is Branch b)
                {
                    stack.Push(b.Right);
                    x = b.Left;
                }
                return ((Leaf)x).Value.GetEnumerator();
            }
        }
    }
}