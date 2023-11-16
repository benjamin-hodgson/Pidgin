using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Pidgin.Configuration;

namespace Pidgin;

public partial struct ParseState<TToken>
{
    private SourcePosDelta ComputeSourcePosDeltaAt(int location)
    {
        if (location < _lastSourcePosDeltaLocation)
        {
            throw new ArgumentOutOfRangeException(nameof(location), location, "Tried to compute a SourcePosDelta from too far in the past. Please report this as a bug in Pidgin!");
        }

        if (location > _bufferStartLocation + _bufferedCount)
        {
            throw new ArgumentOutOfRangeException(nameof(location), location, "Tried to compute a SourcePosDelta from too far in the future. Please report this as a bug in Pidgin!");
        }

        if (ReferenceEquals(_sourcePosCalculator, CharDefaultConfiguration.Instance.SourcePosCalculator))
        {
            // TToken == char and _sourcePosCalculator is the default implementation
            return ComputeSourcePosAt_CharDefault(location);
        }
        else if (ReferenceEquals(_sourcePosCalculator, DefaultConfiguration<TToken>.Instance.SourcePosCalculator))
        {
            // _sourcePosCalculator just increments the col
            return new SourcePosDelta(_lastSourcePosDelta.Lines, _lastSourcePosDelta.Cols + location - _lastSourcePosDeltaLocation);
        }

        var pos = _lastSourcePosDelta;
        for (var i = _lastSourcePosDeltaLocation - _bufferStartLocation; i < location - _bufferStartLocation; i++)
        {
            pos += _sourcePosCalculator(_span[i]);
        }

        return pos;
    }

    private SourcePosDelta ComputeSourcePosAt_CharDefault(int location)
    {
        // coerce _span to Span<char>
        var input = MemoryMarshal.CreateSpan(
            ref Unsafe.As<TToken, char>(ref MemoryMarshal.GetReference(_span)),
            _span.Length
        ).Slice(_lastSourcePosDeltaLocation - _bufferStartLocation, location - _lastSourcePosDeltaLocation);

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

        return _lastSourcePosDelta + new SourcePosDelta(lines, cols);
    }
}
