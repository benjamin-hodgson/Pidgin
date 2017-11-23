using Xunit;
using Pidgin;
using Pidgin.Permutation;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using System.Linq;

namespace Pidgin.Tests
{
    public class PermutationParserTests
    {
        [Fact]
        public void TestSimplePermutation()
        {
            var parser = PermutationParser
                .Map(
                    (a, b, c) => string.Concat(a, b, c),
                    Permutable.Create(Char('a')),
                    Permutable.Create(Char('b')),
                    Permutable.Create(Char('c'))
                );

            var results = new[] { "abc", "bac", "bca", "cba" }.Select(x => parser.ParseOrThrow(x));

            Assert.All(results, x => Assert.Equal("abc", x));
        }

        [Fact]
        public void TestOptionalPermutation()
        {
            var parser = PermutationParser
                .Map(
                    (a, b, c) => string.Concat(a, b, c),
                    Permutable.Create(Char('a')),
                    Permutable.CreateOptional(Char('b'), '_'),
                    Permutable.Create(Char('c'))
                );

            var results1 = new[] { "abc", "bac", "bca", "cba" }.Select(x => parser.ParseOrThrow(x));
            Assert.All(results1, x => Assert.Equal("abc", x));

            var results2 = new[] { "ac", "ca" }.Select(x => parser.ParseOrThrow(x));
            Assert.All(results2, x => Assert.Equal("a_c", x));
        }
    }
}