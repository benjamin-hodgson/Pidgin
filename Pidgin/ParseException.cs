using System;

namespace Pidgin
{
    /// <summary>
    /// Thrown when a parse error is encountered during parsing.
    /// </summary>
    public sealed class ParseException : Exception
    {
        internal ParseException()
        {
        }

        internal ParseException(string message)
            : base(message)
        {
        }

        internal ParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
