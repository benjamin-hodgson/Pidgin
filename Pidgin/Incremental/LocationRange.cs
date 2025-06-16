namespace Pidgin.Incremental;

/// <summary>
/// Represents a portion of an input stream,
/// defined by a start position and a length.
/// </summary>
/// <param name="Start">
/// The starting location of the range.
/// </param>
/// <param name="Length">The length of the range.</param>
public record LocationRange(long Start, long Length)
{
    /// <summary>
    /// The location immediately after the end of the range.
    /// </summary>
    public long End => Start + Length;

    internal bool Contains(long location)
        => location >= Start && location <= End;

    internal bool Overlaps(LocationRange other)
        => End >= other.Start && other.End >= Start;

    internal LocationRange ShiftBy(long amount)
        => this with { Start = Start + amount };
}
