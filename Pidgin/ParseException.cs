using System;

namespace Pidgin
{
    /// <summary>
    /// Thrown when a parse error is encountered during parsing
    /// </summary>
    public class ParseException : Exception
    {
        public ParseException()
        {
        }

        public ParseException(string message) : base(message)
        {
        }

        public ParseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}