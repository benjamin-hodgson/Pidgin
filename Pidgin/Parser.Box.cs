using System.Diagnostics.CodeAnalysis;

namespace Pidgin;

public partial class Parser<TToken, T>
{
    internal BoxParser<TToken, R> Accept<R>(IReboxer<TToken, T, R> reboxer)
        => this is BoxParser<TToken, T> b
            ? b.Rebox(reboxer)
            : BoxParser<TToken, T>.Create(this).Rebox(reboxer);
}

internal interface IReboxer<TToken, T, R>
{
    BoxParser<TToken, R> WithBox<TImpl>(BoxParser<TToken, T>.Of<TImpl> box)
        where TImpl : IParser<TToken, T>;
}

internal abstract class BoxParser<TToken, T> : Parser<TToken, T>
{
    public static Of<TImpl> Create<TImpl>(TImpl impl)
        where TImpl : IParser<TToken, T>
        => new(impl);

    public abstract BoxParser<TToken, R> Rebox<R>(IReboxer<TToken, T, R> reboxer);

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

        public override BoxParser<TToken, R> Rebox<R>(IReboxer<TToken, T, R> reboxer)
            => reboxer.WithBox(this);
    }
}
