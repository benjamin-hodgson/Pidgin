using System;
using System.Runtime.CompilerServices;

namespace Pidgin.TokenStreams
{
    internal static class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void* AsPointer<T>(ref ReadOnlySpan<T> span)
        {
            throw null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ref ReadOnlySpan<T> AsRef<T>(void* ptr)
        {
            throw null;
        }
    }
}
