using System;

namespace Pidgin.Permutation;

internal abstract class PermutationParserBranch<TToken, T>
{
    public abstract PermutationParserBranch<TToken, R> Add<U, R>(Parser<TToken, U> parser, Func<T, U, R> resultSelector);

    public abstract PermutationParserBranch<TToken, R> AddOptional<U, R>(Parser<TToken, U> parser, Func<U> defaultValueFactory, Func<T, U, R> resultSelector);

    public abstract Parser<TToken, T> Build();
}

#pragma warning disable SA1402  // "File may only contain a single type"
internal sealed class PermutationParserBranchImpl<TToken, U, T, R> : PermutationParserBranch<TToken, R>
#pragma warning restore SA1402  // "File may only contain a single type"
{
    private readonly Parser<TToken, U> _parser;
    private readonly PermutationParser<TToken, T> _perm;
    private readonly Func<T, U, R> _func;

    public PermutationParserBranchImpl(Parser<TToken, U> parser, PermutationParser<TToken, T> perm, Func<T, U, R> func)
    {
        _parser = parser;
        _perm = perm;
        _func = func;
    }

    public override PermutationParserBranch<TToken, W> Add<V, W>(Parser<TToken, V> parser, Func<R, V, W> resultSelector)
        => Add(p => p.Add(parser), resultSelector);

    public override PermutationParserBranch<TToken, W> AddOptional<V, W>(Parser<TToken, V> parser, Func<V> defaultValueFactory, Func<R, V, W> resultSelector)
        => Add(p => p.AddOptional(parser, defaultValueFactory), resultSelector);

    private PermutationParserBranch<TToken, W> Add<V, W>(Func<PermutationParser<TToken, T>, PermutationParser<TToken, (T, V)>> addPerm, Func<R, V, W> resultSelector)
    {
        var this_func = _func;
        return new PermutationParserBranchImpl<TToken, U, (T, V), W>(
            _parser,
            addPerm(_perm),
            (tv, u) => resultSelector(this_func(tv.Item1, u), tv.Item2)
        );
    }

    public override Parser<TToken, R> Build()
    {
        var this_func = _func;
        return Parser.Map((x, y) => this_func(y, x), _parser, _perm.Build());
    }
}
