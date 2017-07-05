namespace Pidgin
{
    public static partial class Parser
    {
        public static Parser<char, int> Int { get; } =
            Map(
                (sign, num) => sign.HasValue && sign.Value == '-' ? -num : num,
                (Char('-').Or(Char('+'))).Optional(),
                Digit.ChainAtLeastOnceL(
                    () => 0,
                    (acc, x) => acc * 10 + GetNumericValue(x)
                )
            ).Labelled("number");
        
        // precondition: x is a digit
        private static int GetNumericValue(char x)
            => x - '0';
    }
}