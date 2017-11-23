using System;
using System.Collections.Immutable;

namespace Pidgin.Permutation
{
    internal abstract class PermutationParserTree<TToken, T>
    {
        public PermutationParserTree() {}

        public abstract Parser<TToken, T> Build();

        public abstract PermutationParserTree<TToken, (T, U)> Add<U>(Parser<TToken, U> parser);
        
        public abstract PermutationParser<TToken, (T, U)> AddOptional<U>(Parser<TToken, U> parser, Func<U> defaultValueFactory);

        public abstract PermutationParserTree<TToken, Func<U, (T, U)>> Extend<U>();

        public abstract PermutationParserTree<TToken, U> Select<U>(Func<T, U> func);
    }

    internal sealed class PermutationParserLeaf<TToken, T> : PermutationParserTree<TToken, T>
    {
        private readonly Func<T> _resultFactory;

        public PermutationParserLeaf(Func<T> resultFactory)
        {
            _resultFactory = resultFactory;
        }

        public override PermutationParserTree<TToken, (T, U)> Add<U>(Parser<TToken, U> parser)
            => new PermutationParserBranch<TToken, U, (T, U)>(
                parser,
                new PermutationParser<TToken, Func<U, (T, U)>>(ImmutableList.Create(this.Extend<U>()))
            );

        public override PermutationParser<TToken, (T, U)> AddOptional<U>(Parser<TToken, U> parser, Func<U> defaultValueFactory)
        {
            throw new NotImplementedException();
        }

        public override Parser<TToken, T> Build() => Parser<TToken>.Return(_resultFactory).Select(f => f());

        public override PermutationParserTree<TToken, Func<U, (T, U)>> Extend<U>()
        {
            var resultFactory = this._resultFactory;
            Func<U, (T, U)> newResultFactory = x => (resultFactory(), x);
            return new PermutationParserLeaf<TToken, Func<U, (T, U)>>(() => newResultFactory);
        }

        public override PermutationParserTree<TToken, U> Select<U>(Func<T, U> func)
        {
            var resultFactory = this._resultFactory;
            return new PermutationParserLeaf<TToken, U>(() => func(resultFactory()));
        }
    }
    internal sealed class PermutationParserBranch<TToken, U, T> : PermutationParserTree<TToken, T>
    {
        private readonly Parser<TToken, U> _parser;
        private readonly PermutationParser<TToken, Func<U, T>> _children;

        public PermutationParserBranch(Parser<TToken, U> parser, PermutationParser<TToken, Func<U, T>> children)
        {
            _parser = parser;
            _children = children;
        }

        public override PermutationParserTree<TToken, (T, V)> Add<V>(Parser<TToken, V> parser)
            => new PermutationParserBranch<TToken, U, (T, V)>(
                _parser,
                _children.Add(parser).Select<Func<U, (T, V)>>(pair => x => (pair.Item1(x), pair.Item2))
            );

        public override PermutationParser<TToken, (T, V)> AddOptional<V>(Parser<TToken, V> parser, Func<V> defaultValueFactory)
        {
            throw new NotImplementedException();
        }

        public override Parser<TToken, T> Build()
            => Parser.Map((x, f) => f(x), _parser, _children.Build());

        public override PermutationParserTree<TToken, Func<V, (T, V)>> Extend<V>()
            => new PermutationParserBranch<TToken, U, Func<V, (T, V)>>(
                _parser,
                _children.Select<Func<U, Func<V, (T, V)>>>(f => x => y => (f(x), y))
            );

        public override PermutationParserTree<TToken, V> Select<V>(Func<T, V> func)
            => new PermutationParserBranch<TToken, U, V>(
                _parser,
                _children.Select<Func<U, V>>(f => x => func(f(x)))
            );
    }
}