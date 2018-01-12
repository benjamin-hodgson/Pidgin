using System;

namespace Pidgin
{
    /// <summary>
    /// Represents a (line, col) position in an input stream
    /// </summary>
    public readonly struct SourcePos : IEquatable<SourcePos>, IComparable<SourcePos>
    {
        /// <summary>
        /// Gets the line of the position in the input stream.
        /// The value is 1-indexed: a Line value of 1 refers to the first line of the input document.
        /// </summary>
        /// <returns>The line</returns>
        public int Line { get; }
        /// <summary>
        /// Gets the column of the position in the input stream
        /// The value is 1-indexed: a Col value of 1 refers to the first column of the line.
        /// </summary>
        /// <returns>The column</returns>
        public int Col { get; }

        /// <summary>
        /// Create a new <see cref="SourcePos"/> with the specified 1-indexed line and column number.
        /// </summary>
        /// <param name="line">The 1-indexed line number</param>
        /// <param name="col">The 1-indexed column number</param>
        public SourcePos(int line, int col)
        {
            Line = line;
            Col = col;
        }

        /// <summary>
        /// Creates a <see cref="SourcePos"/> with the column number incremented by one
        /// </summary>
        /// <returns>A <see cref="SourcePos"/> with the column number incremented by one</returns>
        public SourcePos IncrementCol()
            => new SourcePos(Line, Col + 1);
        /// <summary>
        /// Creates a <see cref="SourcePos"/> with the line number incremented by one and the column number reset to 1
        /// </summary>
        /// <returns>A <see cref="SourcePos"/> with the line number incremented by one and the column number reset to 1</returns>
        public SourcePos NewLine()
            => new SourcePos(Line + 1, 1);

        /// <inheritdoc/>
        public bool Equals(SourcePos other)
            => Line == other.Line
            && Col == other.Col;

        /// <inheritdoc/>
        public override bool Equals(object other)
            => !ReferenceEquals(null, other)
            && other is SourcePos
            && Equals((SourcePos)other);

        /// <inheritdoc/>
        public static bool operator ==(SourcePos left, SourcePos right)
            => left.Equals(right);
        /// <inheritdoc/>
        public static bool operator !=(SourcePos left, SourcePos right)
            => !left.Equals(right);
        
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Line.GetHashCode();
                hash = hash * 23 + Col.GetHashCode();
                return hash;
            }
        }

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

        /// <inheritdoc/>
        public static bool operator >(SourcePos left, SourcePos right)
            => left.CompareTo(right) > 0;
        /// <inheritdoc/>
        public static bool operator <(SourcePos left, SourcePos right)
            => left.CompareTo(right) < 0;
        /// <inheritdoc/>
        public static bool operator >=(SourcePos left, SourcePos right)
            => left.CompareTo(right) >= 0;
        /// <inheritdoc/>
        public static bool operator <=(SourcePos left, SourcePos right)
            => left.CompareTo(right) <= 0;
    }
}