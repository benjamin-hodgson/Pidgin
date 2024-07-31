using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Pidgin.Configuration;

namespace Pidgin;

public partial struct ParseState<TToken>
{
    private SourcePosDelta ComputeSourcePosDeltaAt(long location)
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
            // expect delta to be small enough to fit in an int since we're subtracting
            var delta = (int)(location - _lastSourcePosDeltaLocation);

            // _sourcePosCalculator just increments the col
            return new SourcePosDelta(_lastSourcePosDelta.Lines, _lastSourcePosDelta.Cols + delta);
        }

        var pos = _lastSourcePosDelta;

        // expect start and end to be small enough to fit in an int since we're subtracting
        var start = (int)(_lastSourcePosDeltaLocation - _bufferStartLocation);
        var end = (int)(location - _bufferStartLocation);
        for (var i = start; i < end; i++)
        {
            pos += _sourcePosCalculator(_span[i]);
        }

        return pos;
    }

    private SourcePosDelta ComputeSourcePosAt_CharDefault(long location)
    {
        // expect start and end to be small enough to fit in an int since we're subtracting
        var start = (int)(_lastSourcePosDeltaLocation - _bufferStartLocation);
        var end = (int)(location - _lastSourcePosDeltaLocation);

        // coerce _span to Span<char>
        var input = MemoryMarshal.CreateSpan(
            ref Unsafe.As<TToken, char>(ref MemoryMarshal.GetReference(_span)),
            _span.Length
        ).Slice(start, end);

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
