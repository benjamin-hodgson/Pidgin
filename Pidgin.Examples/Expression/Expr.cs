using System;
using System.Collections.Immutable;
using System.Linq;

namespace Pidgin.Examples.Expression
{
    public interface IExpr : IEquatable<IExpr>
    {
    }

    public class Identifier : IExpr
    {
        public string Name { get; }

        public Identifier(string name)
        {
            Name = name;
        }

        public bool Equals(IExpr? other)
            => other is Identifier i && Name == i.Name;
    }

    public class Literal : IExpr
    {
        public int Value { get; }

        public Literal(int value)
        {
            Value = value;
        }

        public bool Equals(IExpr? other)
            => other is Literal l && Value == l.Value;
    }

    public class Call : IExpr
    {
        public IExpr Expr { get; }
        public ImmutableArray<IExpr> Arguments { get; }

        public Call(IExpr expr, ImmutableArray<IExpr> arguments)
        {
            Expr = expr;
            Arguments = arguments;
        }

        public bool Equals(IExpr? other)
            => other is Call c
            && Expr.Equals(c.Expr)
            && Arguments.SequenceEqual(c.Arguments);
    }

    public enum UnaryOperatorType
    {
        Neg,
        Complement
    }
    public class UnaryOp : IExpr
    {
        public UnaryOperatorType Type { get; }
        public IExpr Expr { get; }

        public UnaryOp(UnaryOperatorType type, IExpr expr)
        {
            Type = type;
            Expr = expr;
        }

        public bool Equals(IExpr? other)
            => other is UnaryOp u
            && Type == u.Type
            && Expr.Equals(u.Expr);
    }

    public enum BinaryOperatorType
    {
        Add,
        Mul
    }
    public class BinaryOp : IExpr
    {
        public BinaryOperatorType Type { get; }
        public IExpr Left { get; }
        public IExpr Right { get; }

        public BinaryOp(BinaryOperatorType type, IExpr left, IExpr right)
        {
            Type = type;
            Left = left;
            Right = right;
        }

        public bool Equals(IExpr? other)
            => other is BinaryOp b
            && Type == b.Type
            && Left.Equals(b.Left)
            && Right.Equals(b.Right);
    }
}
