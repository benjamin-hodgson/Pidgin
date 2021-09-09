using System.Collections.Generic;
using Xunit;
using static Pidgin.Parser;

namespace Pidgin.Tests
{
    public class ApiSugarParserTests : ParserTestBase
    {
        [Fact]
        public void TestOperatorOverloads()
        {
            TokenParser<char> semicolumn = ';';
            Parser<char, IEnumerable<char>> terminator = semicolumn + Char('\r') + Char('\n');
            Assert.Equal(";\r\n", terminator.ParseOrThrow(";\r\n"));

            (!semicolumn).ParseOrThrow(","); // Assert no error

            Parser<char, char> expr = null!;
            //Parser<char, char> parenthesized = '(' > Rec(() => expr) < ')'; // Cause stack overflow in runtime
            Parser<char, char> parenthesized = Char('(') > Rec(() => expr) < ')';
            expr = Digit
                | parenthesized
                | Char('+');

            Assert.Equal('1', expr.ParseOrThrow("1"));
            Assert.Equal('1', expr.ParseOrThrow("(1)"));
            Assert.Equal('1', expr.ParseOrThrow("(((1)))"));
            Assert.Equal('+', expr.ParseOrThrow("(((+)))"));
        }
    }
}
