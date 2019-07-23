using System;

namespace Pidgin
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="span"></param>
    /// <typeparam name="TToken"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public delegate T ReadOnlySpanFunc<TToken, T>(ReadOnlySpan<TToken> span);
}