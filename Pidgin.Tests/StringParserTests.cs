using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Xunit;

using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Tests;

#pragma warning disable CA1861  // Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array

public class StringParserTests : ParserTestBase
{
    [Fact]
    public void TestReturn()
    {
        {
            var parser = Return('a');
            AssertPartialParse(parser, "", 'a', 0);
            AssertPartialParse(parser, "foobar", 'a', 0);
        }

        {
            var parser = FromResult('a');
            AssertPartialParse(parser, "", 'a', 0);
            AssertPartialParse(parser, "foobar", 'a', 0);
        }
    }

    [Fact]
    public void TestFail()
    {
        {
            var parser = Fail<Unit>("message");
            var expectedError = new ParseError<char>(
                Maybe.Nothing<char>(),
                false,
                [new Expected<char>(ImmutableArray.Create<char>())],
                0,
                SourcePosDelta.Zero,
                "message"
            );
            AssertFailure(parser, "", expectedError);
            AssertFailure(parser, "foobar", expectedError);

            var ex = Assert.Throws<ParseException<char>>(() => parser.ParseOrThrow(""));
            Assert.Equal(expectedError, ex.Error);
        }
    }

    [Fact]
    public void TestToken()
    {
        {
            var parser = Char('a');
            AssertPartialParse(parser, "a", 'a', 1);
            AssertPartialParse(parser, "ab", 'a', 1);
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create('a'))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "b",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.Create('a'))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = AnyCharExcept('a', 'b', 'c');
            AssertPartialParse(parser, "e", 'e', 1);
            AssertFailure(
                parser,
                "b",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = AnyCharExcept('a');
            AssertPartialParse(parser, "e", 'e', 1);
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Just('a'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = Token('a'.Equals);
            AssertPartialParse(parser, "a", 'a', 1);
            AssertPartialParse(parser, "ab", 'a', 1);
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    ImmutableArray<Expected<char>>.Empty,
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "b",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = Any;
            AssertPartialParse(parser, "a", 'a', 1);
            AssertPartialParse(parser, "b", 'b', 1);
            AssertPartialParse(parser, "ab", 'a', 1);
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("any character")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = Whitespace;
            AssertPartialParse(parser, "\r", '\r', 1);
            AssertPartialParse(parser, "\n", '\n', 1);
            AssertPartialParse(parser, "\t", '\t', 1);
            AssertPartialParse(parser, " ", ' ', 1);
            AssertPartialParse(parser, " abc", ' ', 1);
            AssertFailure(
                parser,
                "abc",
                new ParseError<char>(
                    Maybe.Just('a'),
                    false,
                    [new Expected<char>("whitespace")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("whitespace")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }
    }

    [Fact]
    public void TestCIChar()
    {
        {
            var parser = CIChar('a');
            AssertPartialParse(parser, "a", 'a', 1);
            AssertPartialParse(parser, "ab", 'a', 1);
            AssertPartialParse(parser, "A", 'A', 1);
            AssertPartialParse(parser, "AB", 'A', 1);
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create('A')), new Expected<char>(ImmutableArray.Create('a'))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "b",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.Create('A')), new Expected<char>(ImmutableArray.Create('a'))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }
    }

    [Fact]
    public void TestEnd()
    {
        {
            var parser = End;
            AssertPartialParse(parser, "", Unit.Value, 0);
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Just('a'),
                    false,
                    [default(Expected<char>)],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }
    }

    [Fact]
    public void TestNumber()
    {
        {
            var parser = Num;
            AssertFullParse(parser, "0", 0);
            AssertFullParse(parser, "+0", +0);
            AssertFullParse(parser, "-0", -0);
            AssertFullParse(parser, "1", 1);
            AssertFullParse(parser, "+1", +1);
            AssertFullParse(parser, "-1", -1);
            AssertFullParse(parser, "12345", 12345);
            AssertPartialParse(parser, "1a", 1, 1);
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("number")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Just('a'),
                    false,
                    [new Expected<char>("number")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "+",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("number")],
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
            AssertFailure(
                parser,
                "-",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("number")],
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
        }

        {
            var parser = HexNum;
            AssertFullParse(parser, "09", 0x09);
            AssertFullParse(parser, "ab", 0xab);
            AssertFullParse(parser, "cd", 0xcd);
            AssertFullParse(parser, "ef", 0xef);
            AssertFullParse(parser, "AB", 0xAB);
            AssertFullParse(parser, "CD", 0xCD);
            AssertFullParse(parser, "EF", 0xEF);
            AssertFailure(
                parser,
                "g",
                new ParseError<char>(
                    Maybe.Just('g'),
                    false,
                    [new Expected<char>("hexadecimal number")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = OctalNum;
            AssertFullParse(parser, "7", 7);
            AssertFailure(
                parser,
                "8",
                new ParseError<char>(
                    Maybe.Just('8'),
                    false,
                    [new Expected<char>("octal number")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = LongNum;
            AssertFullParse(parser, "0", 0L);
            AssertFullParse(parser, "+0", +0L);
            AssertFullParse(parser, "-0", -0L);
            AssertFullParse(parser, "1", 1L);
            AssertFullParse(parser, "+1", +1L);
            AssertFullParse(parser, "-1", -1L);
            AssertFullParse(parser, "12345", 12345L);
            var tooBigForInt = ((long)int.MaxValue) + 1;
            AssertFullParse(parser, tooBigForInt.ToString(null as IFormatProvider), tooBigForInt);
            AssertPartialParse(parser, "1a", 1, 1);
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("number")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Just('a'),
                    false,
                    [new Expected<char>("number")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "+",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("number")],
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
            AssertFailure(
                parser,
                "-",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("number")],
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
        }

        {
            var parser = Real;
            AssertFullParse(parser, "0", 0d);
            AssertFullParse(parser, "+0", +0d);
            AssertFullParse(parser, "-0", -0d);
            AssertFullParse(parser, "1", 1d);
            AssertFullParse(parser, "+1", +1d);
            AssertFullParse(parser, "-1", -1d);

            AssertFullParse(parser, "12345", 12345d);
            AssertFullParse(parser, "+12345", +12345d);
            AssertFullParse(parser, "-12345", -12345d);

            AssertFullParse(parser, "12.345", 12.345d);
            AssertFullParse(parser, "+12.345", +12.345d);
            AssertFullParse(parser, "-12.345", -12.345d);

            AssertFullParse(parser, ".12345", .12345d);
            AssertFullParse(parser, "+.12345", +.12345d);
            AssertFullParse(parser, "-.12345", -.12345d);

            AssertFullParse(parser, "12345e10", 12345e10d);
            AssertFullParse(parser, "+12345e10", +12345e10d);
            AssertFullParse(parser, "-12345e10", -12345e10d);
            AssertFullParse(parser, "12345e+10", 12345e+10d);
            AssertFullParse(parser, "+12345e+10", +12345e+10d);
            AssertFullParse(parser, "-12345e+10", -12345e+10d);
            AssertFullParse(parser, "12345e-10", 12345e-10d);
            AssertFullParse(parser, "+12345e-10", +12345e-10d);
            AssertFullParse(parser, "-12345e-10", -12345e-10d);

            AssertFullParse(parser, "12.345e10", 12.345e10d);
            AssertFullParse(parser, "+12.345e10", +12.345e10d);
            AssertFullParse(parser, "-12.345e10", -12.345e10d);
            AssertFullParse(parser, "12.345e+10", 12.345e+10d);
            AssertFullParse(parser, "+12.345e+10", +12.345e+10d);
            AssertFullParse(parser, "-12.345e+10", -12.345e+10d);
            AssertFullParse(parser, "12.345e-10", 12.345e-10d);
            AssertFullParse(parser, "+12.345e-10", +12.345e-10d);
            AssertFullParse(parser, "-12.345e-10", -12.345e-10d);

            AssertFullParse(parser, ".12345e10", .12345e10d);
            AssertFullParse(parser, "+.12345e10", +.12345e10d);
            AssertFullParse(parser, "-.12345e10", -.12345e10d);
            AssertFullParse(parser, ".12345e+10", .12345e+10d);
            AssertFullParse(parser, "+.12345e+10", +.12345e+10d);
            AssertFullParse(parser, "-.12345e+10", -.12345e+10d);
            AssertFullParse(parser, ".12345e-10", .12345e-10d);
            AssertFullParse(parser, "+.12345e-10", +.12345e-10d);
            AssertFullParse(parser, "-.12345e-10", -.12345e-10d);

            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("real number")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Just('a'),
                    false,
                    [new Expected<char>("real number")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "+",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("real number")],
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
            AssertFailure(
                parser,
                "-",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("real number")],
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
            AssertFailure(
                parser,
                "12345.",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("real number")],
                    6,
                    new SourcePosDelta(0, 6),
                    null
                )
            );
            AssertFailure(
                parser,
                "12345e",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("real number")],
                    6,
                    new SourcePosDelta(0, 6),
                    null
                )
            );
            AssertFailure(
                parser,
                "12345e+",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>("real number")],
                    7,
                    new SourcePosDelta(0, 7),
                    null
                )
            );
            AssertFailure(
                parser,
                "12345.e",
                new ParseError<char>(
                    Maybe.Just('e'),
                    false,
                    [new Expected<char>("real number")],
                    6,
                    new SourcePosDelta(0, 6),
                    null
                )
            );
        }
    }

    [Fact]
    [UseCulture("nb-NO")]
    public void TestRealParserWithDifferentCultureInfo()
    {
        var parser = Real;
        AssertFullParse(parser, "12.345", 12.345d);
    }

    [Fact]
    public void TestSequence()
    {
        {
            var parser = String("foo");
            AssertFullParse(parser, "foo", "foo");
            AssertPartialParse(parser, "food", "foo", 3);
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = Sequence(Char('f'), Char('o'), Char('o'));
            AssertFullParse(parser, "foo", "foo".ToArray());
            AssertPartialParse(parser, "food", "foo".ToArray(), 3);
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("f"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("o"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("f"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }
    }

    [Fact]
    public void TestCIString()
    {
        {
            var parser = CIString("foo");
            AssertFullParse(parser, "foo", "foo");
            AssertPartialParse(parser, "food", "foo", 3);
            AssertFullParse(parser, "FOO", "FOO");
            AssertPartialParse(parser, "FOOD", "FOO", 3);
            AssertFullParse(parser, "fOo", "fOo");
            AssertPartialParse(parser, "Food", "Foo", 3);
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "FOul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }
    }

    [Fact]
    public void TestBind()
    {
        {
            // any two equal characters
            var parser = Any.Then(c => Token(c.Equals));
            AssertFullParse(parser, "aa", 'a');
            AssertFailure(
                parser,
                "ab",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
        }

        {
            var parser = Any.Bind(c => Token(c.Equals), (x, y) => new { x, y });
            AssertFullParse(parser, "aa", new { x = 'a', y = 'a' });
            AssertFailure(
                parser,
                "ab",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
        }

        {
            var parser = Any.Then(c => Token(c.Equals), (x, y) => new { x, y });
            AssertFullParse(parser, "aa", new { x = 'a', y = 'a' });
            AssertFailure(
                parser,
                "ab",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
        }

        {
            var parser =
                from x in Any
                from y in Token(x.Equals)
                select new { x, y };
            AssertFullParse(parser, "aa", new { x = 'a', y = 'a' });
            AssertFailure(
                parser,
                "ab",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
        }

        {
            var parser = Char('x').Then(c => Char('y'));
            AssertFullParse(parser, "xy", 'y');
            AssertFailure(
                parser,
                "yy",
                new ParseError<char>(
                    Maybe.Just('y'),
                    false,
                    [new Expected<char>(ImmutableArray.Create('x'))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "xx",
                new ParseError<char>(
                    Maybe.Just('x'),
                    false,
                    [new Expected<char>(ImmutableArray.Create('y'))],
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
        }
    }

    [Fact]
    public void TestThen()
    {
        {
            var parser = Char('a').Then(Char('b'));
            AssertFullParse(parser, "ab", 'b');
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("b"))],
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("a"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = Char('a').Then(Char('b'), (a, b) => new { a, b });
            AssertFullParse(parser, "ab", new { a = 'a', b = 'b' });
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("b"))],
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("a"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = Char('a').Before(Char('b'));
            AssertFullParse(parser, "ab", 'a');
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("b"))],
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("a"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }
    }

    [Fact]
    public void TestMap()
    {
        {
            var parser = Map((x, y, z) => new { x, y, z }, Char('a'), Char('b'), Char('c'));
            AssertFullParse(parser, "abc", new { x = 'a', y = 'b', z = 'c' });
            AssertFailure(
                parser,
                "abd",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("c"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }

        {
            var parser = Char('a').Select(a => new { a });
            AssertFullParse(parser, "a", new { a = 'a' });
        }

        {
            var parser = Char('a').Map(a => new { a });
            AssertFullParse(parser, "a", new { a = 'a' });
        }

        {
            var parser =
                from a in Char('a')
                select new { a };
            AssertFullParse(parser, "a", new { a = 'a' });
        }
    }

    [Fact]
    public void TestOr()
    {
        {
            var parser = Fail<char>("test").Or(Char('a'));
            AssertFullParse(parser, "a", 'a');
            AssertFailure(
                parser,
                "b",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    false,
                    [new Expected<char>(ImmutableArray.Create<char>()), new Expected<char>(ImmutableArray.Create('a'))],
                    0,
                    SourcePosDelta.Zero,
                    "test"
                )
            );
        }

        {
            var parser = Char('a').Or(Char('b'));
            AssertFullParse(parser, "a", 'a');
            AssertFullParse(parser, "b", 'b');
            AssertFailure(
                parser,
                "c",
                new ParseError<char>(
                    Maybe.Just('c'),
                    false,
                    [new Expected<char>(ImmutableArray.Create('a')), new Expected<char>(ImmutableArray.Create('b'))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = String("foo").Or(String("bar"));
            AssertFullParse(parser, "foo", "foo");
            AssertFullParse(parser, "bar", "bar");
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }

        {
            var parser = String("foo").Or(String("foul"));

            // because the first parser consumed input
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }

        {
            var parser = Try(String("foo")).Or(String("foul"));
            AssertFullParse(parser, "foul", "foul");
        }
    }

    [Fact]
    public void TestOneOf()
    {
        {
            var parser = OneOf(Char('a'), Char('b'), Char('c'));
            AssertFullParse(parser, "a", 'a');
            AssertFullParse(parser, "b", 'b');
            AssertFullParse(parser, "c", 'c');
            AssertFailure(
                parser,
                "d",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [
                        new Expected<char>(ImmutableArray.Create('a')),
                        new Expected<char>(ImmutableArray.Create('b')),
                        new Expected<char>(ImmutableArray.Create('c')),
                    ],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = OneOf("abc");
            AssertFullParse(parser, "a", 'a');
            AssertFullParse(parser, "b", 'b');
            AssertFullParse(parser, "c", 'c');
            AssertFailure(
                parser,
                "d",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [
                        new Expected<char>(ImmutableArray.Create('a')),
                        new Expected<char>(ImmutableArray.Create('b')),
                        new Expected<char>(ImmutableArray.Create('c')),
                    ],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = OneOf(String("foo"), String("bar"));
            AssertFullParse(parser, "foo", "foo");
            AssertFullParse(parser, "bar", "bar");
            AssertFailure(
                parser,
                "quux",
                new ParseError<char>(
                    Maybe.Just('q'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo")), new Expected<char>(ImmutableArray.CreateRange("bar"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestCIOneOf()
    {
        {
            var parser = CIOneOf('a', 'b', 'c');
            AssertFullParse(parser, "a", 'a');
            AssertFullParse(parser, "b", 'b');
            AssertFullParse(parser, "c", 'c');
            AssertFullParse(parser, "A", 'A');
            AssertFullParse(parser, "B", 'B');
            AssertFullParse(parser, "C", 'C');
            AssertFailure(
                parser,
                "d",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [
                        new Expected<char>(ImmutableArray.Create('a')),
                        new Expected<char>(ImmutableArray.Create('A')),
                        new Expected<char>(ImmutableArray.Create('b')),
                        new Expected<char>(ImmutableArray.Create('B')),
                        new Expected<char>(ImmutableArray.Create('c')),
                        new Expected<char>(ImmutableArray.Create('C'))
                    ],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = CIOneOf("abc");
            AssertFullParse(parser, "a", 'a');
            AssertFullParse(parser, "b", 'b');
            AssertFullParse(parser, "c", 'c');
            AssertFullParse(parser, "A", 'A');
            AssertFullParse(parser, "B", 'B');
            AssertFullParse(parser, "C", 'C');
            AssertFailure(
                parser,
                "d",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [
                        new Expected<char>(ImmutableArray.Create('a')),
                        new Expected<char>(ImmutableArray.Create('A')),
                        new Expected<char>(ImmutableArray.Create('b')),
                        new Expected<char>(ImmutableArray.Create('B')),
                        new Expected<char>(ImmutableArray.Create('c')),
                        new Expected<char>(ImmutableArray.Create('C'))
                    ],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }
    }

    [Fact]
    public void TestNot()
    {
        {
            var parser = Not(String("food")).Then(String("bar"));
            AssertFullParse(parser, "foobar", "bar");
        }

        {
            var parser = Not(OneOf(Char('a'), Char('b'), Char('c')));
            AssertPartialParse(parser, "e", Unit.Value, 0);
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Just('a'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            var parser = Not(Return('f'));
            AssertFailure(
                parser,
                "foobar",
                new ParseError<char>(
                    Maybe.Just('f'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            // test to make sure it doesn't throw out the buffer, for the purposes of computing error position
            var str = new string('a', 10000);
            var parser = Not(String(str));
            AssertFailure(
                parser,
                str,
                new ParseError<char>(
                    Maybe.Just('a'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }

        {
            // test error pos calculation
            var parser = Char('a').Then(Not(Char('b')));
            AssertFailure(
                parser,
                "ab",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    ImmutableArray<Expected<char>>.Empty,
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
        }
    }

    [Fact]
    public void TestLookahead()
    {
        {
            var parser = Lookahead(String("foo"));
            AssertPartialParse(parser, "foo", "foo", 0);
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foe",
                new ParseError<char>(
                    Maybe.Just('e'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }

        {
            // should backtrack on success
            var parser = Lookahead(String("foo")).Then(String("food"));
            AssertFullParse(parser, "food", "food");
        }
    }

    [Fact]
    public void TestRecoverWith()
    {
        {
            var parser = String("foo").ThenReturn((ParseError<char>?)null)
                .RecoverWith(err => String("bar").ThenReturn(err)!);

            AssertFullParse(
                parser,
                "fobar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }

        {
            var parser = String("nabble").ThenReturn((ParseError<char>?)null)
                .Or(
                    String("foo").ThenReturn((ParseError<char>?)null)
                        .RecoverWith(err => String("bar").ThenReturn(err)!)
                );

            // shouldn't get the expected from nabble
            AssertFullParse(
                parser,
                "fobar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestTryUsingStaticExample()
    {
        {
            static string MkString(char first, IEnumerable<char> rest)
            {
                var sb = new StringBuilder();
                sb.Append(first);
                sb.Append(string.Concat(rest));
                return sb.ToString();
            }

            var pUsing = String("using");
            var pStatic = String("static");
            var identifier = Token(char.IsLetter)
                .Then(Token(char.IsLetterOrDigit).Many(), MkString)
                .Labelled("identifier");
            var usingStatic =
                from kws in Try(
                    from u in pUsing.Before(Whitespace.AtLeastOnce())
                    from s in pStatic.Before(Whitespace.AtLeastOnce())
                    select new { }
                )
                from id in identifier
                select new { isStatic = true, id };
            var notStatic =
                from u in pUsing
                from ws in Whitespace.AtLeastOnce()
                from id in identifier
                select new { isStatic = false, id };
            var parser = usingStatic.Or(notStatic);

            AssertFullParse(parser, "using static Console", new { isStatic = true, id = "Console" });
            AssertFullParse(parser, "using System", new { isStatic = false, id = "System" });
            AssertFailure(
                parser,
                "usine",
                new ParseError<char>(
                    Maybe.Just('e'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("using"))],
                    4,
                    new SourcePosDelta(0, 4),
                    null
                )
            );
            AssertFailure(
                parser,
                "using 123",
                new ParseError<char>(
                    Maybe.Just('1'),
                    false,
                    [new Expected<char>("identifier")],
                    6,
                    new SourcePosDelta(0, 6),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestAssert()
    {
        {
            var parser = Char('a').Assert('a'.Equals);
            AssertFullParse(parser, "a", 'a');
        }

        {
            var parser = Char('a').Assert('b'.Equals);
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    false,
                    [new Expected<char>("result satisfying assertion")],
                    1,
                    SourcePosDelta.OneCol,
                    "Assertion failed"
                )
            );
        }

        {
            var parser = Char('a').Where('a'.Equals);
            AssertFullParse(parser, "a", 'a');
        }

        {
            var parser = Char('a').Where('b'.Equals);
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    false,
                    [new Expected<char>("result satisfying assertion")],
                    1,
                    SourcePosDelta.OneCol,
                    "Assertion failed"
                )
            );
        }
    }

    [Fact]
    public void TestMany()
    {
        {
            var parser = String("foo").Many();
            AssertPartialParse(parser, "", Enumerable.Empty<string>(), 0);
            AssertPartialParse(parser, "bar", Enumerable.Empty<string>(), 0);
            AssertFullParse(parser, "foo", new[] { "foo" });
            AssertFullParse(parser, "foofoo", new[] { "foo", "foo" });
            AssertPartialParse(parser, "food", new[] { "foo" }, 3);
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Whitespaces;
            AssertFullParse(parser, "    ", new[] { ' ', ' ', ' ', ' ' });
            AssertFullParse(parser, "\r\n", new[] { '\r', '\n' });
            AssertPartialParse(parser, " abc", new[] { ' ' }, 1);
            AssertPartialParse(parser, "abc", Enumerable.Empty<char>(), 0);
            AssertPartialParse(parser, "", Enumerable.Empty<char>(), 0);
        }

        {
            var parser = Return(1).Many();
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestManyString()
    {
        {
            var parser = Char('f').ManyString();
            AssertPartialParse(parser, "", "", 0);
            AssertPartialParse(parser, "bar", "", 0);
            AssertFullParse(parser, "f", "f");
            AssertFullParse(parser, "ff", "ff");
            AssertPartialParse(parser, "fo", "f", 1);
        }

        {
            var parser = String("f").ManyString();
            AssertPartialParse(parser, "", "", 0);
            AssertPartialParse(parser, "bar", "", 0);
            AssertFullParse(parser, "f", "f");
            AssertFullParse(parser, "ff", "ff");
            AssertPartialParse(parser, "fo", "f", 1);
        }

        {
            var parser = Return('f').ManyString();
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestSkipMany()
    {
        {
            var parser = String("foo").SkipMany();
            AssertPartialParse(parser, "", Unit.Value, 0);
            AssertPartialParse(parser, "bar", Unit.Value, 0);
            AssertFullParse(parser, "foo", Unit.Value);
            AssertFullParse(parser, "foofoo", Unit.Value);
            AssertPartialParse(parser, "food", Unit.Value, 3);
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = SkipWhitespaces.Then(Char('a'));
            AssertFullParse(parser, "    a", 'a');
            AssertFullParse(parser, " \r\n\ta", 'a');
            AssertFullParse(parser, "a", 'a');
            AssertFullParse(parser, new string(' ', 31) + "a", 'a');
            AssertFullParse(parser, new string(' ', 32) + "a", 'a');
            AssertFullParse(parser, new string(' ', 33) + "a", 'a');
            AssertFullParse(parser, new string(' ', 63) + "a", 'a');
            AssertFullParse(parser, new string(' ', 64) + "a", 'a');
            AssertFullParse(parser, new string(' ', 65) + "a", 'a');
        }

        {
            var parser = Return(1).SkipMany();
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestAtLeastOnce()
    {
        {
            var parser = String("foo").AtLeastOnce();
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFullParse(parser, "foo", new[] { "foo" });
            AssertFullParse(parser, "foofoo", new[] { "foo", "foo" });
            AssertPartialParse(parser, "food", new[] { "foo" }, 3);
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Return(1).AtLeastOnce();
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestAtLeastOnceString()
    {
        {
            var parser = Char('f').AtLeastOnceString();
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create('f'))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "b",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.Create('f'))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFullParse(parser, "f", "f");
            AssertFullParse(parser, "ff", "ff");
            AssertPartialParse(parser, "fg", "f", 1);
        }

        {
            var parser = String("f").AtLeastOnceString();
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create('f'))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "b",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.Create('f'))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFullParse(parser, "f", "f");
            AssertFullParse(parser, "ff", "ff");
            AssertPartialParse(parser, "fg", "f", 1);
        }

        {
            var parser = Return('f').AtLeastOnceString();
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestSkipAtLeastOnce()
    {
        {
            var parser = String("foo").SkipAtLeastOnce();
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFullParse(parser, "foo", Unit.Value);
            AssertFullParse(parser, "foofoo", Unit.Value);
            AssertPartialParse(parser, "food", Unit.Value, 3);
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Return(1).SkipAtLeastOnce();
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestUntil()
    {
        {
            var parser = String("foo").Until(Char(' '));
            AssertFullParse(parser, " ", Enumerable.Empty<string>());
            AssertPartialParse(parser, " bar", Enumerable.Empty<string>(), 1);
            AssertFullParse(parser, "foo ", new[] { "foo" });
            AssertFullParse(parser, "foofoo ", new[] { "foo", "foo" });
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "food",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Return(1).Until(Char(' '));
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestManyThen()
    {
        {
            var parser = String("foo").ManyThen(Char(' '));
            AssertFullParse(parser, " ", (Enumerable.Empty<string>(), ' '));
            AssertPartialParse(parser, " bar", (Enumerable.Empty<string>(), ' '), 1);
            AssertFullParse(parser, "foo ", (new[] { "foo" }, ' '));
            AssertFullParse(parser, "foofoo ", (new[] { "foo", "foo" }, ' '));
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "food",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Return(1).ManyThen(Char(' '));
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestSkipUntil()
    {
        {
            var parser = String("foo").SkipUntil(Char(' '));
            AssertFullParse(parser, " ", Unit.Value);
            AssertPartialParse(parser, " bar", Unit.Value, 1);
            AssertFullParse(parser, "foo ", Unit.Value);
            AssertFullParse(parser, "foofoo ", Unit.Value);
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "food",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Return(1).SkipUntil(Char(' '));
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestSkipManyThen()
    {
        {
            var parser = String("foo").SkipManyThen(Char(' '));
            AssertFullParse(parser, " ", ' ');
            AssertPartialParse(parser, " bar", ' ', 1);
            AssertFullParse(parser, "foo ", ' ');
            AssertFullParse(parser, "foofoo ", ' ');
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "food",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Return(1).SkipManyThen(Char(' '));
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestAtLeastOnceUntil()
    {
        {
            var parser = String("foo").AtLeastOnceUntil(Char(' '));
            AssertFullParse(parser, "foo ", new[] { "foo" });
            AssertFullParse(parser, "foofoo ", new[] { "foo", "foo" });
            AssertFailure(
                parser,
                " ",
                new ParseError<char>(
                    Maybe.Just(' '),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                " bar",
                new ParseError<char>(
                    Maybe.Just(' '),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "food",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Return(1).AtLeastOnceUntil(Char(' '));
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestAtLeastOnceThen()
    {
        {
            var parser = String("foo").AtLeastOnceThen(Char(' '));
            AssertFullParse(parser, "foo ", (new[] { "foo" }, ' '));
            AssertFullParse(parser, "foofoo ", (new[] { "foo", "foo" }, ' '));
            AssertFailure(
                parser,
                " ",
                new ParseError<char>(
                    Maybe.Just(' '),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                " bar",
                new ParseError<char>(
                    Maybe.Just(' '),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "food",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Return(1).AtLeastOnceThen(Char(' '));
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestSkipAtLeastOnceUntil()
    {
        {
            var parser = String("foo").SkipAtLeastOnceUntil(Char(' '));
            AssertFullParse(parser, "foo ", Unit.Value);
            AssertFullParse(parser, "foofoo ", Unit.Value);
            AssertFailure(
                parser,
                " ",
                new ParseError<char>(
                    Maybe.Just(' '),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                " bar",
                new ParseError<char>(
                    Maybe.Just(' '),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "food",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Return(1).SkipAtLeastOnceUntil(Char(' '));
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestSkipAtLeastOnceThen()
    {
        {
            var parser = String("foo").SkipAtLeastOnceThen(Char(' '));
            AssertFullParse(parser, "foo ", ' ');
            AssertFullParse(parser, "foofoo ", ' ');
            AssertFailure(
                parser,
                " ",
                new ParseError<char>(
                    Maybe.Just(' '),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                " bar",
                new ParseError<char>(
                    Maybe.Just(' '),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "food",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foofoul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    5,
                    new SourcePosDelta(0, 5),
                    null
                )
            );
        }

        {
            var parser = Return(1).SkipAtLeastOnceThen(Char(' '));
            Assert.Throws<InvalidOperationException>(() => parser.Parse(""));
        }
    }

    [Fact]
    public void TestRepeat()
    {
        {
            var parser = String("foo").Repeat(3);
            AssertFullParse(parser, "foofoofoo", new[] { "foo", "foo", "foo" });
            AssertFailure(
                parser,
                "foofoo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    6,
                    new SourcePosDelta(0, 6),
                    null
                )
            );
        }

        {
            var parser = Char('f').RepeatString(3);
            AssertFullParse(parser, "fff", "fff");
            AssertFailure(
                parser,
                "ff",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("f"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestSeparated()
    {
        {
            var parser = String("foo").Separated(Char(' '));
            AssertPartialParse(parser, "", Enumerable.Empty<string>(), 0);
            AssertFullParse(parser, "foo", new[] { "foo" });
            AssertFullParse(parser, "foo foo", new[] { "foo", "foo" });
            AssertPartialParse(parser, "foobar", new[] { "foo" }, 3);
            AssertPartialParse(parser, "bar", Enumerable.Empty<string>(), 0);
            AssertFailure(
                parser,
                "four",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foo bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    4,
                    new SourcePosDelta(0, 4),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestSeparatedAtLeastOnce()
    {
        {
            var parser = String("foo").SeparatedAtLeastOnce(Char(' '));
            AssertFullParse(parser, "foo", new[] { "foo" });
            AssertFullParse(parser, "foo foo", new[] { "foo", "foo" });
            AssertPartialParse(parser, "foobar", new[] { "foo" }, 3);
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "four",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foo bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    4,
                    new SourcePosDelta(0, 4),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestSeparatedAndTerminated()
    {
        {
            var parser = String("foo").SeparatedAndTerminated(Char(' '));
            AssertFullParse(parser, "foo ", new[] { "foo" });
            AssertFullParse(parser, "foo foo ", new[] { "foo", "foo" });
            AssertPartialParse(parser, "foo bar", new[] { "foo" }, 4);
            AssertPartialParse(parser, "", Array.Empty<string>(), 0);
            AssertPartialParse(parser, "bar", Array.Empty<string>(), 0);
            AssertFailure(
                parser,
                "foo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange(" "))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "four",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foobar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange(" "))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "foo foobar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange(" "))],
                    7,
                    new SourcePosDelta(0, 7),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestSeparatedAndTerminatedAtLeastOnce()
    {
        {
            var parser = String("foo").SeparatedAndTerminatedAtLeastOnce(Char(' '));
            AssertFullParse(parser, "foo ", new[] { "foo" });
            AssertFullParse(parser, "foo foo ", new[] { "foo", "foo" });
            AssertPartialParse(parser, "foo bar", new[] { "foo" }, 4);
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "foo",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.Create(' '))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "four",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foobar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange(" "))],
                    3,
                    new SourcePosDelta(0, 3),
                    null
                )
            );
            AssertFailure(
                parser,
                "foo foobar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange(" "))],
                    7,
                    new SourcePosDelta(0, 7),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestSeparatedAndOptionallyTerminated()
    {
        {
            var parser = String("foo").SeparatedAndOptionallyTerminated(Char(' '));
            AssertFullParse(parser, "foo ", new[] { "foo" });
            AssertFullParse(parser, "foo", new[] { "foo" });
            AssertFullParse(parser, "foo foo ", new[] { "foo", "foo" });
            AssertFullParse(parser, "foo foo", new[] { "foo", "foo" });
            AssertPartialParse(parser, "foo foobar", new[] { "foo", "foo" }, 7);
            AssertPartialParse(parser, "foo foo bar", new[] { "foo", "foo" }, 8);
            AssertPartialParse(parser, "foo bar", new[] { "foo" }, 4);
            AssertPartialParse(parser, "foobar", new[] { "foo" }, 3);
            AssertPartialParse(parser, "", Array.Empty<string>(), 0);
            AssertPartialParse(parser, "bar", Array.Empty<string>(), 0);
            AssertFailure(
                parser,
                "four",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
            AssertFailure(
                parser,
                "foo four",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    6,
                    new SourcePosDelta(0, 6),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestSeparatedAndOptionallyTerminatedAtLeastOnce()
    {
        {
            var parser = String("foo").SeparatedAndOptionallyTerminatedAtLeastOnce(Char(' '));
            AssertFullParse(parser, "foo ", new[] { "foo" });
            AssertFullParse(parser, "foo", new[] { "foo" });
            AssertFullParse(parser, "foo foo ", new[] { "foo", "foo" });
            AssertFullParse(parser, "foo foo", new[] { "foo", "foo" });
            AssertPartialParse(parser, "foo foobar", new[] { "foo", "foo" }, 7);
            AssertPartialParse(parser, "foo foo bar", new[] { "foo", "foo" }, 8);
            AssertPartialParse(parser, "foo bar", new[] { "foo" }, 4);
            AssertPartialParse(parser, "foobar", new[] { "foo" }, 3);
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    true,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "bar",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parser,
                "four",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestBetween()
    {
        {
            var parser = String("foo").Between(Char('{'), Char('}'));
            AssertFullParse(parser, "{foo}", "foo");
        }
    }

    [Fact]
    public void TestOptional()
    {
        {
            var parser = String("foo").Optional();
            AssertFullParse(parser, "foo", Maybe.Just("foo"));
            AssertPartialParse(parser, "food", Maybe.Just("foo"), 3);
            AssertPartialParse(parser, "bar", Maybe.Nothing<string>(), 0);
            AssertPartialParse(parser, "", Maybe.Nothing<string>(), 0);
            AssertFailure(
                parser,
                "four",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("foo"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }

        {
            var parser = Try(String("foo")).Optional();
            AssertFullParse(parser, "foo", Maybe.Just("foo"));
            AssertPartialParse(parser, "food", Maybe.Just("foo"), 3);
            AssertPartialParse(parser, "bar", Maybe.Nothing<string>(), 0);
            AssertPartialParse(parser, "", Maybe.Nothing<string>(), 0);
            AssertPartialParse(parser, "four", Maybe.Nothing<string>(), 0);
        }

        {
            var parser = Char('+').Optional().Then(Digit).Select(char.GetNumericValue);
            AssertFullParse(parser, "1", 1);
            AssertFullParse(parser, "+1", 1);
            AssertFailure(
                parser,
                "a",
                new ParseError<char>(
                    Maybe.Just('a'),
                    false,
                    [new Expected<char>("digit")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
        }
    }

    [Fact]
    public void TestMapWithInput()
    {
        {
            var parser = String("abc").Many().MapWithInput((input, result) => (input.ToString(), result.Count()));
            AssertFullParse(parser, "abc", ("abc", 1));
            AssertFullParse(parser, "abcabc", ("abcabc", 2));

            // long input, to check that it doesn't discard the buffer
            AssertFullParse(
                parser,
                string.Concat(Enumerable.Repeat("abc", 5000)),
                (string.Concat(Enumerable.Repeat("abc", 5000)), 5000)
            );

            AssertFailure(
                parser,
                "abd",
                new ParseError<char>(
                    Maybe.Just('d'),
                    false,
                    [new Expected<char>(ImmutableArray.CreateRange("abc"))],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }
    }

    [Fact]
    public void TestRec()
    {
        // roughly equivalent to String("foo").Separated(Char(' '))
        Parser<char, string>? p2 = null;
        var p1 = String("foo").Then(
            Rec(() => p2!).Optional(),
            (x, y) => y.HasValue ? x + y.Value : x
        );
        p2 = Char(' ').Then(Rec(() => p1));

        AssertFullParse(p1, "foo foo", "foofoo");
    }

    [Fact]
    public void TestLabelled()
    {
        {
            var p = String("foo").Labelled("bar");
            AssertFailure(
                p,
                "baz",
                new ParseError<char>(
                    Maybe.Just('b'),
                    false,
                    [new Expected<char>("bar")],
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                p,
                "foul",
                new ParseError<char>(
                    Maybe.Just('u'),
                    false,
                    [new Expected<char>("bar")],
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }
    }

    private class TestCast1
    {
    }

    private sealed class TestCast2 : TestCast1
    {
        public override bool Equals(object? other) => other is TestCast2;

        public override int GetHashCode() => 1;
    }

    [Fact]
    public void TestCast()
    {
        {
            var parser = Return(new TestCast2()).Cast<TestCast1>();
            AssertPartialParse(parser, "", new TestCast2(), 0);
        }

        {
            var parser = Return(new TestCast1()).OfType<TestCast2>();
            AssertFailure(
                parser,
                "",
                new ParseError<char>(
                    Maybe.Nothing<char>(),
                    false,
                    [new Expected<char>("result of type TestCast2")],
                    0,
                    SourcePosDelta.Zero,
                    "Expected a TestCast2 but got a TestCast1"
                )
            );
        }
    }

    [Fact]
    public void TestCurrentPos()
    {
        {
            var parser = CurrentSourcePosDelta;
            AssertPartialParse(parser, "", SourcePosDelta.Zero, 0);
        }

        {
            var parser = String("foo").Then(CurrentSourcePosDelta);
            AssertFullParse(parser, "foo", new SourcePosDelta(0, 3));
        }

        {
            var parser = Try(String("foo")).Or(Return("")).Then(CurrentSourcePosDelta);
            AssertPartialParse(parser, "f", SourcePosDelta.Zero, 0);  // it should backtrack
        }
    }

    internal enum TestEnum
    {
        Value1 = 0,
        Value2,
        Value3
    }

    [Fact]
    public void EnumParseTest()
    {
        const string value1 = "value1";
        var result = Parser.Enum<TestEnum>().Parse(value1);
        Assert.False(result.Success);

        result = Parser.CIEnum<TestEnum>().Parse(value1);
        Assert.Equal(TestEnum.Value1, result.Value);

        const string value2 = "Value2";
        result = Parser.Enum<TestEnum>().Parse(value2);
        Assert.Equal(TestEnum.Value2, result.Value);

        result = Parser.CIEnum<TestEnum>().Parse(value2);
        Assert.Equal(TestEnum.Value2, result.Value);
    }
}

#pragma warning restore CA1861  // Prefer 'static readonly' fields over constant array arguments if the called method is called repeatedly and is not mutating the passed array
