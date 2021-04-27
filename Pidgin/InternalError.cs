using System.Runtime.InteropServices;

namespace Pidgin
{
    /// <summary>
    /// Represents error information during parsing.
    ///
    /// WARNING: This API is <strong>unstable</strong>
    /// and subject to change in future versions of the library.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct InternalError<TToken>
    {
        /// <summary>True if the error is due to the parser reaching the end of the input</summary>
        public bool EOF { get; }
        /// <summary>Contains the unexpected token</summary>
        public Maybe<TToken> Unexpected { get; }
        /// <summary>The absolute location at which the error occurred</summary>
        public int ErrorLocation { get; }
        /// <summary>A custom message</summary>
        public string? Message { get; }

        /// <summary>Creates an <see cref="InternalError{TToken}"/>.</summary>
        public InternalError(Maybe<TToken> unexpected, bool eof, int errorLocation, string? message)
        {
            Unexpected = unexpected;
            EOF = eof;
            ErrorLocation = errorLocation;
            Message = message;
        }
    }
}