using System;
using Pidgin.Examples.Expr;
using Xunit;
using static Pidgin.Examples.Expr.ExprParser;
using static Pidgin.Parser;

namespace Pidgin.Tests
{
    public class ExprExampleTests : ParserTestBase
    {
        [Fact]
        public void TestTerminals()
        {
            AssertSuccess(LBrace.Parse("{"), '{', true);
            AssertSuccess(RBrace.Parse("}"), '}', true);
            AssertSuccess(LParen.Parse("("), '(', true);
            AssertSuccess(RParen.Parse(")"), ')', true);
            AssertSuccess(Comma.Parse(","), ',', true);
            AssertSuccess(UScore.Parse("_"), '_', true);
            AssertSuccess(DQuote.Parse("\""), '"', true);
            AssertSuccess(SQuote.Parse("'"), '\'', true);

            AssertSuccess(Str.Parse("\"abc\""), "abc", true);
            AssertSuccess(Chr.Parse("'a'"), "a", true);

            AssertSuccess(ExprParser.Int.Parse("234"), "234", true);
            AssertSuccess(Float.Parse("234.0"), "234.0", true);
            AssertSuccess(Float.Parse(".0"), ".0", true);
            AssertSuccess(Float.Parse("1e2"), "1e2", true);
            AssertSuccess(Float.Parse("1E2"), "1E2", true);
            AssertSuccess(Float.Parse("1E-2"), "1E-2", true);
            AssertSuccess(Float.Parse("1E+2"), "1E+2", true);
            AssertSuccess(Float.Parse("1.0E2"), "1.0E2", true);
            AssertSuccess(Float.Parse("1.0E-2"), "1.0E-2", true);
            AssertSuccess(Float.Parse("1.0E+2"), "1.0E+2", true);


            AssertSuccess(IdStart.Parse("_"), '_', true);
            AssertSuccess(IdStart.Parse("a"), 'a', true);
            AssertSuccess(IdRest.Parse("_"), '_', true);
            AssertSuccess(IdRest.Parse("1"), '1', true);
            AssertSuccess(IdRest.Parse("_"), '_', true);
            AssertSuccess(IdRest.Parse("a"), 'a', true);
            AssertSuccess(Id.Parse("_"), "_", true);
            AssertSuccess(Id.Parse("A"), "A", true);
            AssertSuccess(Id.Parse("z"), "z", true);
            AssertSuccess(Id.Parse("abc"), "abc", true);
            AssertSuccess(Id.Parse("_abc"), "_abc", true);
            AssertSuccess(Id.Parse("_1"), "_1", true);
            AssertSuccess(Id.Parse("_1_a_zZ"), "_1_a_zZ", true);
            AssertSuccess(Id.Parse("e"), "e", true);
            AssertSuccess(Id.Parse("el"), "el", true);
            AssertSuccess(Id.Parse("els"), "els", true);
            AssertSuccess(Id.Parse("else1"), "else1", true);
            AssertSuccess(Id.Parse("i"), "i", true);
            AssertSuccess(Id.Parse("if1"), "if1", true);
            AssertSuccess(Id.Parse("if_"), "if_", true);
            AssertSuccess(Id.Parse("t"), "t", true);
            AssertSuccess(Id.Parse("tr"), "tr", true);

            AssertSuccess(True.Parse("true"), "true", true);
            AssertSuccess(False.Parse("false"), "false", true);
            AssertSuccess(If.Parse("if"), "if", true);
            AssertSuccess(Else.Parse("else"), "else", true);
            AssertSuccess(Then.Parse("then"), "then", true);
            AssertSuccess(ExprParser.In.Parse("in"), "in", true);

            AssertSuccess(Bool.Parse("true"), "true", true);
            AssertSuccess(Bool.Parse("false"), "false", true);
            AssertSuccess(Bool.Parse("\"true\""), "true", true);
            AssertSuccess(Bool.Parse("\"false\""), "false", true);

            AssertSuccess(KeyWords.Parse("else"), "else", true);
            AssertSuccess(KeyWords.Parse("then"), "then", true);
            AssertSuccess(KeyWords.Parse("if"), "if", true);
            AssertSuccess(KeyWords.Parse("in "), "in", true);
        }

        [Fact]
        public void TestTerminalsFailures()
        {
            AssertFailure(Id.Parse("true"), false);
            AssertFailure(Id.Parse("false"), false);
            AssertFailure(Id.Parse("if"), false);
            AssertFailure(Id.Parse("then"), false);
            AssertFailure(Id.Parse("else"), false);

            AssertFailure(KeyWords.Parse("if1"), true);
            AssertFailure(KeyWords.Parse("_if"), false);
        }

        [Fact]
        public void TestExpr()
        {
            AssertSuccess(Parse("t1"), "t1", true);
            AssertSuccess(Parse("a"), "a", true);
            AssertSuccess(Parse("t1 + fa"), "t1+fa", true);
            AssertSuccess(Parse(" + 1 "), "+1", true);
            AssertSuccess(Parse("a+b"), "a+b", true);
            AssertSuccess(Parse("!i"), "!i", true);
            AssertSuccess(Parse("+a "), "+a", true);
            AssertSuccess(Parse("+t1"), "+t1", true);
            AssertSuccess(Parse("-f"), "-f", true);
            AssertSuccess(Parse("+t"), "+t", true);
            AssertSuccess(Parse("+t-f"), "+t-f", true);
            AssertSuccess(Parse("+t-f/3*a^2"), "+t-f/3*a^2", true);
            AssertSuccess(Parse("+t--f/-3*+a^2"), "+t--f/-3*+a^2", true);
            AssertSuccess(Parse("+t--f/-3*+a^-2"), "+t--f/-3*+a^-2", true);
            AssertSuccess(Parse("(+t--f)/(-3)*(+a^-2)"), "(+t--f)/(-3)*(+a^-2)", true);
            AssertSuccess(Parse("((+t-(-f))/(-3)*(+a^-2))"), "((+t-(-f))/(-3)*(+a^-2))", true);
            AssertSuccess(Parse("2+3 in {1,a, c}"), "(new object[]{1,a,c}).Contains(2+3)", true);
            AssertSuccess(Parse("2+3 in {1,a, c} && (5+(ii^ff))"), "(new object[]{1,a,c}).Contains(2+3)&&(5+(ii^ff))", true);
            AssertSuccess(Parse("g()||2+3 in {1,a, c} && (5+(ii^ff))"),
                "g()||(new object[]{1,a,c}).Contains(2+3)&&(5+(ii^ff))", true);
            AssertSuccess(Parse("g(h(1), i(2,3&&false) ) || 2+3 in {1,a, c} && (5+(ii^ff))"),
                "g(h(1),i(2,3&&false))||(new object[]{1,a,c}).Contains(2+3)&&(5+(ii^ff))", true);
            AssertSuccess(Parse("if(trUe(1), Then(2,3&&false) ) || 2+3 in {1,a, c} && (5+(ii^ff))"),
                "if(trUe(1),Then(2,3&&false))||(new object[]{1,a,c}).Contains(2+3)&&(5+(ii^ff))", true);
            AssertSuccess(Parse("if(trUe(1), Then(2,3&&false) ) || 2+3 in {1,a, c} && (5+(ii^ff))"),
                "if(trUe(1),Then(2,3&&false))||(new object[]{1,a,c}).Contains(2+3)&&(5+(ii^ff))", true);

            AssertSuccess(Parse("34.5e-3if true else 99e2"), "(/*py*/ true?34.5e-3:99e2)", true);
            AssertSuccess(Parse("34.5e-3  if   true   else    99e2"), "(/*py*/ true?34.5e-3:99e2)", true);
            AssertSuccess(Parse("t1  if   c1   else    t2 if c2 else f2"), "(/*py*/ c1?t1:(/*py*/ c2?t2:f2))", true);

            AssertSuccess(Parse("34.5e-3?true:99e2"), "(/*c*/ 34.5e-3?true:99e2)", true);
            AssertSuccess(Parse("34.5e-3 ?true:99e2"), "(/*c*/ 34.5e-3?true:99e2)", true);
            AssertSuccess(Parse("34.5e-3? true:99e2"), "(/*c*/ 34.5e-3?true:99e2)", true);
            AssertSuccess(Parse("34.5e-3  ?  true:99e2"), "(/*c*/ 34.5e-3?true:99e2)", true);
            AssertSuccess(Parse("34.5e-3  ?  true :99e2"), "(/*c*/ 34.5e-3?true:99e2)", true);
            AssertSuccess(Parse("34.5e-3  ?  true: 99e2"), "(/*c*/ 34.5e-3?true:99e2)", true);
            AssertSuccess(Parse("34.5e-3  ?  true  :  99e2"), "(/*c*/ 34.5e-3?true:99e2)", true);
            AssertSuccess(Parse("a==b&&if(1)>d  ?  d<e  :  h>d"), "(/*c*/ a==b&&if(1)>d?d<e:h>d)", true);
            AssertSuccess(Parse("a!=b&&if(1)>=d  ?  d<=e  :  h>=d"), "(/*c*/ a!=b&&if(1)>=d?d<=e:h>=d)", true);

            AssertSuccess(Parse("1>2 ? 2>3 ? 4 : 2 : 1"), "(/*c*/ 1>2?(/*c*/ 2>3?4:2):1)", true);

            AssertSuccess(Parse("(t+f(if(c1?t1:f1, t1 if c1 else f1)))"), "(t+f(if((/*c*/ c1?t1:f1),(/*py*/ c1?t1:f1))))", true);
        }

        [Fact]
        public void TestTerExpr()
        {
            AssertSuccess(Parse("c1 ? c2 ? c3 ? t3 : f3 : f2 : f1"), "(/*c*/ c1?(/*c*/ c2?(/*c*/ c3?t3:f3):f2):f1)", true);
            AssertSuccess(Parse("c1 ? t1 : c2 ? t2 : f2"), "(/*c*/ c1?t1:(/*c*/ c2?t2:f2))", true);
            AssertSuccess(Parse("c1 ? t1 : c2 ? t2 : c3 ? t3:f3"), "(/*c*/ c1?t1:(/*c*/ c2?t2:(/*c*/ c3?t3:f3)))", true);
            AssertSuccess(Parse("c1 ? c2 ? t2 :f2 : c3 ? t3:f3"), "(/*c*/ c1?(/*c*/ c2?t2:f2):(/*c*/ c3?t3:f3))", true);
            AssertSuccess(Parse("c1 ? (c2 ? t2 :c3 ? t3:f3) : c3 ? t3:f3"), "(/*c*/ c1?((/*c*/ c2?t2:(/*c*/ c3?t3:f3))):(/*c*/ c3?t3:f3))", true);

            AssertSuccess(Parse("t1  if   c1   else    t2 if c2 else t3 if c3 else f3"), "(/*py*/ c1?t1:(/*py*/ c2?t2:(/*py*/ c3?t3:f3)))", true);
            AssertSuccess(Parse("t1 if t2 if c2 else f2 else f1"), "(/*py*/ (/*py*/ c2?t2:f2)?t1:f1)", true);
        }

        [Fact]
        public void TestTerExprFailures()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Parse("c1 ? t1"));
            Assert.Equal("Unbalanced C-style Ternary Operators, fewer 'else' parts than 'then' parts", ex.Message);
            ex = Assert.Throws<InvalidOperationException>(() => Parse("c1 ? t1 : f1 : f2"));
            Assert.Equal("Unbalanced C-style Ternary Operators, still have 1 unmatched.", ex.Message);
            ex = Assert.Throws<InvalidOperationException>(() => Parse("c1 ? c2 ? f1 : f2"));
            Assert.Equal("Unbalanced C-style Ternary Operators, fewer 'else' parts than 'then' parts", ex.Message);
            ex = Assert.Throws<InvalidOperationException>(() => Parse("if (c1 ? c2 ? f1 : f2)+ a : b"));
            Assert.Equal("Unbalanced C-style Ternary Operators, fewer 'else' parts than 'then' parts", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() => Parse("c1 if t1"));
            Assert.Equal("Unbalanced Python-style Ternary Operators, fewer 'else' parts than 'then' parts", ex.Message);
            ex = Assert.Throws<InvalidOperationException>(() => Parse("c1 if t1 else f1 else f2"));
            Assert.Equal("Unbalanced Python-style Ternary Operators, still have 1 unmatched.", ex.Message);
            ex = Assert.Throws<InvalidOperationException>(() => Parse("c1 if t2 if c2 else f2"));
            Assert.Equal("Unbalanced Python-style Ternary Operators, fewer 'else' parts than 'then' parts", ex.Message);
            ex = Assert.Throws<InvalidOperationException>(() => Parse("if (c1 if t2 if c2 else f2)+ a else b"));
            Assert.Equal("Unbalanced Python-style Ternary Operators, fewer 'else' parts than 'then' parts", ex.Message);
        }
    }
}