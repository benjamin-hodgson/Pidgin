using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pidgin
{
    /// <summary>
    /// Represents a difference in textual lines and columns corresponding to a region of an input stream.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct SourcePosDelta : IEquatable<SourcePosDelta>, IComparable<SourcePosDelta>
    {
        private const ulong _linesMask = 0xFFFFFFFF00000000;
        private const ulong _colsMask = 0x00000000FFFFFFFF;
        [FieldOffset(0)]
        private readonly ulong _data;

        /// <summary>
        /// Gets the number of lines represented by the <see cref="SourcePosDelta"/>.
        /// </summary>
        /// <returns>The number of lines</returns>
        public int Lines => (int)((_data & _linesMask) >> 32);
        /// <summary>
        /// Gets the number of columns represented by the <see cref="SourcePosDelta"/>.
        /// </summary>
        /// <returns>The number of columns</returns>
        public int Cols => (int)(_data & _colsMask);

        /// <summary>
        /// Create a new <see cref="SourcePosDelta"/> with the specified number of lines and columns.
        /// </summary>
        /// <param name="lines">The number of lines</param>
        /// <param name="cols">The number of columns</param>
        public SourcePosDelta(int lines, int cols) : this((((ulong)lines) << 32) | (uint)cols)
        {
        }

        private SourcePosDelta(ulong data)
        {
            _data = data;
        }

        /// <summary>
        /// Add two <see cref="SourcePosDelta"/>s.
        /// </summary>
        /// <param name="other">The <see cref="SourcePosDelta"/> to add to this one.</param>
        /// <returns>A <see cref="SourcePosDelta"/> representing the composition of this and <paramref name="other"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SourcePosDelta Plus(SourcePosDelta other)
        {
            var mask = other.Lines == 0
                ? ulong.MaxValue
                : _linesMask;
            // There's a possibility of the Cols overflowing into the Lines here.
            // I'm not too concerned about that, because the Cols
            // would've overflowed by themselves anyway
            return new((_data & mask) + other._data);
        }

        /// <summary>
        /// Add two <see cref="SourcePosDelta"/>s.
        /// </summary>
        /// <param name="other">The <see cref="SourcePosDelta"/> to add to this one.</param>
        /// <returns>A <see cref="SourcePosDelta"/> representing the composition of this and <paramref name="other"/>.</returns>
        public SourcePosDelta Add(SourcePosDelta other) => Plus(other);

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

        ///
        public override string ToString() => $"({Lines}, {Cols})";

        /// <inheritdoc/>
        public bool Equals(SourcePosDelta other)
            => Lines == other.Lines
            && Cols == other.Cols;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is not null
            && obj is SourcePosDelta delta
            && Equals(delta);

        /// <inheritdoc/>
        public static bool operator ==(SourcePosDelta left, SourcePosDelta right)
            => left.Equals(right);
        /// <inheritdoc/>
        public static bool operator !=(SourcePosDelta left, SourcePosDelta right)
            => !left.Equals(right);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Lines, Cols);

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
