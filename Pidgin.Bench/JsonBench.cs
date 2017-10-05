using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Pidgin.Bench.SpracheParsers;
using Pidgin.Bench.SuperpowerParsers;
using Pidgin.Examples.Json;

namespace Pidgin.Bench
{
    [Config(typeof(Config))]
    public class JsonBench
    {
        private string _bigJson;
        private string _longJson;
        private string _wideJson;
        private string _deepJson;

        [GlobalSetup]
        public void Setup()
        {
            _bigJson = BuildJson(4, 4, 3).ToString();
            _longJson = BuildJson(256, 1, 1).ToString();
            _wideJson = BuildJson(1, 1, 256).ToString();
            _deepJson = BuildJson(1, 256, 1).ToString();
        }

        [Benchmark]
        public void BigJson_Pidgin()
        {
            JsonParser.Parse(_bigJson);
        }
        [Benchmark]
        public void BigJson_Sprache()
        {
            SpracheJsonParser.Parse(_bigJson);
        }
        [Benchmark]
        public void BigJson_Superpower()
        {
            SuperpowerJsonParser.Parse(_bigJson);
        }
        [Benchmark]
        public void BigJson_FParsec()
        {
            Pidgin.Bench.FParsec.JsonParser.parse(_bigJson);
        }

        [Benchmark]
        public void LongJson_Pidgin()
        {
            JsonParser.Parse(_longJson);
        }
        [Benchmark]
        public void LongJson_Sprache()
        {
            SpracheJsonParser.Parse(_longJson);
        }
        [Benchmark]
        public void LongJson_Superpower()
        {
            SuperpowerJsonParser.Parse(_longJson);
        }
        [Benchmark]
        public void LongJson_FParsec()
        {
            Pidgin.Bench.FParsec.JsonParser.parse(_longJson);
        }

        [Benchmark]
        public void DeepJson_Pidgin()
        {
            JsonParser.Parse(_deepJson);
        }
        [Benchmark]
        public void DeepJson_Sprache()
        {
            SpracheJsonParser.Parse(_deepJson);
        }
        // this one blows the stack
        // [Benchmark]
        // public void DeepJson_Superpower()
        // {
        //     SuperpowerJsonParser.Parse(_deepJson);
        // }
        [Benchmark]
        public void DeepJson_FParsec()
        {
            Pidgin.Bench.FParsec.JsonParser.parse(_deepJson);
        }

        [Benchmark]
        public void WideJson_Pidgin()
        {
            JsonParser.Parse(_wideJson);
        }
        [Benchmark]
        public void WideJson_Sprache()
        {
            SpracheJsonParser.Parse(_wideJson);
        }
        [Benchmark]
        public void WideJson_Superpower()
        {
            SuperpowerJsonParser.Parse(_wideJson);
        }
        [Benchmark]
        public void WideJson_FParsec()
        {
            Pidgin.Bench.FParsec.JsonParser.parse(_wideJson);
        }
        
        private static IJson BuildJson(int length, int depth, int width)
            => new JsonArray(
                Enumerable.Repeat(1, length)
                    .Select(_ => BuildObject(depth, width))
                    .ToImmutableArray()
            );
        private static IJson BuildObject(int depth, int width)
        {
            if (depth == 0)
            {
                return new JsonString(RandomString(6));
            }
            return new JsonObject(
                Enumerable.Repeat(1, width)
                    .Select(_ => new KeyValuePair<string, IJson>(RandomString(5), BuildObject(depth-1, width)))
                    .ToImmutableDictionary()
            );
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}