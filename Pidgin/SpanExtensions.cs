using System;

namespace Pidgin
{
    internal static class SpanExtensions
    {
        public static unsafe string ToString(this ReadOnlySpan<char> span)
        {
            fixed (char* p = span)
            {
                return new string(p, 0, span.Length);
            }
        }
    }
}