using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;
using static System.Linq.Expressions.Expression;

namespace Pidgin.Tests
{
    public class ArgumentNullTests
    {
        [Theory]
        [MemberData(nameof(GetStaticClassParserMethods))]
        public void TestNullArgumentsOnStaticClassParser(MethodInfo method, object[] args)
        {
            Assert.Single(args.Where(x => x == null));
            var paramName = method.GetParameters()[Array.FindIndex(args, x => x == null)].Name;
            Assert.Throws<ArgumentNullException>(paramName, () => InvokeStaticMethod(method, args));
        }
        [Theory]
        [MemberData(nameof(GetGenericStaticClassParserMethods))]
        public void TestNullArgumentsOnGenericStaticClassParser(MethodInfo method, object[] args)
        {
            Assert.Single(args.Where(x => x == null));
            var paramName = method.GetParameters()[Array.FindIndex(args, x => x == null)].Name;
            Assert.Throws<ArgumentNullException>(paramName, () => InvokeStaticMethod(method, args));
        }
        [Theory]
        [MemberData(nameof(GetClassParserMethods))]
        public void TestNullArgumentsOnClassParser(MethodInfo method, object[] args)
        {
            Assert.Single(args.Where(x => x == null));
            var paramName = method.GetParameters()[Array.FindIndex(args, x => x == null)].Name;
            Assert.Throws<ArgumentNullException>(paramName, () => InvokeMethod(new AParser<int, int>(), method, args));
        }

        private void InvokeStaticMethod(MethodInfo method, object[] args)
        {
            InvokeMethod(null, method, args);
        }
        private void InvokeMethod(object? obj, MethodInfo method, object[] args)
        {
            try
            {
                method.Invoke(obj, args);
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException!).Throw();
            }
        }

        public static IEnumerable<object[]> GetStaticClassParserMethods() =>
            from m in typeof(Parser).GetMethods(BindingFlags.Public | BindingFlags.Static)
            let ps = m.GetParameters()
            where ps.Any()
            from args in GetArgs(ps)
            select new object[]
            {
                m.IsGenericMethod
                    ? m.MakeGenericMethod(m.GetGenericArguments().Select(_ => typeof(int)).ToArray())
                    : m,
                args
            };
        public static IEnumerable<object[]> GetGenericStaticClassParserMethods() =>
            from m in typeof(Parser<int>).GetMethods(BindingFlags.Public | BindingFlags.Static)
            let ps = m.GetParameters()
            where ps.Any()
            where m.Name != "Sequence"  // todo
                && m.Name != "Return"
                && m.Name != "FromResult"
            from args in GetArgs(ps)
            select new object[]
            {
                m.IsGenericMethod
                    ? m.MakeGenericMethod(m.GetGenericArguments().Select(_ => typeof(int)).ToArray())
                    : m,
                args
            };
        public static IEnumerable<object[]> GetClassParserMethods() =>
            from m in typeof(Parser<int, int>).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            let ps = m.GetParameters()
            where ps.Any()
            where !new[]{ "Parse", "Equals", "WithResult", "ThenReturn" }.Contains(m.Name)
            from args in GetArgs(ps)
            select new object[]
            {
                m.IsGenericMethod
                    ? m.MakeGenericMethod(m.GetGenericArguments().Select(_ => typeof(int)).ToArray())
                    : m,
                args
            };

        private static IEnumerable<object?[]> GetArgs(ParameterInfo[] parameters)
            => Cat(
                Enumerable.Repeat(parameters, parameters.Length)
                    .Select(
                        (ps, i) => Sequence(
                            ps.Select((p, j) => GetArg(p.ParameterType, i == j))
                        ).Select(xs => xs.ToArray())
                    )
            );
        
        private static Maybe<IEnumerable<T>> Sequence<T>(IEnumerable<Maybe<T>> maybes)
            => maybes.All(x => x.HasValue)
                ? Maybe.Just(maybes.Select(x => x.Value))
                : Maybe.Nothing<IEnumerable<T>>();
        
        private static IEnumerable<T> Cat<T>(IEnumerable<Maybe<T>> maybes)
            => maybes.Where(x => x.HasValue).Select(x => x.Value);
        
        private static Maybe<object?> GetArg(Type parameterType, bool shouldBeNull)
        {
            if (shouldBeNull)
            {
                if (parameterType.GetTypeInfo().IsValueType)
                {
                    // can't return null
                    return Maybe.Nothing<object?>();
                }
                return Maybe.Just<object?>(null);
            }
            var parserArgs = GetParserArgs(parameterType);
            if (parserArgs != null)
            {
                var genericArgs = parserArgs
                    .Select(a => a.IsGenericParameter ? typeof(int) : a)
                    .ToArray();
                return Maybe.Just(Activator.CreateInstance(typeof(AParser<,>).MakeGenericType(genericArgs)));
            }
            var funcArgs = GetFuncArgs(parameterType);
            if (funcArgs != null)
            {
                var genericArgs = funcArgs
                    .Select(a => a.IsGenericParameter ? typeof(int) : a)
                    .ToArray();
                var (args, ret) = (genericArgs.Take(genericArgs.Length - 1), genericArgs[genericArgs.Length -1]);
                var ps = args.Select(a => Parameter(a));
                var returnValue = ((dynamic)GetArg(ret, false)).Value;
                return Maybe.Just<object?>(Lambda(Constant(returnValue), ps).Compile());
            }
            if (parameterType.Equals(typeof(string)))
            {
                return Maybe.Just<object?>("");
            }
            return Maybe.Just(Activator.CreateInstance(parameterType));
        }

        private static Type[]? GetParserArgs(Type type)
        {
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition().Equals(typeof(Parser<,>)))
            {
                return type.GenericTypeArguments;
            }
            var baseType = type.GetTypeInfo().BaseType;
            if (baseType == null)
            {
                return null;
            }
            return GetParserArgs(baseType);
        }

        private static Type[]? GetFuncArgs(Type type)
        {
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition().FullName!.StartsWith("System.Func"))
            {
                return type.GenericTypeArguments;
            }
            return null;
        }


        private class AParser<TToken, T> : Parser<TToken, T>
        {
            public AParser() { }

            internal override bool TryParse(ref ParseState<TToken> state, ref PooledList<Expected<TToken>> expecteds, out T result)
            {
                throw new NotImplementedException();
            }
        }
    }
}