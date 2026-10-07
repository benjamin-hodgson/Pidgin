using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;

using Xunit;

namespace Pidgin.Tests;

public class PooledListTests
{
    [Fact]
    public void TestIndexOf()
    {
        {
            var l = new PooledList<int> { 1, 2, 5 };
            Assert.Equal(0, l.IndexOf(1));
            Assert.Equal(2, l.IndexOf(5));
            Assert.Equal(-1, l.IndexOf(8));
        }

        {
            var l = default(PooledList<int>);
            Assert.Equal(-1, l.IndexOf(1));
        }
    }

    [Fact]
    public void TestInsert()
    {
        {
            var l = default(PooledList<int>);
            l.Insert(0, -1);
            var x = Assert.Single(l);
            Assert.Equal(-1, x);
        }

        {
            var l = new PooledList<int> { 1, 2, 5 };
            l.Insert(0, -1);
            l.Insert(4, -2);
            Assert.Equal(new[] { -1, 1, 2, 5, -2 }, l.AsSpan().ToArray());
        }

        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var l = default(PooledList<int>);
                l.Insert(-1, 1);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var l = default(PooledList<int>);
                l.Insert(1, 1);
            });
        }
    }

    [Fact]
    public void TestRemoveAt()
    {
        {
            var l = new PooledList<int> { 1 };
            l.RemoveAt(0);
            Assert.Empty(l);
        }

        {
            var l = new PooledList<int> { 1, 2, 5 };
            l.RemoveAt(0);
            l.RemoveAt(1);
            var x = Assert.Single(l);
            Assert.Equal(2, x);
        }

        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var l = default(PooledList<int>);
                l.RemoveAt(-1);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var l = default(PooledList<int>);
                l.RemoveAt(0);
            });
        }
    }

    [Fact]
    public void TestBugInAddRange()
    {
        var expected = $"longer than the initial capacity, which is {PooledList<char>.InitialCapacity}";

        // fail if I make InitialCapacity larger without updating test
        Assert.True(expected.Length > PooledList<char>.InitialCapacity);

        var builder = default(PooledList<char>);
        builder.AddRange(expected.AsSpan());
        Assert.Equal(expected, new string(builder.AsSpan()));
    }

    [Fact]
    public void TestAddRangeEmptyDoesNotRent()
    {
        var l = new PooledList<int>(new ThrowingArrayPool<int>());
        l.AddRange(ReadOnlySpan<int>.Empty);
        l.AddRange(ImmutableArray<int>.Empty);
        l.AddRange(new List<int>());
        l.AddRange(Array.Empty<int>());
        Assert.Empty(l);
        l.Dispose();
    }

    private sealed class ThrowingArrayPool<T> : ArrayPool<T>
    {
        public override T[] Rent(int minimumLength)
            => throw new InvalidOperationException("Rent should not be called");

        public override void Return(T[] array, bool clearArray = false)
            => throw new InvalidOperationException("Return should not be called");
    }
}
