using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public static partial class Parser
{
    internal static R Unbox<TToken, T, R>(this IUnboxer<TToken, T, R> unboxer, Parser<TToken, T> p)
        => p.Accept(unboxer);
}

public partial class Parser<TToken, T>
{
    internal virtual R Accept<R>(IUnboxer<TToken, T, R> unboxer)
        => BoxParser<TToken, T>.Create(this).Accept(unboxer);
}

internal interface IUnboxer<TToken, T, R>
{
    R Unbox<TImpl>(BoxParser<TToken, T>.Of<TImpl> box)
        where TImpl : IParser<TToken, T>;
}

internal abstract class BoxParser<TToken, T> : Parser<TToken, T>
{
    public static Of<TImpl> Create<TImpl>(TImpl impl)
        where TImpl : IParser<TToken, T>
        => new(impl);

    public class Of<TImpl> : BoxParser<TToken, T>
        where TImpl : IParser<TToken, T>
    {
        // needs to be public and non-readonly
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "needs to be public")]
        public TImpl Value;

        public Of(TImpl value)
        {
            Value = value;
        }

        public override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, [MaybeNullWhen(false)] out T result)
            => Value.TryParse(ref state, ref expecteds, out result);

        internal override R Accept<R>(IUnboxer<TToken, T, R> unboxer)
            => unboxer.Unbox(this);
    }
}
