using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System;
using System.Numerics;
using Pidgin.Configuration;

namespace Pidgin
{
    internal partial struct ParseState<TToken>
    {
        private SourcePos ComputeSourcePosAt(int location)
        {
            if (location < _lastSourcePosLocation)
            {
                throw new ArgumentOutOfRangeException(nameof(location), location, "Tried to compute a SourcePos from too far in the past. Please report this as a bug in Pidgin!");
            }
            if (location > _bufferStartLocation + _bufferedCount)
            {
                throw new ArgumentOutOfRangeException(nameof(location), location, "Tried to compute a SourcePos from too far in the future. Please report this as a bug in Pidgin!");
            }

            if (ReferenceEquals(_sourcePosCalculator, CharDefaultConfiguration.Instance.SourcePosCalculator))
            {
                // TToken == char and _sourcePosCalculator is the default implementation
                ComputeSourcePosAt_CharDefault(location);
            }
            else if (ReferenceEquals(_sourcePosCalculator, DefaultConfiguration<TToken>.Instance.SourcePosCalculator))
            {
                // _sourcePosCalculator just increments the col
                return new SourcePos(_lastSourcePos.Line, _lastSourcePos.Col + location - _lastSourcePosLocation);
            }

            var pos = _lastSourcePos;
            for (var i = _lastSourcePosLocation - _bufferStartLocation; i < location - _bufferStartLocation; i++)
            {
                pos = _sourcePosCalculator(_span[i], pos);
            }
            return pos;
        }

        private SourcePos ComputeSourcePosAt_CharDefault(int location)
        {
            // coerce _span to Span<short> (assuming TToken ~ char)
            var input = MemoryMarshal.CreateSpan(
                ref Unsafe.As<TToken, short>(ref MemoryMarshal.GetReference(_span)),
                _span.Length
            ).Slice(_lastSourcePosLocation - _bufferStartLocation, location - _lastSourcePosLocation);

            var lines = 0;
            var cols = 0;

            var i = input.Length - 1;
            // count cols after last newline
            while (i >= Vector<short>.Count)
            {
                var chunk = new Vector<short>(input.Slice(i - Vector<short>.Count));
                var chunkContainsNewline = Vector.EqualsAny(chunk, new Vector<short>((short)'\n'));
                if (chunkContainsNewline)
                {
                    break;
                }
                // 4 for tabs, 1 for other chars
                var tabs = Vector.Equals(chunk, new Vector<short>((short)'\t'));  // -1 for \t; 0 otherwise
                var charCounts = tabs * new Vector<short>(-3) + Vector<short>.One;  // 4 for \t; 1 otherwise
                cols += Vector.Dot(charCounts, Vector<short>.One);
                i -= Vector<short>.Count;
            }
            // either this is the rightmost chunk containing a newline, or we are in the leftmost chunk
            while (i >= Math.Max(i - Vector<short>.Count, 0))
            {
                var c = input[i];
                if (c == '\n')
                {
                    lines++;
                }
                else if (c == '\t') 
                {
                    cols += 4;
                }
                else
                {
                    cols++;
                }
                i--;
            }
            // count remaining newlines
            while (i >= Vector<short>.Count)
            {
                var chunk = new Vector<short>(input.Slice(i - Vector<short>.Count));
                var newlines = Vector.Equals(chunk, new Vector<short>((short)'\n'));  // -1 for \n; 0 otherwise
                lines += Vector.Dot(newlines, new Vector<short>(-1));
                i -= Vector<short>.Count;
            }
            // count newlines in leftmost chunk
            while (i >= 0)
            {
                if (input[i] == '\n')
                {
                    lines++;
                }
                i--;
            }
            return new SourcePos(
                lines + _lastSourcePos.Line,
                (lines == 0 ? _lastSourcePos.Col : 0) + cols
            );
        }
    }
}
