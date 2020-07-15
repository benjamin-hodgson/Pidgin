using System.Buffers;
using Microsoft.Extensions.ObjectPool;

namespace Pidgin
{
    internal partial class ExpectedCollector<TToken>
    {
        public class PooledObjectPolicy : PooledObjectPolicy<ExpectedCollector<TToken>>
        {
            private ArrayPool<Expected<TToken>> _arrayPool;

            public PooledObjectPolicy(ArrayPool<Expected<TToken>> arrayPool)
            {
                _arrayPool = arrayPool;
            }

            public override ExpectedCollector<TToken> Create() => new ExpectedCollector<TToken>(_arrayPool);

            public override bool Return(ExpectedCollector<TToken> obj)
            {
                // todo: can we return the array to the pool less frequently?
                obj.Dispose();
                return true;
            }
        }
    }
}