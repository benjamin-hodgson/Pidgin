using System;
using System.Runtime.CompilerServices;

namespace Pidgin.TokenStreams
{
    internal unsafe readonly struct ReadOnlySpanReference<TToken>
    {
        private readonly void* _ptr;

        public ReadOnlySpanReference(ref ReadOnlySpan<TToken> span)
        {
            _ptr = Unsafe.AsPointer<TToken>(ref span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ReadOnlySpan<TToken> Get()
        {
            return ref Unsafe.AsRef<TToken>(_ptr);
        }

        public bool IsDefault() => new IntPtr(_ptr) == IntPtr.Zero;
    }
}
