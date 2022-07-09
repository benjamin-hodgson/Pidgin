using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Pidgin.Examples.Expression;

public abstract class Expr : IEquatable<Expr>
{
    public abstract bool Equals(Expr? other);

    public override bool Equals(object? obj) => Equals(obj as Expr);

    public abstract override int GetHashCode();
}

public class Identifier : Expr
{
    public string Name { get; }

    public Identifier(string name)
    {
        Name = name;
    }

    public override bool Equals(Expr? other)
        => other is Identifier i && Name == i.Name;

    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
}

public class Literal : Expr
{
    public int Value { get; }

    public Literal(int value)
    {
        Value = value;
    }

    public override bool Equals(Expr? other)
        => other is Literal l && Value == l.Value;

    public override int GetHashCode() => Value;
}

[SuppressMessage("naming", "CA1716")]  // "Rename type so that it no longer conflicts with a reserved language keyword"
public class Call : Expr
{
    public Expr Expr { get; }
    public ImmutableArray<Expr> Arguments { get; }

    public Call(Expr expr, ImmutableArray<Expr> arguments)
    {
        Expr = expr;
        Arguments = arguments;
    }

    public override bool Equals(Expr? other)
        => other is Call c
        && Expr.Equals(c.Expr)
        && Arguments.SequenceEqual(c.Arguments);

    public override int GetHashCode() => HashCode.Combine(Expr, Arguments);
}

public enum UnaryOperatorType
{
    Neg,
    Complement
}
public class UnaryOp : Expr
{
    public UnaryOperatorType Type { get; }
    public Expr Expr { get; }

    public UnaryOp(UnaryOperatorType type, Expr expr)
    {
        Type = type;
        Expr = expr;
    }

    public override bool Equals(Expr? other)
        => other is UnaryOp u
        && Type == u.Type
        && Expr.Equals(u.Expr);

    public override int GetHashCode() => HashCode.Combine(Type, Expr);
}

public enum BinaryOperatorType
{
    Add,
    Mul
}
public class BinaryOp : Expr
{
    public BinaryOperatorType Type { get; }
    public Expr Left { get; }
    public Expr Right { get; }

    public BinaryOp(BinaryOperatorType type, Expr left, Expr right)
    {
        Type = type;
        Left = left;
        Right = right;
    }

    public override bool Equals(Expr? other)
        => other is BinaryOp b
        && Type == b.Type
        && Left.Equals(b.Left)
        && Right.Equals(b.Right);

    public override int GetHashCode() => HashCode.Combine(Type, Left, Right);
}
