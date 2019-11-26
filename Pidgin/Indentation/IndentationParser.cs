using System;
using System.Collections.Generic;
using System.Linq;

namespace Pidgin.Indentation
{
    public sealed class IndentationParser
    {
        public static Parser<TToken, T> TopLevel<TToken, T>(Parser<TToken, T> parser)
            => Parser<TToken>.CurrentPos.Assert(p => p.Col == 1).Then(parser);

        public static Parser<TToken, R> Block<TToken, T, U, R>(
            Parser<TToken, T> reference,
            Func<T, IndentationMode<TToken, U>> continuation,
            Func<T, U, R> resultSelector
        ) => Parser<TToken>.CurrentPos
            .Then(reference, ValueTuple.Create)
            .Then(
                t => continuation(t.Item2).Continue(t.Item1),
                (t, x) => resultSelector(t.Item2, x)
            );

        public static Parser<TToken, R> Block<TToken, T, R>(
            Parser<TToken, T> reference,
            Func<T, IndentationMode<TToken, R>> continuation
        ) => Parser<TToken>.CurrentPos
            .Then(reference, ValueTuple.Create)
            .Then(t => continuation(t.Item2).Continue(t.Item1));

        public static Parser<TToken, R> Block<TToken, T, U, R>(
            Parser<TToken, T> reference,
            IndentationMode<TToken, U> continuation,
            Func<T, U, R> resultSelector
        ) => Parser<TToken>.CurrentPos
            .Then(reference, ValueTuple.Create)
            .Then(
                t => continuation.Continue(t.Item1),
                (t, x) => resultSelector(t.Item2, x)
            );

        public static Parser<TToken, R> Block<TToken, T, R>(
            Parser<TToken, T> reference,
            IndentationMode<TToken, R> continuation
        ) => Parser<TToken>.CurrentPos.Before(reference)
            .Then(x => continuation.Continue(x));
    }

    public static class IndentationMode
    {
        public static IndentationMode<TToken, U> Indented<TToken, T, U>(Parser<TToken, T> indentedLine, Func<IEnumerable<T>, U> resultSelector)
            => new Block<TToken, T, U>(indentedLine, resultSelector, false);
        
        public static IndentationMode<TToken, IEnumerable<T>> Indented<TToken, T>(Parser<TToken, T> indentedLine)
            => new Block<TToken, T, IEnumerable<T>>(indentedLine, xs => xs, false);
        
        public static IndentationMode<TToken, U> IndentedAtLeastOnce<TToken, T, U>(Parser<TToken, T> indentedLine, Func<IEnumerable<T>, U> resultSelector)
            => new Block<TToken, T, U>(indentedLine, resultSelector, true);
        
        public static IndentationMode<TToken, IEnumerable<T>> IndentedAtLeastOnce<TToken, T>(Parser<TToken, T> indentedLine)
            => new Block<TToken, T, IEnumerable<T>>(indentedLine, xs => xs, true);
    }

    public static class IndentationMode<TToken>
    {
        public static IndentationMode<TToken, T> NonIndented<T>(T result)
            => new NoBlock<TToken, T>(result);
    }

    public abstract class IndentationMode<TToken, T>
    {
        internal IndentationMode() { }

        internal abstract Parser<TToken, T> Continue(SourcePos initialPos);
    }

    internal class NoBlock<TToken, T> : IndentationMode<TToken, T>
    {
        private readonly Parser<TToken, T> _returnResult;

        public NoBlock(T result)
        {
            _returnResult = Parser<TToken>.Return(result);
        }

        internal override Parser<TToken, T> Continue(SourcePos initialPos)
            => _returnResult;
    }

    internal class Block<TToken, T, U> : IndentationMode<TToken, U>
    {
        private readonly Parser<TToken, T> _indentedLine;
        private readonly Func<IEnumerable<T>, U> _resultSelector;
        private readonly bool _atLeastOnce;

        public Block(Parser<TToken, T> indentedLine, Func<IEnumerable<T>, U> resultSelector, bool atLeastOnce)
        {
            _indentedLine = indentedLine;
            _resultSelector = resultSelector;
            _atLeastOnce = atLeastOnce;
        }

        internal override Parser<TToken, U> Continue(SourcePos initialPos)
            => Parser<TToken>.CurrentPos
                .Assert(p => p.Col > initialPos.Col)
                .Then(referencePos =>
                    Parser<TToken>.CurrentPos
                        .Assert(p => p.Col == referencePos.Col)
                        .Then(_indentedLine)
                        .AtLeastOnce()
                )
                .Or(NoIndentationFound)
                .Select(_resultSelector);

        private Parser<TToken, IEnumerable<T>> NoIndentationFound
            => _atLeastOnce
                ? Parser<TToken>.Fail<IEnumerable<T>>()
                : Parser<TToken>.Return(Enumerable.Empty<T>());
    }
}