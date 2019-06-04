using System;
using System.Linq;
using System.Text;

namespace Pidgin
{
    public static partial class Parser
    {
        private static readonly Parser<char, string> SignString
            = Char('-').ThenReturn("-")
                .Or(Char('+').ThenReturn("+"))
                .Or(Parser<char>.Return(""));
        private static readonly Parser<char, int> Sign
            = Char('+').ThenReturn(1)
                .Or(Char('-').ThenReturn(-1))
                .Or(Parser<char>.Return(1));

        /// <summary>
        /// A parser which parses a base-10 integer with an optional sign.
        /// The resulting <c>int</c> is not checked for overflow.
        /// </summary>
        /// <returns>A parser which parses a base-10 integer with an optional sign</returns>
        public static Parser<char, int> DecimalNum { get; } = Int(10).Labelled("number");

        /// <summary>
        /// A parser which parses a base-10 integer with an optional sign.
        /// The resulting <c>int</c> is not checked for overflow.
        /// </summary>
        /// <returns>A parser which parses a base-10 integer with an optional sign</returns>
        public static Parser<char, int> Num { get; } = DecimalNum;

        /// <summary>
        /// A parser which parses a base-10 long integer with an optional sign.
        /// </summary>
        /// <returns>A parser which parses a base-10 long integer with an optional sign</returns>
        public static Parser<char, long> LongNum { get; } = Long(10).Labelled("number");

        /// <summary>
        /// A parser which parses a base-8 (octal) integer with an optional sign.
        /// The resulting <c>int</c> is not checked for overflow.
        /// </summary>
        /// <returns>A parser which parses a base-8 (octal) integer with an optional sign</returns>
        public static Parser<char, int> OctalNum { get; } = Int(8).Labelled("octal number");

        /// <summary>
        /// A parser which parses a base-16 (hexadecimal) integer with an optional sign.
        /// The resulting <c>int</c> is not checked for overflow.
        /// </summary>
        /// <returns>A parser which parses a base-16 (hexadecimal) integer with an optional sign</returns>
        public static Parser<char, int> HexNum { get; } = Int(16).Labelled("hexadecimal number");

        /// <summary>
        /// A parser which parses an integer in the given base with an optional sign.
        /// The resulting <c>int</c> is not checked for overflow.
        /// </summary>
        /// <param name="base">The base in which the number is notated, between 1 and 36</param>
        /// <returns>A parser which parses an integer with an optional sign</returns>
        public static Parser<char, int> Int(int @base)
            => Map(
                (sign, num) => sign * num,
                Sign,
                UnsignedInt(@base)
            ).Labelled($"base-{@base} number");

        /// <summary>
        /// A parser which parses an integer in the given base without a sign.
        /// The resulting <c>int</c> is not checked for overflow.
        /// </summary>
        /// <param name="base">The base in which the number is notated, between 1 and 36</param>
        /// <returns>A parser which parses an integer without a sign.</returns>
        public static Parser<char, int> UnsignedInt(int @base)
            => DigitChar(@base).ChainAtLeastOnceL(
                () => 0,
                (acc, x) => acc * @base + x
            ).Labelled($"base-{@base} number");

        /// <summary>
        /// Creates a parser which parses a long integer in the given base with an optional sign.
        /// The resulting <see cref="System.Int64" /> is not checked for overflow.
        /// </summary>
        /// <param name="base">The base in which the number is notated, between 1 and 36</param>
        /// <returns>A parser which parses a long integer with an optional sign</returns>
        public static Parser<char, long> Long(int @base)
            => Map(
                (sign, num) => sign * num,
                Sign,
                UnsignedLong(@base)
            ).Labelled($"base-{@base} number");

        /// <summary>
        /// A parser which parses a long integer in the given base without a sign.
        /// The resulting <see cref="System.Int64" /> is not checked for overflow.
        /// </summary>
        /// <param name="base">The base in which the number is notated, between 1 and 36</param>
        /// <returns>A parser which parses a long integer without a sign.</returns>
        public static Parser<char, long> UnsignedLong(int @base)
            => DigitCharLong(@base).ChainAtLeastOnceL(
                () => 0L,
                (acc, x) => acc * @base + x
            ).Labelled($"base-{@base} number");
        
        private static Parser<char, int> DigitChar(int @base)
            => @base <= 10
                ? Parser<char>.Token(c => c >= '0' && c < '0' + @base)
                    .Select(c => GetDigitValue(c))
                : Parser<char>
                    .Token(c =>
                        c >= '0' && c < '9'
                        || c >= 'A' && c < 'A' + @base - 10
                        || c >= 'a' && c < 'a' + @base - 10
                    )
                    .Select(c => GetLetterOrDigitValue(c));
        
        private static Parser<char, long> DigitCharLong(int @base)
            => @base <= 10
                ? Parser<char>.Token(c => c >= '0' && c < '0' + @base)
                    .Select(c => GetDigitValueLong(c))
                : Parser<char>
                    .Token(c =>
                        c >= '0' && c < '9'
                        || c >= 'A' && c < 'A' + @base - 10
                        || c >= 'a' && c < 'a' + @base - 10
                    )
                    .Select(c => GetLetterOrDigitValueLong(c));

        private static int GetDigitValue(char c) => c - '0';
        private static int GetLetterOrDigitValue(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return GetDigitValue(c);
            }
            if (c >= 'A' && c <= 'Z')
            {
                return GetUpperLetterOffset(c) + 10;
            }
            return GetLowerLetterOffset(c) + 10;
        }
        private static int GetUpperLetterOffset(char c) => c - 'A';
        private static int GetLowerLetterOffset(char c) => c - 'a';

        private static long GetDigitValueLong(char c) => c - '0';
        private static long GetLetterOrDigitValueLong(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return GetDigitValueLong(c);
            }
            if (c >= 'A' && c <= 'Z')
            {
                return GetUpperLetterOffsetLong(c) + 10;
            }
            return GetLowerLetterOffsetLong(c) + 10;
        }
        private static long GetUpperLetterOffsetLong(char c) => c - 'A';
        private static long GetLowerLetterOffsetLong(char c) => c - 'a';


        private static Parser<char, (string, string)> _fractionalPart
            = Char('.').ThenReturn(".").Then(Digit.AtLeastOnceString(), ValueTuple.Create);
        private static Parser<char, (string, string)> _optionalFractionalPart
            = _fractionalPart.Or(Parser<char>.Return(("", "")));
        /// <summary>
        /// A parser which parses a floating point number with an optional sign.
        /// </summary>
        /// <returns>A parser which parses a floating point number with an optional sign</returns>
        public static Parser<char, double> Real
            = Map(
                (sign, t, u) =>
                {
                    var (intPart, point, fracPart) = t;
                    var (e, expSign, exp) = u;
                    var success = double.TryParse(string.Concat(sign, intPart, point, fracPart, e, expSign, exp), out var result);
                    if (success)
                    {
                        return (double?)result;
                    }
                    return (double?)null;
                },
                SignString,
                from intPart in Digit.ManyString()
                from fracPart in intPart.Length > 0  // if we saw an integral part, the fractional part is optional
                    ? _optionalFractionalPart
                    : _fractionalPart
                select (intPart, fracPart.Item1, fracPart.Item2),
                Map(
                    ValueTuple.Create,
                    CIChar('e').ThenReturn("e"),
                    SignString,
                    Digit.AtLeastOnceString()
                ).Or(Parser<char>.Return(("", "", "")))
            )
            .Assert(x => x.HasValue, "Couldn't parse a double")
            .Select(x => x.Value)
            .Labelled($"real number");
    }
}