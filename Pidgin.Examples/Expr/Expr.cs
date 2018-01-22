using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pidgin.Examples.Expr
{
    public abstract class Expr : IEquatable<Expr>
    {
        public override bool Equals(object other) => other is Expr && Equals((Expr) other);

        public bool Equals(Expr other)
        {
            // I had a normal recursive-virtual-method implementation
            // but it blew the stack on big inputs
            var stack = new Stack<(Expr, Expr)>();
            stack.Push((this, other));
            while (stack.Any())
            {
                var (l, r) = stack.Pop();

                switch (l)
                {
                    case Lit l1 when r is Lit l2:
                        if (l1.Value != l2.Value) return false;
                        break;
                    case AUnaryOperator u1 when r is AUnaryOperator u2:
                        if (u1.Op != u2.Op) return false;
                        stack.Push((u1.Expr, u2.Expr));
                        break;
                    case ABinaryOperator b1 when r is ABinaryOperator b2:
                        if (b1.Op != b2.Op) return false;
                        stack.Push((b1.Left, b2.Left));
                        stack.Push((b1.Right, b2.Right));
                        break;
                    case TernaryIf t1 when r is TernaryIf t2:
                        if (t1.Op != t2.Op) return false;
                        if (t1.Op2 != t2.Op2) return false;
                        stack.Push((t1.CondExpr, t2.CondExpr));
                        stack.Push((t1.TrueExpr, t2.TrueExpr));
                        stack.Push((t1.FalseExpr, t2.FalseExpr));
                        break;
                    case Function f1 when r is Function f2:
                        if (f1.Name != f2.Name) return false;
                        for (var i = 0; i < f1.Params.Count; i++) stack.Push((f1.Params[i], f2.Params[i]));
                        break;
                    case Membership m1 when r is Membership m2:
                        for (var i = 0; i < m1.Members.Count; i++) stack.Push((m1.Members[i], m2.Members[i]));
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        public override int GetHashCode() => 0; // doesn't matter
        public static Expr Empty => new EmptyExpr();

        private class EmptyExpr : Expr { public override string ToString() => ""; }
    }

    #region derived clases for AST

    #region Literals
    public abstract class Lit : Expr
    {
        public string Value { get; }
        protected Lit(string value) { Value = value; }
        public override string ToString() => $"{Value}";
    }
    public class NumLit : Lit { public NumLit(string value) : base(value) { } }
    public class BoolLit : Lit { public BoolLit(string value) : base(value) { } public override string ToString() => $"{Value.ToLower()}"; }
    public class CharLit : Lit { public CharLit(string value) : base(value) { } public override string ToString() => $"'{Value}'"; }
    public class StringLit : Lit { public StringLit(string value) : base(value) { } public override string ToString() => $"\"{Value}\""; }
    public class Identifier : Lit { public Identifier(string value) : base(value) { } }
    #endregion Literals

    #region Operators
    public abstract class AOperator : Expr
    {
        public string Op { get; }
        protected AOperator(string op) { Op = op; }
        public override string ToString() => $"{Op}";
    }

    #region Unary Operators
    public abstract class AUnaryOperator : AOperator
    {
        public Expr Expr { get; }
        protected AUnaryOperator(string op, Expr expr) : base(op) { Expr = expr; }
        public override string ToString() => $"{Op}{Expr}";
    }
    public class Negative : AUnaryOperator { public Negative(Expr expr) : base("-", expr) { } }
    public class Positive : AUnaryOperator { public Positive(Expr expr) : base("+", expr) { } }
    public class Not : AUnaryOperator { public Not(Expr expr) : base("!", expr) { } }
    #endregion Unary Operators

    #region Binary Operators
    public abstract class ABinaryOperator : AOperator
    {
        public Expr Left { get; }
        public Expr Right { get; }
        protected ABinaryOperator(string op, Expr left, Expr right) : base(op) { Left = left; Right = right; }
        public override string ToString() => $"{Left}{Op}{Right}";
    }
    public class Plus : ABinaryOperator { public Plus(Expr left, Expr right) : base("+", left, right) { } }
    public class Minus : ABinaryOperator { public Minus(Expr left, Expr right) : base("-", left, right) { } }
    public class Mult : ABinaryOperator { public Mult(Expr left, Expr right) : base("*", left, right) { } }
    public class Div : ABinaryOperator { public Div(Expr left, Expr right) : base("/", left, right) { } }
    public class Power : ABinaryOperator { public Power(Expr left, Expr right) : base("^", left, right) { } }
    public class Mod : ABinaryOperator { public Mod(Expr left, Expr right) : base("%", left, right) { } }
    public class Cat : ABinaryOperator { public Cat(Expr left, Expr right) : base("&", left, right) { } public override string ToString() => $"{Left}+{Right}"; }
    public class Lt : ABinaryOperator { public Lt(Expr left, Expr right) : base("<", left, right) { } }
    public class Le : ABinaryOperator { public Le(Expr left, Expr right) : base("<=", left, right) { } }
    public class Gt : ABinaryOperator { public Gt(Expr left, Expr right) : base(">", left, right) { } }
    public class Ge : ABinaryOperator { public Ge(Expr left, Expr right) : base(">=", left, right) { } }
    public class Eq : ABinaryOperator { public Eq(Expr left, Expr right) : base("==", left, right) { } }
    public class Ne : ABinaryOperator { public Ne(Expr left, Expr right) : base("!=", left, right) { } public Ne(string op, Expr left, Expr right) : base(op, left, right) { } }
    public class In : ABinaryOperator { public In(Expr left, Expr right) : base("in", left, right) { } public override string ToString() => $"{Right}.Contains({Left})"; }
    public class And : ABinaryOperator { public And(Expr left, Expr right) : base("&&", left, right) { } }
    public class Or : ABinaryOperator { public Or(Expr left, Expr right) : base("||", left, right) { } }
    public class NullCoalesce : ABinaryOperator { public NullCoalesce(Expr left, Expr right) : base("??", left, right) { } }

    // ternary if
    public static class TerUtils
    {
        public static Expr FormCTerExpr(Expr left, Expr right)
        {
            if (!(right is CTerElseExpr pivot && pivot.Right is CTerElseExpr pivotChild)) return new CTerIfExpr(left,right);
            var c = new CTerElseExpr(pivot.Left, pivotChild.Left);
            return new CTerElseExpr(new CTerIfExpr(left, c), pivotChild.Right);
        }
        public static Expr FormPyTerExpr(Expr left, Expr right)
        {
            if (!(right is PyTerExprElse pivot && pivot.Right is PyTerExprElse pivotChild)) return new PyTerExprIf(left, right);
            var c = new PyTerExprElse(pivot.Left, pivotChild.Left);
            return new PyTerExprElse(new PyTerExprIf(left, c), pivotChild.Right);
        }
    }

    // C-style ternary if
    public class CTerIfExpr : ABinaryOperator
    {
        public CTerElseExpr CTerElseExpr { get; }
        public CTerIfExpr(Expr left, Expr right) : base("?", left, right)
        {
            CTerElseExpr = right as CTerElseExpr;
            if (CTerElseExpr == null) throw new ArgumentException($"Type of {nameof(right)} excepted to be {nameof(CTerElseExpr)} but is {right.GetType().FullName}");
        }

        public override string ToString() => $"(/*c*/ {Left}?{CTerElseExpr?.Left}:{CTerElseExpr?.Right})";
    }

    public class CTerElseExpr : ABinaryOperator
    {
        public CTerElseExpr(Expr left, Expr right) : base(":", left, right) { }
    }

    // python ternary if
    public class PyTerExprIf : ABinaryOperator
    {
        public PyTerExprElse PyTerExprElse { get; }
        public PyTerExprIf(Expr left, Expr right) : base(" _pyif_ ", left, right)
        {
            PyTerExprElse = right as PyTerExprElse;
            if (PyTerExprElse == null) throw new ArgumentException($"Type of {nameof(right)} excepted to be {nameof(PyTerExprElse)} but is {right.GetType().FullName}");
        }
        public override string ToString() => $"(/*py*/ {PyTerExprElse.Left}?{Left}:{PyTerExprElse.Right})";
    }
    public class PyTerExprElse : ABinaryOperator { public PyTerExprElse(Expr left, Expr right) : base("else", left, right) { } }
    #endregion Binary Operators

    #region Others
    public class TernaryIf : AOperator
    {
        public string Op2 { get; }
        public Expr CondExpr { get; }
        public Expr TrueExpr { get; }
        public Expr FalseExpr { get; }
        public TernaryIf(Expr condExpr, Expr trueExpr, Expr falseExpr) : this("?", ":", condExpr, trueExpr, falseExpr) { }
        public TernaryIf(string op, string op2, Expr condExpr, Expr trueExpr, Expr falseExpr) : base(op)
        {
            Op2 = op2 ?? ":";
            CondExpr = condExpr; TrueExpr = trueExpr; FalseExpr = falseExpr;
        }
        public override string ToString() => $"{CondExpr}{Op}{TrueExpr}{Op2}{FalseExpr}";
    }
    public class Function : Expr
    {
        public string Name { get; }
        public List<Expr> Params { get; }
        public Function(string name, IEnumerable<Expr> p) { Name = name; Params = p.ToList(); }
        public override string ToString() => $"{Name}({string.Join(",", Params)})";
    }
    public class Membership : Expr
    {
        public List<Expr> Members { get; }
        public Membership(IEnumerable<Expr> m) { Members = m.ToList(); }
        public override string ToString() => $"(new object[]{{{string.Join(",", Members)}}})";
    }
    public class Nested : Expr
    {
        public Expr Inner { get; }
        public Nested(Expr inner) { Inner = inner; }
        public override string ToString() => $"({Inner})";
    }

    #endregion Ternary Operators

    #endregion Operators

    #endregion derived clases for AST
}
