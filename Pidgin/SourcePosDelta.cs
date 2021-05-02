using System;

namespace Pidgin
{
    /// <summary>
    /// Represents a difference in textual lines and columns corresponding to a region of an input stream.
    /// </summary>
    public readonly struct SourcePosDelta : IEquatable<SourcePosDelta>, IComparable<SourcePosDelta>
    {
        /// <summary>
        /// Gets the number of lines represented by the <see cref="SourcePosDelta"/>.
        /// </summary>
        /// <returns>The number of lines</returns>
        public int Lines { get; }
        /// <summary>
        /// Gets the number of columns represented by the <see cref="SourcePosDelta"/>.
        /// </summary>
        /// <returns>The number of columns</returns>
        public int Cols { get; }

        /// <summary>
        /// Create a new <see cref="SourcePosDelta"/> with the specified number of lines and columns.
        /// </summary>
        /// <param name="lines">The number of lines</param>
        /// <param name="cols">The number of columns</param>
        public SourcePosDelta(int lines, int cols)
        {
            Lines = lines;
            Cols = cols;
        }

        /// <summary>
        /// Add two <see cref="SourcePosDelta"/>s.
        /// </summary>
        /// <param name="other">The <see cref="SourcePosDelta"/> to add to this one.</param>
        /// <returns>A <see cref="SourcePosDelta"/> representing the composition of this and <paramref name="other"/>.</returns>
        public SourcePosDelta Plus(SourcePosDelta other)
            => new SourcePosDelta(
                this.Lines + other.Lines,
                (other.Lines == 0 ? this.Cols : 0) + other.Cols
            );

        /// <summary>
        /// A <see cref="SourcePosDelta"/> representing no change in the source position.
        /// </summary>
        /// <returns>A <see cref="SourcePosDelta"/> representing no change in the source position.</returns>
        public static SourcePosDelta Zero { get; } = new SourcePosDelta(0, 0);

        /// <summary>
        /// A <see cref="SourcePosDelta"/> representing a newline being consumed.
        /// </summary>
        /// <returns>A <see cref="SourcePosDelta"/> representing a newline being consumed.</returns>
        public static SourcePosDelta NewLine { get; } = new SourcePosDelta(1, 0);

        /// <summary>
        /// A <see cref="SourcePosDelta"/> representing a single column being consumed.
        /// </summary>
        /// <returns>A <see cref="SourcePosDelta"/> representing a single column being consumed.</returns>
        public static SourcePosDelta OneCol { get; } = new SourcePosDelta(0, 1);

        /// <summary>
        /// Add two <see cref="SourcePosDelta"/>s.
        /// </summary>
        /// <param name="left">The first <see cref="SourcePosDelta"/>.</param>
        /// <param name="right">The <see cref="SourcePosDelta"/> to add to <paramref name="left"/>.</param>
        /// <returns>A <see cref="SourcePosDelta"/> representing the composition of <paramref name="left"/> and <paramref name="right"/>.</returns>
        public static SourcePosDelta operator +(SourcePosDelta left, SourcePosDelta right)
            => left.Plus(right);

        /// <inheritdoc/>
        public bool Equals(SourcePosDelta other)
            => Lines == other.Lines
            && Cols == other.Cols;

        /// <inheritdoc/>
        public override bool Equals(object? other)
            => !ReferenceEquals(null, other)
            && other is SourcePosDelta
            && Equals((SourcePosDelta)other);

        /// <inheritdoc/>
        public static bool operator ==(SourcePosDelta left, SourcePosDelta right)
            => left.Equals(right);
        /// <inheritdoc/>
        public static bool operator !=(SourcePosDelta left, SourcePosDelta right)
            => !left.Equals(right);
        
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Lines.GetHashCode();
                hash = hash * 23 + Cols.GetHashCode();
                return hash;
            }
        }

        /// <inheritdoc/>
        public int CompareTo(SourcePosDelta other)
        {
            var lineCmp = Lines.CompareTo(other.Lines);
            if (lineCmp != 0)
            {
                return lineCmp;
            }
            return Cols.CompareTo(other.Cols);
        }

        /// <inheritdoc/>
        public static bool operator >(SourcePosDelta left, SourcePosDelta right)
            => left.CompareTo(right) > 0;
        /// <inheritdoc/>
        public static bool operator <(SourcePosDelta left, SourcePosDelta right)
            => left.CompareTo(right) < 0;
        /// <inheritdoc/>
        public static bool operator >=(SourcePosDelta left, SourcePosDelta right)
            => left.CompareTo(right) >= 0;
        /// <inheritdoc/>
        public static bool operator <=(SourcePosDelta left, SourcePosDelta right)
            => left.CompareTo(right) <= 0;
    }
}