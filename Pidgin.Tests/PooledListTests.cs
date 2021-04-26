using System;
using System.Buffers;
using Xunit;

namespace Pidgin.Tests
{
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
                var l = new PooledList<int>();
                Assert.Equal(-1, l.IndexOf(1));
            }
        }

        [Fact]
        public void TestInsert()
        {
            {
                var l = new PooledList<int>();
                l.Insert(0, -1);
                Assert.Equal(1, l.Count);
                Assert.Equal(-1, l[0]);
            }
            {
                var l = new PooledList<int> { 1, 2, 5 };
                l.Insert(0, -1);
                l.Insert(4, -2);
                Assert.Equal(new[] { -1, 1, 2, 5, -2 }, l.AsSpan().ToArray());
            }
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => {
                    var l = new PooledList<int>();
                    l.Insert(-1, 1);
                });
                Assert.Throws<ArgumentOutOfRangeException>(() => {
                    var l = new PooledList<int>();
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
                Assert.Equal(0, l.Count);
            }
            {
                var l = new PooledList<int> { 1, 2, 5 };
                l.RemoveAt(0);
                l.RemoveAt(1);
                Assert.Equal(1, l.Count);
                Assert.Equal(2, l[0]);
            }
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => {
                    var l = new PooledList<int>();
                    l.RemoveAt(-1);
                });
                Assert.Throws<ArgumentOutOfRangeException>(() => {
                    var l = new PooledList<int>();
                    l.RemoveAt(0);
                });
            }
        }

        [Fact]
        public void TerribleBugWhenAppendingString()
        {
            var expected = "longer than the capacity";
            var builder = new PooledList<char>(ArrayPool<char>.Shared, 1);
            builder.AddRange(expected.AsSpan());
            Assert.Equal(expected, new string(builder.AsSpan()));
        }
    }
}
