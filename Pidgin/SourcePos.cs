using System;

namespace Pidgin;

/// <summary>
/// Represents a (line, col) position in an input stream.
/// </summary>
public readonly struct SourcePos : IEquatable<SourcePos>, IComparable<SourcePos>
{
    /// <summary>
    /// Gets the line of the position in the input stream.
    /// The value is 1-indexed: a Line value of 1 refers to the first line of the input document.
    /// </summary>
    /// <returns>The line.</returns>
    public int Line { get; }

    /// <summary>
    /// Gets the column of the position in the input stream
    /// The value is 1-indexed: a Col value of 1 refers to the first column of the line.
    /// </summary>
    /// <returns>The column.</returns>
    public int Col { get; }

    /// <summary>
    /// Create a new <see cref="SourcePos"/> with the specified 1-indexed line and column number.
    /// </summary>
    /// <param name="line">The 1-indexed line number.</param>
    /// <param name="col">The 1-indexed column number.</param>
    public SourcePos(int line, int col)
    {
        Line = line;
        Col = col;
    }

    /// <summary>
    /// Add a <see cref="SourcePosDelta"/> to this <see cref="SourcePos"/>.
    /// </summary>
    /// <param name="other">The <see cref="SourcePosDelta"/> to add to this <see cref="SourcePos"/>.</param>
    /// <returns>A <see cref="SourcePos"/> representing the composition of this and <paramref name="other"/>.</returns>
    public SourcePos Plus(SourcePosDelta other)
        => new(
            Line + other.Lines,
            (other.Lines == 0 ? Col : 1) + other.Cols
        );

    /// <summary>
    /// Add a <see cref="SourcePosDelta"/> to this <see cref="SourcePos"/>.
    /// </summary>
    /// <param name="other">The <see cref="SourcePosDelta"/> to add to this <see cref="SourcePos"/>.</param>
    /// <returns>A <see cref="SourcePos"/> representing the composition of this and <paramref name="other"/>.</returns>
    public SourcePos Add(SourcePosDelta other) => Plus(other);

    /// <summary>
    /// Creates a <see cref="SourcePos"/> with the column number incremented by one.
    /// </summary>
    /// <returns>A <see cref="SourcePos"/> with the column number incremented by one.</returns>
    public SourcePos IncrementCol()
        => new(Line, Col + 1);

    /// <summary>
    /// Creates a <see cref="SourcePos"/> with the line number incremented by one and the column number reset to 1.
    /// </summary>
    /// <returns>A <see cref="SourcePos"/> with the line number incremented by one and the column number reset to 1.</returns>
    public SourcePos NewLine()
        => new(Line + 1, 1);

    /// <summary>
    /// Add a <see cref="SourcePosDelta"/> to this <see cref="SourcePos"/>.
    /// </summary>
    /// <param name="left">The <see cref="SourcePos"/>.</param>
    /// <param name="right">The <see cref="SourcePosDelta"/> to add to this <see cref="SourcePos"/>.</param>
    /// <returns>A <see cref="SourcePos"/> representing the composition of <paramref name="left"/> and <paramref name="right"/>.</returns>
    public static SourcePos operator +(SourcePos left, SourcePosDelta right)
        => left.Plus(right);

    /// <inheritdoc/>
    public bool Equals(SourcePos other)
        => Line == other.Line
        && Col == other.Col;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is not null
        && obj is SourcePos pos
        && Equals(pos);

    /// <summary>Equality operator.</summary>
    /// <param name="left">The left <see cref="SourcePos"/>.</param>
    /// <param name="right">The right <see cref="SourcePos"/>.</param>
    public static bool operator ==(SourcePos left, SourcePos right)
        => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    /// <param name="left">The left <see cref="SourcePos"/>.</param>
    /// <param name="right">The right <see cref="SourcePos"/>.</param>
    public static bool operator !=(SourcePos left, SourcePos right)
        => !left.Equals(right);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Line, Col);

    /// <inheritdoc/>
    public int CompareTo(SourcePos other)
    {
        var lineCmp = Line.CompareTo(other.Line);
        if (lineCmp != 0)
        {
            return lineCmp;
        }

        return Col.CompareTo(other.Col);
    }

    /// <summary>Comparison operator.</summary>
    /// <param name="left">The left <see cref="SourcePos"/>.</param>
    /// <param name="right">The right <see cref="SourcePos"/>.</param>
    public static bool operator >(SourcePos left, SourcePos right)
        => left.CompareTo(right) > 0;

    /// <summary>Comparison operator.</summary>
    /// <param name="left">The left <see cref="SourcePos"/>.</param>
    /// <param name="right">The right <see cref="SourcePos"/>.</param>
    public static bool operator <(SourcePos left, SourcePos right)
        => left.CompareTo(right) < 0;

    /// <summary>Comparison operator.</summary>
    /// <param name="left">The left <see cref="SourcePos"/>.</param>
    /// <param name="right">The right <see cref="SourcePos"/>.</param>
    public static bool operator >=(SourcePos left, SourcePos right)
        => left.CompareTo(right) >= 0;

    /// <summary>Comparison operator.</summary>
    /// <param name="left">The left <see cref="SourcePos"/>.</param>
    /// <param name="right">The right <see cref="SourcePos"/>.</param>
    public static bool operator <=(SourcePos left, SourcePos right)
        => left.CompareTo(right) <= 0;
}
