namespace Pidgin.Incremental;

/// <summary>
/// Represents information about an edit to an input stream,
/// including the range of the original input affected and
/// the length of the new content.
/// </summary>
/// <param name="InputRange">
/// The range in the original input stream that is being replaced.
/// </param>
/// <param name="NewContentLength">
/// The length of the new content that replaces the original range.
/// </param>
public record EditInfo(LocationRange InputRange, long NewContentLength)
{
    internal long LengthDelta => NewContentLength - InputRange.Length;
    internal long NewEnd => InputRange.Start + NewContentLength;

    /// <summary>
    /// Turn a location in the original input stream into
    /// a corresponding location in the new (edited) one.
    /// </summary>
    /// <param name="oldLocation">The location in the original input stream.</param>
    /// <returns>The new location, or null if the location was edited.</returns>
    internal long? Shift(long oldLocation)
    {
        if (oldLocation < InputRange.Start)
        {
            return oldLocation;
        }

        // see "NOTE: Edits at ends of cached results" in LocationRange
        if (oldLocation >= InputRange.End)
        {
            return oldLocation + LengthDelta;
        }

        return null;
    }

    /// <summary>
    /// Turn a location in the new (edited) input stream into
    /// a corresponding location in the original one.
    /// </summary>
    /// <param name="newLocation">The location in the new (edited) input stream.</param>
    /// <returns>
    /// The corresponding location in the original input stream,
    /// or null if the location was edited.
    /// </returns>
    internal long? Unshift(long newLocation)
    {
        if (newLocation < InputRange.Start)
        {
            return newLocation;
        }

        // see "NOTE: Edits at ends of cached results" in LocationRange
        if (newLocation >= NewEnd)
        {
            return newLocation - LengthDelta;
        }

        return null;
    }
}
