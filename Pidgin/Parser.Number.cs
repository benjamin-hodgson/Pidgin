using System;
using System.Linq;
using System.Text;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// A parser which parses a floating point number with an optional sign.
        /// The resulting <c>double</c> is not checked for overflow.
        /// </summary>
        /// <returns>A parser which parses a floating point number with an optional sign</returns>
        public static Parser<char, double> Float { get; } = RealNum();

        /// <summary>
        /// A parser which parses a floating point number with an optional sign.
        /// The resulting <c>double</c> is not checked for overflow.
        /// </summary>
        /// <returns>A parser which parses a floating point number with an optional sign</returns>
        public static Parser<char, double> RealNum()
            => Map(
                (sign, num) => sign.HasValue && sign.Value == '-' ? -num : num,
                (Char('-').Or(Char('+'))).Optional(),
                UnsignedReal()
            ).Labelled($"real number");

        /// <summary>
        /// A parser which parses a floating point number without a sign.
        /// The resulting <c>double</c> is not checked for overflow.
        /// </summary>
        /// <returns>A parser which parses a floating point number without a sign.</returns>

        // This method follows a similar approach that the other numeric parsers
        // use.  Unfortuenetely it does not handle ".123" (no integer portion)
        //public static Parser<char, double> UnsignedReal()
        // => Map(
        //    (integerPart, _, decimalPart) => integerPart + decimalPart,
        //    UnsignedInt(10),
        //    Char('.'),
        //        DigitChar(10).ChainAtLeastOnceL(
        //            () => (val: 0.0, exp: 0.1),
        //            (t, n) =>
        //            {
        //                t.val = t.val + (n * t.exp);
        //                t.exp = t.exp / 10;
        //                return t;
        //            }
        //        ).Select(t => t.val)
        //    );

        // This method effectively accumulates a numeric string (with a decimal point) and then
        // uses the built in C# routines to convert the value.  The method of isolated 
        // increment value creation as used by the numeric parser would have been too difficult
        // without the establishment of multi-parser context (e.g. we have to know at least 
        // which character position we are right of the decimal).  That along with the rounding
        // and other issues of dealing with floating point values, the path is best suited to
        // the tried and true .NET support. 

        public static Parser<char, double> UnsignedReal()
            => DigitCharDouble().ChainAtLeastOnceAL(

                    // Seed method
                        () => 0.0,

                    // Process method
                    (acc, x, s) =>
                    {
                        double d = 0.0;
                        if (s != "."){
                            d = Convert.ToDouble(s);
                        }
                        return d;
                    },

                    // Post process method
                    // (validation)
                    (acc, s) =>
                    {
                        if (s.IndexOf(".") < 0 || s.Length < 2)
                        {
                            return false;
                        }
                        return true;
                    }

                ).Labelled($"real number");

        // This method will need to be enhanced for coutries that use a ","
        // instead of a decimal point.  I was unsure of your international strategy.
        private static Parser<char, double> DigitCharDouble()
            => Parser<char>.Token(c => (c >= '0' && c < '0' + 10) || c == '.')
                //.Select(c => (double) c);
                .Select(c => (double) GetDigitValueDouble(c));

        // This method just returns the character value
        // (there is probably an existing replacement)
        private static int GetDigitValueDouble(char c) => c;

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
                (sign, num) => sign.HasValue && sign.Value == '-' ? -num : num,
                (Char('-').Or(Char('+'))).Optional(),
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
                (sign, num) => sign.HasValue && sign.Value == '-' ? -num : num,
                (Char('-').Or(Char('+'))).Optional(),
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
    }
}