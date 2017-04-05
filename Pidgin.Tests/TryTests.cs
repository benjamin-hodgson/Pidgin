using System;
using System.Collections.Generic;
using System.IO;
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
            DoTest((p, x) => p.Parse(x), x => x, x => (IEnumerable<char>)x);
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


        private void DoTest<TToken, TInput>(Func<Parser<TToken, IEnumerable<TToken>>, TInput, Result<TToken, IEnumerable<TToken>>> parseFunc, Func<string, IEnumerable<TToken>> render, Func<string, TInput> toInput) where TToken : IEquatable<TToken>
        {
            {
                var parser =
                    Try(Parser<TToken>.Sequence(render("foo")))
                        .Then(Parser<TToken>.Sequence(render("bar")))
                        .Or(Parser<TToken>.Sequence(render("four")));
                AssertSuccess(parseFunc(parser, toInput("foobar")), render("bar"), true);
                AssertSuccess(parseFunc(parser, toInput("four")), render("four"), true);  // it should have consumed the "fo" but then backtracked
                AssertFailure(parseFunc(parser, toInput("foo")), new[]{ new Expected<TToken>(render("foobar")) }, new SourcePos(1,4), true);
                AssertFailure(parseFunc(parser, toInput("f")), new[]{ new Expected<TToken>(render("four")) }, new SourcePos(1,2), true);
                AssertFailure(parseFunc(parser, toInput("")), new[]{ new Expected<TToken>(render("foobar")), new Expected<TToken>(render("four")) }, new SourcePos(1,1), false);
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
                AssertFailure(parseFunc(parser, toInput("fooba")), new[]{ new Expected<TToken>(render("foobat")) }, new SourcePos(1, 6), true);
                AssertFailure(parseFunc(parser, toInput("foob")), new[]{ new Expected<TToken>(render("foobat")) }, new SourcePos(1, 5), true);
                AssertFailure(parseFunc(parser, toInput("foo")), new[]{ new Expected<TToken>(render("foobat")) }, new SourcePos(1, 4), true);
                AssertFailure(parseFunc(parser, toInput("f")), new[]{ new Expected<TToken>(render("foobat")) }, new SourcePos(1, 2), true);
                AssertFailure(parseFunc(parser, toInput("")), new[]{ new Expected<TToken>(render("foobar")), new Expected<TToken>(render("foobat")), new Expected<TToken>(render("foobaz")) }, new SourcePos(1, 1), false);
            }
        }
    }
}