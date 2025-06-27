using System.Collections.Immutable;

using Pidgin.Incremental;

using Xunit;

using static Pidgin.Parser;

namespace Pidgin.Tests;

public class IncrementalParsingTests
{
    [Fact]
    public void TestNoChanges()
    {
        var parser = String("foo").Select(Str).Incremental();
        var (ctx1, result1) = parser.ParseIncrementallyOrThrow("food", null);
        Assert.Equal(Str("foo"), result1);

        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("food", ctx1);
        Assert.Same(result1, result2);

        // make an edit outside of the consumed range - should not invalidate the cache
        ctx2 = ctx2.AddEdit(new(new(4, 0), 1));

        var (ctx3, result3) = parser.ParseIncrementallyOrThrow("foods", ctx2);
        Assert.Same(result2, result3);
    }

    [Fact]
    public void TestCacheInvalidation()
    {
        var parser = String("foo").Select(Str).Incremental();
        var (ctx1, result1) = parser.ParseIncrementallyOrThrow("foo", null);
        Assert.Equal(Str("foo"), result1);

        // a no-op edit in the middle of the consumed range
        ctx1 = ctx1.AddEdit(new(new(1, 0), 0));

        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("foo", ctx1);
        Assert.NotSame(result1, result2);
        Assert.Equal(result1, result2);

        var (ctx3, result3) = parser.ParseIncrementallyOrThrow("foo", ctx2);
        Assert.Same(result2, result3);
    }

    [Fact]
    public void TestCacheInvalidationWithLookahead()
    {
        var parser = Try(String("food")).Or(String("foo")).Select(Str).Incremental();
        var (ctx1, result1) = parser.ParseIncrementallyOrThrow("foot", null);
        Assert.Equal(Str("foo"), result1);

        // An edit after the consumed range but touching
        // the right side of the lookaround range
        // (should invalidate cache - see "NOTE: Edits at
        // ends of cached results" in LocationRange)
        ctx1 = ctx1.AddEdit(new(new(4, 0), 2));

        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("footie", ctx1);
        Assert.NotSame(result1, result2);
        Assert.Equal(result1, result2);

        var (ctx3, result3) = parser.ParseIncrementallyOrThrow("footie", ctx2);
        Assert.Same(result2, result3);
    }

    [Fact]
    public void TestShiftCachedResultRight()
    {
        var parser = SkipWhitespaces.Then(String("foo").Select(Str).Incremental());
        var (ctx1, result1) = parser.ParseIncrementallyOrThrow("foo", null);
        Assert.Equal(Str("foo"), result1);

        // edit touching the left of the cached range
        // (should not invalidate cache - see "NOTE:
        // Edits at ends of cached results" in LocationRange)
        ctx1 = ctx1.AddEdit(new(new(0, 0), 2));

        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("  foo", ctx1);
        var shifted2 = Assert.IsType<Shifted>(result2);
        Assert.Equal(2, shifted2.Shift);
        Assert.Same(result1, shifted2.Unshifted);

        ctx2 = ctx2.AddEdit(new(new(0, 0), 2));

        var (ctx3, result3) = parser.ParseIncrementallyOrThrow("    foo", ctx2);
        var shifted3 = Assert.IsType<Shifted>(result3);
        Assert.Equal(4, shifted3.Shift);
        Assert.Same(result1, shifted3.Unshifted);
    }

    [Fact]
    public void TestShiftCachedResultLeft()
    {
        var parser = SkipWhitespaces.Then(String("foo").Select(Str).Incremental());
        var (ctx1, result1) = parser.ParseIncrementallyOrThrow("    foo", null);
        Assert.Equal(Str("foo"), result1);

        // edit touching the left of the cached range
        // (should not invalidate cache - see "NOTE:
        // Edits at ends of cached results" in LocationRange)
        ctx1 = ctx1.AddEdit(new(new(1, 2), 0));

        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("  foo", ctx1);
        var shifted2 = Assert.IsType<Shifted>(result2);
        Assert.Equal(-2, shifted2.Shift);
        Assert.Same(result1, shifted2.Unshifted);

        ctx2 = ctx2.AddEdit(new(new(0, 2), 0));

        var (ctx3, result3) = parser.ParseIncrementallyOrThrow("foo", ctx2);
        var shifted3 = Assert.IsType<Shifted>(result3);
        Assert.Equal(-4, shifted3.Shift);
        Assert.Same(result1, shifted3.Unshifted);
    }

    [Fact]
    public void TestNestedExpressionsPartialInvalidation()
    {
        var parser = Rec<char, Result>(expr =>
            Char('(')
                .Then(expr.Many().Select(xs => new List([.. xs])).Cast<Result>())
                .Before(Char(')'))
                .Or(String("foo").Select(Str))
                .Incremental()
        );

        var (ctx1, result1) = parser.ParseIncrementallyOrThrow("((foo))", null);
        var list11 = Assert.IsType<List>(result1);
        var list12 = Assert.IsType<List>(list11.Children[0]);
        Assert.Equal(Str("foo"), list12.Children[0]);

        ctx1 = ctx1.AddEdit(new(new(1, 0), 5));
        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("((foo)(foo))", ctx1);

        // Should rebuild the spine of the expression but reuse unaffected parts
        Assert.NotSame(result1, result2);
        var list21 = Assert.IsType<List>(result2);
        Assert.Equal(2, list21.Children.Length);
        var list22 = Assert.IsType<List>(list21.Children[0]);
        Assert.NotSame(list11.Children[0], list22);
        var shifted2 = Assert.IsType<Shifted>(list21.Children[1]);
        Assert.Equal(5, shifted2.Shift);
        Assert.Same(list11.Children[0], shifted2.Unshifted);
    }

    [Fact]
    public void TestShiftChildResultMultipleTimes()
    {
        // test for a bug in Tree.Search
        var parser = Rec<char, Result>(expr =>
            Char('(')
                .Then(expr.Many().Select(xs => new List([.. xs])).Cast<Result>())
                .Before(Char(')'))
                .Or(String("foo").Select(Str))
                .Incremental()
        );

        var (ctx1, result1) = parser.ParseIncrementallyOrThrow("((foo))", null);
        var list11 = Assert.IsType<List>(result1);
        var list12 = Assert.IsType<List>(list11.Children[0]);
        Assert.Equal(Str("foo"), list12.Children[0]);

        ctx1 = ctx1.AddEdit(new(new(1, 0), 5));
        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("((foo)(foo))", ctx1);

        ctx2 = ctx2.AddEdit(new(new(7, 0), 5));
        var (ctx3, result3) = parser.ParseIncrementallyOrThrow("((foo)((foo)foo))", ctx2);
        var reusedChild = ((List)((List)result3).Children[1]).Children[1];
        var shifted = Assert.IsType<Shifted>(reusedChild);
        Assert.Equal(10, shifted.Shift);
        Assert.Same(list12.Children[0], shifted.Unshifted);
    }

    [Fact]
    public void TestBacktrackOverCachedResult()
    {
        // If an incremental parser succeeds but is later backtracked over,
        // the result remains cached. As of right now I've convinced myself
        // that this is all right but perhaps there are scenarios (that I
        // haven't foreseen) in which this is incorrect.
        var foo = String("foo").Select(Str).Incremental();
        var parser = Try(foo.Before(String("bar"))).Or(foo);
        var (ctx1, result1) = parser.ParseIncrementallyOrThrow("food", null);
        Assert.Equal(Str("foo"), result1);

        // CachedParseResultTable.Search() takes the first result it finds
        // in the tree (the one that was backtracked over), so you don't
        // see reuse until the second reparse.
        // todo: maybe this ought to be fixed (use the last result, not the first?)
        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("food", ctx1);
        var (ctx3, result3) = parser.ParseIncrementallyOrThrow("food", ctx2);
        Assert.Same(result2, result3);
    }

    [Fact]
    public void TestUseIncrementalParserWithNonIncrementalParseMethod()
    {
        var parser = String("foo").Select(Str).Incremental();
        var result = parser.ParseOrThrow("food");
        Assert.Equal(Str("foo"), result);
    }

    [Fact]
    public void TestMultipleParsersAtSameStartPosition()
    {
        var innerParser = String("foo").Select(Str).Incremental();
        var outerParser = innerParser.Then(
            Try(String("bar")).Or(String("baz")).Select(Str),
            (foo, bar) => new List([foo, bar])
        )
            .Cast<Result>()
            .Incremental();

        var (ctx1, result1) = outerParser.ParseIncrementallyOrThrow("foobar", null);
        var list1 = Assert.IsType<List>(result1);

        var (ctx2, result2) = outerParser.ParseIncrementallyOrThrow("foobar", ctx1);
        Assert.Same(result1, result2);

        ctx2 = ctx2.AddEdit(new(new(5, 1), 1));
        var (ctx3, result3) = outerParser.ParseIncrementallyOrThrow("foobaz", ctx2);
        Assert.NotSame(result1, result3);
        Assert.Same(list1.Children[0], Assert.IsType<List>(result3).Children[0]);

        var outerParser2 = innerParser.Then(
            String("bat").Select(Str),
            (foo, bar) => new List([foo, bar])
        )
            .Cast<Result>()
            .Incremental();

        ctx3 = ctx3.AddEdit(new(new(5, 1), 1));
        var (ctx4, result4) = outerParser2.ParseIncrementallyOrThrow("foobat", ctx3);
        Assert.NotSame(result1, result4);
        Assert.Same(list1.Children[0], Assert.IsType<List>(result4).Children[0]);
    }

    private static Result Str(string value)
        => new String(value);

    private abstract record Result : IShiftable<Result>
    {
        public virtual Result ShiftBy(long amount)
            => amount != 0 ? new Shifted(this, amount) : this;
    }

    private sealed record String(string Value) : Result;

    private sealed record Shifted(Result Unshifted, long Shift) : Result
    {
        public override Result ShiftBy(long amount)
            => amount != 0 ? this with { Shift = Shift + amount } : this;
    }

    private sealed record List(ImmutableArray<Result> Children) : Result;
}
