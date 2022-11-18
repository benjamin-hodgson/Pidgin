using System;

namespace Pidgin;

/// <summary>
/// A function which computes a result from a <see cref="ReadOnlySpan{TToken}"/> and an additional argument.
/// </summary>
/// <param name="span">The input span.</param>
/// <param name="param">An additional argument.</param>
/// <typeparam name="T">The type of elements of the span.</typeparam>
/// <typeparam name="TParam">The type of the additional argument.</typeparam>
/// <typeparam name="TReturn">The type of the result computed by the function.</typeparam>
/// <returns>The result.</returns>
public delegate TReturn ReadOnlySpanFunc<T, in TParam, out TReturn>(ReadOnlySpan<T> span, TParam param);
