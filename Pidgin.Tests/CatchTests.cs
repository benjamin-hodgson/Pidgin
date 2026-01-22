using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

using Xunit;

namespace Pidgin.Tests;

public partial class CatchTests : ParserTestBase
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
        DoTest((p, x) => p.Parse(x.Span), x => x, x => x.AsMemory());
    }

    private static void DoTest<TToken, TInput>(
        Func<Parser<TToken, IEnumerable<TToken>>, TInput, Result<TToken, IEnumerable<TToken>>> parseFunc,
        Func<string, IEnumerable<TToken>> render,
        Func<string, TInput> toInput
    )
        where TToken : IEquatable<TToken>
    {
        {
            var parser =
                Parser<TToken>.Sequence(render("foo"))
                .Or(Parser<TToken>.Sequence(render("1throw"))
                    .Then(Parser<TToken>.Sequence(render("after"))
                        .RecoverWith(e => throw new InvalidOperationException())))
                .Or(Parser<TToken>.Sequence(render("2throw"))
                    .Then(Parser<TToken>.Sequence(render("after"))
                        .RecoverWith(e => throw new NotImplementedException())))
                .Catch<InvalidOperationException>((e, i) => Parser<TToken>.Any.Repeat(i))
                .Catch<NotImplementedException>((e) => Parser<TToken>.Any.Many());
            AssertSuccess(parseFunc(parser, toInput("foobar")), render("foo"));
            AssertSuccess(parseFunc(parser, toInput("1throwafter")), render("after"));
            AssertSuccess(parseFunc(parser, toInput("1throwandrecover")), render("1throwa")); // it should have consumed the "1throwa" but then backtracked
            AssertSuccess(parseFunc(parser, toInput("1throwaftsomemore")), render("1throwaft")); // it should have consumed the "1throwaft" but then backtracked
            AssertSuccess(parseFunc(parser, toInput("2throwafter")), render("after"));
            AssertSuccess(parseFunc(parser, toInput("2throwandrecover")), render("2throwandrecover")); // it should have consumed the "2throwa" but then backtracked
            AssertSuccess(parseFunc(parser, toInput("2throwaftsomemore")), render("2throwaftsomemore")); // it should have consumed the "2throwaft" but then backtracked
            AssertFailure(
                parseFunc(parser, toInput("f")),
                new ParseError<TToken>(
                    Maybe.Nothing<TToken>(),
                    true,
                    ImmutableArray.Create(new Expected<TToken>(ImmutableArray.CreateRange(render("foo")))),
                    1,
                    SourcePosDelta.OneCol,
                    null
                )
            );
            AssertFailure(
                parseFunc(parser, toInput("")),
                new ParseError<TToken>(
                    Maybe.Nothing<TToken>(),
                    true,
                    ImmutableArray.Create(new Expected<TToken>(ImmutableArray.CreateRange(render("foo"))), new Expected<TToken>(ImmutableArray.CreateRange(render("1throw"))), new Expected<TToken>(ImmutableArray.CreateRange(render("2throw")))),
                    0,
                    SourcePosDelta.Zero,
                    null
                )
            );
            AssertFailure(
                parseFunc(parser, toInput("foul")),
                new ParseError<TToken>(
                    Maybe.Just(render("u").Single()),
                    false,
                    ImmutableArray.Create(new Expected<TToken>(ImmutableArray.CreateRange(render("foo")))),
                    2,
                    new SourcePosDelta(0, 2),
                    null
                )
            );
        }
    }
}
