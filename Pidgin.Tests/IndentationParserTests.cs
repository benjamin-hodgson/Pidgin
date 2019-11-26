using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using static Pidgin.Indentation.IndentationParser;
using Pidgin.Indentation;
using System.Collections.Immutable;
using Xunit;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pidgin.Tests
{
    public class IndentationParserTests : ParserTestBase
    {
        private static Parser<char, string> _noIndent = Try(String("noIndent")).Before(SkipWhitespaces);
        private static Parser<char, string> _indent = Try(String("indent")).Before(SkipWhitespaces);
        private static Parser<char, string> _optionalIndent = Try(String("optionalIndent")).Before(SkipWhitespaces);

        private static Parser<char, Tree> _item =
            OneOf(
                _noIndent.Select(name => new Tree(name, ImmutableArray<Tree>.Empty)),
                Block(
                    _indent,
                    IndentationMode.IndentedAtLeastOnce(Rec(() => _item)),
                    (header, body) => new Tree(header, body.ToImmutableArray())
                ),
                Block(
                    _optionalIndent,
                    IndentationMode.Indented(Rec(() => _item)),
                    (header, body) => new Tree(header, body.ToImmutableArray())
                )
            );
        private static Parser<char, Tree> _file
            = SkipWhitespaces
                .Then(TopLevel(_item).Many())
                .Select(xs => new Tree("top", xs.ToImmutableArray()));
        

        [Fact]
        public void TestNoTopLevelItems()
        {
            var input = @"
";

            AssertSuccess(_file.Parse(input), new Tree("top"), true);
        }

        [Fact]
        public void TestSingleTopLevelItem()
        {
            var input = @"
noIndent
";

            AssertSuccess(_file.Parse(input), new Tree("top", new Tree("noIndent")), true);
        }

        [Fact]
        public void TestMultipleTopLevelItems()
        {
            var input = @"
noIndent
noIndent

noIndent
";

            AssertSuccess(_file.Parse(input), new Tree("top", new Tree("noIndent"), new Tree("noIndent"), new Tree("noIndent")), true);
        }



        private class Tree : IEquatable<Tree>
        {
            public string Name { get; }
            public ImmutableArray<Tree> Children { get; }

            public Tree(string name, ImmutableArray<Tree> children)
            {
                Name = name;
                Children = children;
            }

            public Tree(string name, params Tree[] children) : this(name, children.ToImmutableArray())
            {
            }

            public override bool Equals(object? obj)
                => obj is Tree tree && this.Equals(tree);

            public override int GetHashCode()
            {
                var hc = new HashCode();
                hc.Add(Name);
                foreach (var child in Children)
                {
                    hc.Add(child);
                }
                return hc.ToHashCode();
            }

            public bool Equals([AllowNull] Tree other)
                => !ReferenceEquals(other, null)
                && Name == other.Name
                && Children.SequenceEqual(other.Children);
        }
    }
}