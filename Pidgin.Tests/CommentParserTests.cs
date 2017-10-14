using Pidgin.Comment;
using Xunit;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Tests
{
    public class CommentParserTests : ParserTestBase
    {
        [Fact]
        public void TestSkipLineComment()
        {
            var p = CommentParser.SkipLineComment(String("//")).Then(End());

            {
                var comment = "//\n";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
            {
                var comment = "//";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
            {
                var comment = "// here is a comment ending with an osx style newline\n";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
            {
                var comment = "// here is a comment ending with a windows style newline\r\n";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
            {
                var comment = "// here is a comment with a \r carriage return in the middle\r\n";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
            {
                var comment = "// here is a comment at the end of a file";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
        }

        [Fact]
        public void TestSkipBlockComment()
        {
            var p = CommentParser.SkipBlockComment(String("/*"), String("*/")).Then(End());

            {
                var comment = "/**/";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
            {
                var comment = "/* here is a block comment with \n newlines in */";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
        }

        [Fact]
        public void TestSkipNestedBlockComment()
        {
            var p = CommentParser.SkipNestedBlockComment(String("/*"), String("*/")).Then(End());
            
            {
                var comment = "/**/";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
            {
                var comment = "/*/**/*/";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
            {
                var comment = "/* here is a non-nested block comment with \n newlines in */";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
            {
                var comment = "/* here is a /* nested */ block comment with \n newlines in */";

                var result = p.Parse(comment);

                AssertSuccess(result, Unit.Value, true);
            }
        }
    }
}