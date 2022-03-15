using System;
using System.Collections.Generic;
using System.Linq;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// Creates a parser that parses and returns one of enum values
        /// </summary>
        /// <param name="ignoreCase">Flag, true: parser  values in a case insensitive manner, false: case sensitive</param>
        /// <typeparam name="TEnum">Enum type</typeparam>
        /// <returns>A parser that parses and returns one of enum values</returns>
        public static Parser<char, TEnum> ParseEnum<TEnum>(bool ignoreCase = false)
            where TEnum : struct, Enum
        {
            return new EnumParser<TEnum>(ignoreCase);
        }

        /// <summary>
        /// Creates a parser that parses and returns one of enum values, exclude specified values
        /// </summary>
        /// <param name="ignoreCase">Flag, true: parser  values in a case insensitive manner, false: case sensitive</param>
        /// <param name="excluded">Values that will not be parsed</param>
        /// <typeparam name="TEnum">Enum type</typeparam>
        /// <returns>Creates a parser that parses and returns one of enum values, exclude specified values</returns>
        public static Parser<char, TEnum> ParseEnumExclude<TEnum>(bool ignoreCase, params TEnum[] excluded)
            where TEnum : struct, Enum
        {
            return new EnumParser<TEnum>(ignoreCase, excluded);
        }

        /// <summary>
        /// Creates a parser that parses and returns a enum value
        /// </summary>
        /// <param name="value">Enum value to parse</param>
        /// <param name="ignoreCase">Flag, true: parser  values in a case insensitive manner, false: case sensitive</param>
        /// <typeparam name="TEnum">Enum type</typeparam>
        /// <returns></returns>
        public static Parser<char, TEnum> ParseEnumValue<TEnum>(TEnum value, bool ignoreCase = false)
            where TEnum : struct, Enum
        {
            return String(value.ToString(), ignoreCase).Select(x => Enum.Parse<TEnum>(x, ignoreCase));
        }
    }

    internal class EnumParser<TEnum> : Parser<char, TEnum>
        where TEnum : struct, Enum
    {
        private readonly Parser<char, TEnum> _parser;

        public EnumParser(bool ignoreCase, params TEnum[] excluded)
        {
            _parser = Parser.OneOf(EnumHelper.GetNamesExcept(excluded)
                    .Select(value => Parser.String(value, ignoreCase))
                    .Select(Parser.Try))
                .Select(x => Enum.Parse<TEnum>(x, ignoreCase));
        }

        public override bool TryParse(ref ParseState<char> state, ref PooledList<Expected<char>> expecteds,
            out TEnum result)
        {
            return _parser.TryParse(ref state, ref expecteds, out result);
        }
    }

    internal static class EnumHelper
    {
        internal static string? GetName<TEnum>(TEnum value)
            where TEnum : struct, Enum
        {
            return Enum.GetName(value);
        }

        internal static IEnumerable<string> GetNamesExcept<TEnum>(params TEnum[] excludedElements)
            where TEnum : struct, Enum
        {
            return GetValueExcept(excludedElements).Select(GetName)!;
        }

        internal static IEnumerable<TEnum> GetValues<TEnum>()
            where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>();
        }

        internal static IEnumerable<TEnum> GetValueExcept<TEnum>(params TEnum[] excludedElements)
            where TEnum : struct, Enum
        {
            return GetValues<TEnum>().Where(x => !excludedElements.Contains(x));
        }
    }
}