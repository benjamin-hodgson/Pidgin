using System;

namespace Pidgin
{
    public delegate TReturn ReadOnlySpanFunc<TToken, in TParam, out TReturn>(ReadOnlySpan<TToken> span, TParam param);
}