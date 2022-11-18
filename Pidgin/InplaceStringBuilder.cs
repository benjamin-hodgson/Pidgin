using System;
using System.Runtime.CompilerServices;

namespace Pidgin;

/// <summary>
/// A mutable struct! Careful!.
/// </summary>
internal struct InplaceStringBuilder
{
    private int _offset;
    private readonly int _capacity;
    private readonly string _value;

    public InplaceStringBuilder(int capacity)
    {
        _offset = 0;
        _capacity = capacity;
        _value = new string('\0', capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Append(char c)
    {
        if (_offset >= _capacity)
        {
            throw new InvalidOperationException();
        }

        fixed (char* destination = _value)
        {
            destination[_offset] = c;
            _offset++;
        }
    }

    public override string ToString()
    {
        if (_capacity != _offset)
        {
            throw new InvalidOperationException();
        }

        return _value;
    }
}
