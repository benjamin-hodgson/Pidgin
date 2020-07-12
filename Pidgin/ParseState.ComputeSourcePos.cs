using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
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
                return ComputeSourcePosAt_CharDefault(location);
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
            // coerce _span to Span<char>
            var input = MemoryMarshal.CreateSpan(
                ref Unsafe.As<TToken, char>(ref MemoryMarshal.GetReference(_span)),
                _span.Length
            ).Slice(_lastSourcePosLocation - _bufferStartLocation, location - _lastSourcePosLocation);

            var lines = 0;
            var cols = 0;

            var i = input.Length - 1;
            // count cols after last newline
            while (i >= 0 && lines == 0)
            {
                if (input[i] == '\n')
                {
                    lines++;
                }
                else if (input[i] == '\t')
                {
                    cols += 4;
                }
                else
                {
                    cols++;
                }
                i--;
            }
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
                (lines == 0 ? _lastSourcePos.Col : 1) + cols
            );
        }
    }
}
