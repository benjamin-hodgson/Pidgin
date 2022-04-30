using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Farkle;
using Farkle.Builder;
using Pidgin.Examples.Json;

namespace Pidgin.Bench.FarkleParsers
{
    public static class FarkleJsonParser
    {
        private sealed class ImmutableArrayBuilder<T> : ICollection<T>
        {
            private readonly ImmutableArray<T>.Builder _builder = ImmutableArray.CreateBuilder<T>();
            public IEnumerator<T> GetEnumerator() => _builder.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_builder).GetEnumerator();
            public void Add(T item) => _builder.Add(item);
            public void Clear() => _builder.Clear();
            public bool Contains(T item) => _builder.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _builder.CopyTo(array, arrayIndex);
            public bool Remove(T item) => _builder.Remove(item);
            public int Count => _builder.Count;
            public bool IsReadOnly => ((ICollection<T>) _builder).IsReadOnly;

            public ImmutableArray<T> ToImmutable() => _builder.ToImmutable();
        }

        private sealed class ImmutableDictionaryBuilder<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>> where TKey: notnull
        {
            private readonly ImmutableDictionary<TKey, TValue>.Builder _builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _builder.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_builder).GetEnumerator();
            public void Add(KeyValuePair<TKey, TValue> item) => _builder.Add(item);
            public void Clear() => _builder.Clear();
            public bool Contains(KeyValuePair<TKey, TValue> item) => _builder.Contains(item);
            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_builder).CopyTo(array, arrayIndex);
            public bool Remove(KeyValuePair<TKey, TValue> item) => _builder.Remove(item);
            public int Count => _builder.Count;
            public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)_builder).IsReadOnly;

            public ImmutableDictionary<TKey, TValue> ToImmutable() => _builder.ToImmutable();
        }

        private static DesigntimeFarkle<TTo> SelectBetween<TFrom, TTo>(this DesigntimeFarkle<TFrom> df, string from, string to, Func<TFrom, TTo> f) where TTo : notnull =>
            Nonterminal.Create(df.Name, from.Appended().Extend(df).Append(to).Finish(f));

        public static readonly DesigntimeFarkle<IJson> Designtime;
        public static readonly RuntimeFarkle<IJson> Runtime;

        static FarkleJsonParser()
        {
            var json = Nonterminal.Create<IJson>("JSON");
            var @string = Terminal.Create("String",
                (_, data) => data[1..^1].ToString(),
                Regex.FromRegexString("\"[^\"]*\""));

            var jsonArray =
                json.SeparatedBy<IJson, ImmutableArrayBuilder<IJson>>(Terminal.Literal(","), true)
                    .Rename("Array Body")
                    .Optional()
                    .SelectBetween("[", "]", x => (IJson) new JsonArray(x?.ToImmutable() ?? ImmutableArray<IJson>.Empty))
                    .Rename("Array");

            var jsonObject =
                Nonterminal.Create("Object Member",
                        @string.Extended().Append(":").Extend(json).Finish(KeyValuePair.Create))
                    .SeparatedBy<KeyValuePair<string, IJson>, ImmutableDictionaryBuilder<string, IJson>>(Terminal.Literal(","), true)
                    .Rename("Object Body")
                    .Optional()
                    .SelectBetween("{", "}", x => (IJson) new JsonObject(x?.ToImmutable() ?? ImmutableDictionary<string, IJson>.Empty))
                    .Rename("Object");

            json.SetProductions(
                @string.Extended().Finish(x => (IJson)new JsonString(x)),
                jsonArray.AsIs(),
                jsonObject.AsIs());

            Designtime = json.CaseSensitive();
            Runtime = Designtime.Build();
        }

        public static IJson Parse(string str)
        {
            var result = Runtime.Parse(str);
            if (result.IsOk) return result.ResultValue;
            throw new Exception(result.ErrorValue.ToString());
        }
    }
}