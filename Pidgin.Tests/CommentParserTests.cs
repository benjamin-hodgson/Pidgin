using Pidgin.Comment;
using Xunit;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Tests;

public class CommentParserTests : ParserTestBase
{
    [Theory]
    [InlineData("//\n")]
    [InlineData("//")]
    [InlineData("// here is a comment ending with an osx style newline\n")]
    [InlineData("// here is a comment ending with a windows style newline\r\n")]
    [InlineData("// here is a comment with a \r carriage return in the middle\r\n")]
    [InlineData("// here is a comment at the end of a file")]
    public void TestSkipLineComment(string comment)
    {
        TestCommentParser(
            CommentParser.SkipLineComment(String("//")).Then(End),
            comment
        );
    }

    [Theory]
    [InlineData("/**/")]
    [InlineData("/* here is a block comment with \n newlines in */")]
    public void TestSkipBlockComment(string comment)
    {
        TestCommentParser(
            CommentParser.SkipBlockComment(String("/*"), String("*/")).Then(End),
            comment
        );
    }

    [Theory]
    [InlineData("/**/")]
    [InlineData("/*/**/*/")]
    [InlineData("/* here is a non-nested block comment with \n newlines in */")]
    [InlineData("/* here is a /* nested */ block comment with \n newlines in */")]
    public void TestSkipNestedBlockComment(string comment)
    {
        TestCommentParser(
            CommentParser.SkipNestedBlockComment(String("/*"), String("*/")).Then(End),
            comment
        );
    }

    private static void TestCommentParser(Parser<char, Unit> parser, string comment)
    {
        AssertFullParse(parser, comment, Unit.Value);
    }
}
