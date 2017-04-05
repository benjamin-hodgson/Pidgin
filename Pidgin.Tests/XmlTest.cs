using Pidgin.Examples.Xml;
using Xunit;
using Attribute = Pidgin.Examples.Xml.Attribute;

namespace Pidgin.Tests
{
    public class XmlTest
    {

        [Fact]
        public void TestParseSelfClosingTagWithoutAttributes()
        {
            {
                var input = "<foo/>";
                var expected = new Tag("foo", new Attribute[] {}, null);

                var result = XmlParser.Parse(input);
                
                Assert.True(result.Success);
                Assert.Equal(expected, result.Value);
            }
        }

        [Fact]
        public void TestParseSelfClosingTagWithAttributes()
        {
            {
                var input = "<foo bar=\"baz\" wibble=\"wobble\"/>";
                var expected = new Tag("foo", new[] { new Attribute("bar", "baz"), new Attribute("wibble", "wobble") }, null);

                var result = XmlParser.Parse(input);
    
                Assert.True(result.Success);
                Assert.NotNull(result.Value);
                Assert.Equal(expected, result.Value);
            }
        }

        [Fact]
        public void TestParseTagWithNoContentAndNoAttributes()
        {
            {
                var input = "<foo> </foo>";
                var expected = new Tag("foo", new Attribute[]{}, new Tag[]{});

                var result = XmlParser.Parse(input);

                Assert.True(result.Success);
                Assert.NotNull(result.Value);
                Assert.Equal(expected, result.Value);
            }
        }

        [Fact]
        public void TestParseTagWithContentAndAttributes()
        {
            {
                var input = "<foo bar=\"baz\" wibble=\"wobble\"><bar></bar><baz/></foo>";
                var expected = new Tag(
                    "foo",
                    new[] { new Attribute("bar", "baz"), new Attribute("wibble", "wobble") },
                    new[] { new Tag("bar", new Attribute[]{}, new Tag[]{}), new Tag("baz", new Attribute[]{}, null) });
                
                var result = XmlParser.Parse(input);

                Assert.True(result.Success);
                Assert.NotNull(result.Value);
                Assert.Equal(expected, result.Value);
            }
        }

        [Fact]
        public void TestParseMismatchingTags()
        {
            {
                var input = "<foo></bar>";

                var result = XmlParser.Parse(input);

                Assert.False(result.Success);
            }
        }
    }
}