using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using static Pidgin.Parser;

namespace Pidgin.Tests
{
    public class TryTests : ParserTestBase
    {
        [Fact]
        public void TestString()
        {
            DoTest((p, x) => p.Parse(x), x => x, x => x);
        }
        [Fact]
        public void TestList()
        {
            DoTest((p, x) => p.Parse(x), x => x, x => x.ToCharArray());
        }
        [Fact]
        public void TestReadOnlyList()
        {
            DoTest((p, x) => p.ParseReadOnlyList(x), x => x, x => x.ToCharArray());
        }
        [Fact]
        public void TestEnumerator()
        {
            DoTest((p, x) => p.Parse(x), x => x, x => x.AsEnumerable());
        }
        [Fact]
        public void TestReader()
        {
            DoTest((p, x) => p.Parse(x), x => x, x => new StringReader(x));
        }
        [Fact]
        public void TestStream()
        {
            DoTest((p, x) => p.Parse(x), Encoding.ASCII.GetBytes, x => new MemoryStream(Encoding.ASCII.GetBytes(x)));
        }
        [Fact]
        public void TestSpan()
        {
            IEnumerable<char> render(string input) => input;
            ReadOnlySpan<char> toInput(string input) => input.AsSpan();
            Result<char, T> parseFunc<T>(Parser<char, T> parser, ReadOnlySpan<char> input) => parser.Parse(input);
            {
                var parser =
                    Try(Parser<char>.Sequence(render("foo")))
                        .Then(Parser<char>.Sequence(render("bar")))
                        .Or(Parser<char>.Sequence(render("four")));
                AssertSuccess(parseFunc(parser, toInput("foobar")), render("bar"), true);
                AssertSuccess(parseFunc(parser, toInput("four")), render("four"), true);  // it should have consumed the "fo" but then backtracked
                AssertFailure(
                    parseFunc(parser, toInput("foo")),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[]{ new Expected<char>(Rope.CreateRange(render("foobar"))) },
                        new SourcePos(1,4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("foobag")),
                    new ParseError<char>(
                        Maybe.Just(render("g").Single()),
                        false,
                        new[]{ new Expected<char>(Rope.CreateRange(render("foobar"))) },
                        new SourcePos(1,6),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("f")),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[]{ new Expected<char>(Rope.CreateRange(render("four"))) },
                        new SourcePos(1,2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("")),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[]{ new Expected<char>(Rope.CreateRange(render("foobar"))), new Expected<char>(Rope.CreateRange(render("four"))) },
                        new SourcePos(1,1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    parseFunc(parser, toInput("foul")),
                    new ParseError<char>(
                        Maybe.Just(render("l").Single()),
                        false,
                        new[]{ new Expected<char>(Rope.CreateRange(render("four"))) },
                        new SourcePos(1,4),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Try(
                    Parser<char>.Sequence(render("foo")).Then(
                        Try(Parser<char>.Sequence(render("bar"))).Or(Parser<char>.Sequence(render("baz")))
                    )
                ).Or(Parser<char>.Sequence(render("foobat")));
                AssertSuccess(parseFunc(parser, toInput("foobar")), render("bar"), true);
                AssertSuccess(parseFunc(parser, toInput("foobaz")), render("baz"), true);
                // "" -> "foo" -> "fooba[r]" -> "foo" -> "fooba[z]" -> "foo" -> "" -> "foobat"
                AssertSuccess(parseFunc(parser, toInput("foobat")), render("foobat"), true);
                AssertFailure(
                    parseFunc(parser, toInput("fooba")),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[]{ new Expected<char>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1, 6),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("foobag")),
                    new ParseError<char>(
                        Maybe.Just(render("g").Single()),
                        false,
                        new[]{ new Expected<char>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1,6),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("foob")),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[]{ new Expected<char>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1, 5),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("foo")),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[]{ new Expected<char>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("f")),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[]{ new Expected<char>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("foul")),
                    new ParseError<char>(
                        Maybe.Just(render("u").Single()),
                        false,
                        new[]{ new Expected<char>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1,3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("")),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[]{
                            new Expected<char>(Rope.CreateRange(render("foobar"))),
                            new Expected<char>(Rope.CreateRange(render("foobat"))),
                            new Expected<char>(Rope.CreateRange(render("foobaz")))
                        },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
        }


        private void DoTest<TToken, TInput>(
            Func<Parser<TToken, IEnumerable<TToken>>, TInput, Result<TToken, IEnumerable<TToken>>> parseFunc,
            Func<string, IEnumerable<TToken>> render,
            Func<string, TInput> toInput
        ) where TToken : IEquatable<TToken>
        {
            {
                var parser =
                    Try(Parser<TToken>.Sequence(render("foo")))
                        .Then(Parser<TToken>.Sequence(render("bar")))
                        .Or(Parser<TToken>.Sequence(render("four")));
                AssertSuccess(parseFunc(parser, toInput("foobar")), render("bar"), true);
                AssertSuccess(parseFunc(parser, toInput("four")), render("four"), true);  // it should have consumed the "fo" but then backtracked
                AssertFailure(
                    parseFunc(parser, toInput("foo")),
                    new ParseError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("foobar"))) },
                        new SourcePos(1,4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("foobag")),
                    new ParseError<TToken>(
                        Maybe.Just(render("g").Single()),
                        false,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("foobar"))) },
                        new SourcePos(1,6),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("f")),
                    new ParseError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("four"))) },
                        new SourcePos(1,2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("")),
                    new ParseError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("foobar"))), new Expected<TToken>(Rope.CreateRange(render("four"))) },
                        new SourcePos(1,1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    parseFunc(parser, toInput("foul")),
                    new ParseError<TToken>(
                        Maybe.Just(render("l").Single()),
                        false,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("four"))) },
                        new SourcePos(1,4),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Try(
                    Parser<TToken>.Sequence(render("foo")).Then(
                        Try(Parser<TToken>.Sequence(render("bar"))).Or(Parser<TToken>.Sequence(render("baz")))
                    )
                ).Or(Parser<TToken>.Sequence(render("foobat")));
                AssertSuccess(parseFunc(parser, toInput("foobar")), render("bar"), true);
                AssertSuccess(parseFunc(parser, toInput("foobaz")), render("baz"), true);
                // "" -> "foo" -> "fooba[r]" -> "foo" -> "fooba[z]" -> "foo" -> "" -> "foobat"
                AssertSuccess(parseFunc(parser, toInput("foobat")), render("foobat"), true);
                AssertFailure(
                    parseFunc(parser, toInput("fooba")),
                    new ParseError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1, 6),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("foobag")),
                    new ParseError<TToken>(
                        Maybe.Just(render("g").Single()),
                        false,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1,6),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("foob")),
                    new ParseError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1, 5),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("foo")),
                    new ParseError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("f")),
                    new ParseError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("foul")),
                    new ParseError<TToken>(
                        Maybe.Just(render("u").Single()),
                        false,
                        new[]{ new Expected<TToken>(Rope.CreateRange(render("foobat"))) },
                        new SourcePos(1,3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    parseFunc(parser, toInput("")),
                    new ParseError<TToken>(
                        Maybe.Nothing<TToken>(),
                        true,
                        new[]{
                            new Expected<TToken>(Rope.CreateRange(render("foobar"))),
                            new Expected<TToken>(Rope.CreateRange(render("foobat"))),
                            new Expected<TToken>(Rope.CreateRange(render("foobaz")))
                        },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
        }
    }
}