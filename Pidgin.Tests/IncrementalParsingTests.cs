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
        var nonShifted1 = Assert.IsType<String>(result1);
        Assert.Equal("foo", nonShifted1.Value);

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
        var nonShifted1 = Assert.IsType<String>(result1);
        Assert.Equal("foo", nonShifted1.Value);

        // a no-op edit in the middle of the consumed range
        ctx1 = ctx1.AddEdit(new(new(1, 0), 0));

        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("foo", ctx1);
        Assert.NotSame(result1, result2);
        var nonShifted2 = Assert.IsType<String>(result2);
        Assert.Equal("foo", nonShifted2.Value);

        var (ctx3, result3) = parser.ParseIncrementallyOrThrow("foo", ctx2);
        Assert.Same(result2, result3);
    }

    [Fact]
    public void TestCacheInvalidationWithLookahead()
    {
        var parser = Try(String("food")).Or(String("foo")).Select(Str).Incremental();
        var (ctx1, result1) = parser.ParseIncrementallyOrThrow("foot", null);
        var nonShifted1 = Assert.IsType<String>(result1);
        Assert.Equal("foo", nonShifted1.Value);

        // An edit after the consumed range but touching
        // the right side of the lookaround range
        ctx1 = ctx1.AddEdit(new(new(4, 0), 2));

        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("footie", ctx1);
        Assert.NotSame(result1, result2);
        var nonShifted2 = Assert.IsType<String>(result2);
        Assert.Equal("foo", nonShifted2.Value);

        var (ctx3, result3) = parser.ParseIncrementallyOrThrow("footie", ctx2);
        Assert.Same(result2, result3);
    }

    [Fact]
    public void TestShiftCachedResult()
    {
        var parser = SkipWhitespaces.Then(String("foo").Select(Str).Incremental());
        var (ctx1, result1) = parser.ParseIncrementallyOrThrow(" foo", null);
        var nonShifted1 = Assert.IsType<String>(result1);
        Assert.Equal("foo", nonShifted1.Value);

        // edit to the left of the cached range
        ctx1 = ctx1.AddEdit(new(new(0, 0), 1));

        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("  foo", ctx1);
        Assert.NotSame(result1, result2);
        var shifted2 = Assert.IsType<Shifted>(result2);
        Assert.Same(nonShifted1, shifted2.Unshifted);

        var (ctx3, result3) = parser.ParseIncrementallyOrThrow("  foo", ctx2);
        Assert.Same(result2, result3);
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
        var nonShifted1 = Assert.IsType<String>(list12.Children[0]);
        Assert.Equal("foo", nonShifted1.Value);

        ctx1 = ctx1.AddEdit(new(new(1, 0), 5));
        var (ctx2, result2) = parser.ParseIncrementallyOrThrow("((foo)(foo))", ctx1);

        Assert.NotSame(result1, result2);
        var list21 = Assert.IsType<List>(result2);
        Assert.Equal(2, list21.Children.Length);
        var shifted2 = Assert.IsType<Shifted>(list21.Children[1]);
        Assert.Equal(5, shifted2.Shift);
        Assert.Same(list11.Children[0], shifted2.Unshifted);
    }

    private static Result Str(string value)
        => new String(value);

    private abstract record Result : IIncrementalParseResult<Result>
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
