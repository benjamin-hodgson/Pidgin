using System;
using Microsoft.Extensions.ObjectPool;

namespace Pidgin.Configuration
{
    internal class OverrideConfiguration<TToken> : IConfiguration<TToken>
    {
        public Func<TToken, SourcePos, SourcePos> SourcePosCalculator { get; }
        public IArrayPoolProvider ArrayPoolProvider { get; }
        public ObjectPoolProvider ObjectPoolProvider { get; }

        public OverrideConfiguration(
            IConfiguration<TToken> next,
            Func<TToken, SourcePos, SourcePos>? posCalculator = null,
            IArrayPoolProvider? arrayPoolProvider = null,
            ObjectPoolProvider? objectPoolProvider = null
        )
        {
            SourcePosCalculator = posCalculator ?? next.SourcePosCalculator;
            ArrayPoolProvider = arrayPoolProvider ?? next.ArrayPoolProvider;
            ObjectPoolProvider = objectPoolProvider ?? next.ObjectPoolProvider;
        }
    }
}