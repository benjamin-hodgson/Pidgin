using System;

namespace Pidgin.Configuration
{
    internal class OverrideConfiguration<TToken> : IConfiguration<TToken>
    {
        private readonly Func<TToken, SourcePos, SourcePos> _posCalculator;

        public OverrideConfiguration(
            IConfiguration<TToken> next,
            Func<TToken, SourcePos, SourcePos>? posCalculator = null
        )
        {
            _posCalculator = posCalculator ?? CopyFrom(next, n => n._posCalculator, n => n.CalculateSourcePos);
        }

        public SourcePos CalculateSourcePos(TToken token, SourcePos previous)
            => _posCalculator(token, previous);

        private static T CopyFrom<T>(
            IConfiguration<TToken> next,
            Func<OverrideConfiguration<TToken>, T> privateFieldSelector,
            Func<IConfiguration<TToken>, T> publicFieldSelector
        ) => next is OverrideConfiguration<TToken> o
            ? privateFieldSelector(o)
            : publicFieldSelector(next);
    }
}