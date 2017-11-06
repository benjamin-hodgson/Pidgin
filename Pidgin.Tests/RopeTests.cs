using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Pidgin.Tests
{
    public class RopeTests
    {
        [Fact]
        public void TestLength()
        {
            {
                var emptyRope = Rope.Create<char>();
                Assert.Equal(0, emptyRope.Length);
            }
            {
                var singletonRope = Rope.Create('a');
                Assert.Equal(1, singletonRope.Length);
            }
            {
                var shortRope = Rope.CreateRange("abcd");
                Assert.Equal(4, shortRope.Length);
            }
            {
                var shortConcatRope = Rope.CreateRange("ab").Concat(Rope.CreateRange("cd"));
                Assert.Equal(4, shortConcatRope.Length);
            }
            {
                var longConcatRope = Rope.CreateRange(Enumerable.Repeat('a', 500)).Concat(Rope.CreateRange(Enumerable.Repeat('a', 500)));
                Assert.Equal(1000, longConcatRope.Length);
            }
            {
                var manyConcatRope = Enumerable.Repeat(Rope.Create('a'), 3).Aggregate((x, y) => x.Concat(y));
                Assert.Equal(3, manyConcatRope.Length);
            }
            {
                var longManyConcatRope = Enumerable.Repeat(Rope.Create('a'), 1000).Aggregate((x, y) => x.Concat(y));
                Assert.Equal(1000, longManyConcatRope.Length);
            }
        }

        [Fact]
        public void TestToImmutableArray()
        {
            {
                var emptyRope = Rope.Create<char>();
                Assert.Equal(ImmutableArray.Create<char>(), emptyRope.ToImmutableArray());
            }
            {
                var singletonRope = Rope.Create('a');
                Assert.Equal(new[] { 'a' }, singletonRope.ToImmutableArray());
            }
            {
                var shortRope = Rope.CreateRange("abcd");
                Assert.Equal("abcd", shortRope.ToImmutableArray());
            }
            {
                var shortConcatRope = Rope.CreateRange("ab").Concat(Rope.CreateRange("cd"));
                Assert.Equal("abcd", shortConcatRope.ToImmutableArray());
            }
            {
                var longConcatRope = Rope.CreateRange(Enumerable.Repeat('a', 500)).Concat(Rope.CreateRange(Enumerable.Repeat('a', 500)));
                Assert.Equal(Enumerable.Repeat('a', 1000), longConcatRope.ToImmutableArray());
            }
            {
                var manyConcatRope = Enumerable.Repeat(Rope.Create('a'), 3).Aggregate((x, y) => x.Concat(y));
                Assert.Equal("aaa", manyConcatRope.ToImmutableArray());
            }
            {
                var longManyConcatRope = Enumerable.Repeat(Rope.Create('a'), 1000).Aggregate((x, y) => x.Concat(y));
                Assert.Equal(Enumerable.Repeat('a', 1000), longManyConcatRope.ToImmutableArray());
            }
        }

        [Fact]
        public void TestRopeAsEnumerableEqualsSelfToImmutableArray()
        {
            void ShouldEqualSelf<T>(Rope<T> item) => Assert.Equal(item.ToImmutableArray(), item.AsEnumerable());
            
            {
                var emptyRope = Rope.Create<char>();
                ShouldEqualSelf(emptyRope);
            }
            {
                var singletonRope = Rope.Create('a');
                ShouldEqualSelf(singletonRope);
            }
            {
                var shortRope = Rope.CreateRange("abcd");
                ShouldEqualSelf(shortRope);
            }
            {
                var shortConcatRope = Rope.CreateRange("ab").Concat(Rope.CreateRange("cd"));
                ShouldEqualSelf(shortConcatRope);
            }
            {
                var longConcatRope = Rope.CreateRange(Enumerable.Repeat('a', 500)).Concat(Rope.CreateRange(Enumerable.Repeat('a', 500)));
                ShouldEqualSelf(longConcatRope);
            }
            {
                var manyConcatRope = Enumerable.Repeat(Rope.Create('a'), 3).Aggregate((x, y) => x.Concat(y));
                ShouldEqualSelf(manyConcatRope);
            }
            {
                var longManyConcatRope = Enumerable.Repeat(Rope.Create('a'), 1000).Aggregate((x, y) => x.Concat(y));
                ShouldEqualSelf(longManyConcatRope);
            }
        }

        [Fact]
        public void TestGetHashCode()
        {
            {
                var emptyRope = Rope.Create<char>();
                Assert.Equal(371857150, emptyRope.GetHashCode());
            }
            {
                var singletonRope = Rope.Create('a');
                Assert.Equal(378386365, singletonRope.GetHashCode());
            }
            {
                var shortRope = Rope.CreateRange("abcd");
                Assert.Equal(1200620054, shortRope.GetHashCode());
            }
            {
                var shortConcatRope = Rope.CreateRange("ab").Concat(Rope.CreateRange("cd"));
                Assert.Equal(1200620054, shortConcatRope.GetHashCode());
            }
            {
                var longConcatRope = Rope.CreateRange(Enumerable.Repeat('a', 500)).Concat(Rope.CreateRange(Enumerable.Repeat('a', 500)));
                Assert.Equal(1129875070, longConcatRope.GetHashCode());
            }
            {
                var manyConcatRope = Enumerable.Repeat(Rope.Create('a'), 3).Aggregate((x, y) => x.Concat(y));
                Assert.Equal(119771257, manyConcatRope.GetHashCode());
            }
            {
                var longManyConcatRope = Enumerable.Repeat(Rope.Create('a'), 1000).Aggregate((x, y) => x.Concat(y));
                Assert.Equal(1129875070, longManyConcatRope.GetHashCode());
            }
        }
    }
}