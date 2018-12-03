using System;
using System.Runtime.CompilerServices;

namespace Pidgin.TokenStreams
{
    // These unsafe coercion methods are not expressible in C# directly, but they are expressible in (unverifiable) IL.
    // (See also System.Runtime.CompilerServices.Unsafe. That class doesn't work here because you can't use Span as a type parameter.)
    //
    // So we stub out the methods in C# and then rewrite the compiled DLL as a build step (see Pidgin.csproj).
    // The build step invokes Pidgin.DllRewriter.
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
