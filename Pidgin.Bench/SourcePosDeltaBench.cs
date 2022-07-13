using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using BenchmarkDotNet.Attributes;

namespace Pidgin.Bench;

[DisassemblyDiagnoser]
public class SourcePosDeltaBench
{
    private const ulong _linesMask = 0xFFFFFFFF00000000;
    private const ulong _colsMask = 0x00000000FFFFFFFF;
    private SourcePosDelta1[]? _data1;
    private SourcePosDelta2[]? _data2;
    private SourcePosDelta2[]? _data3;
    private SourcePosDelta2[]? _data4;

    [Params(256,1024,4096)]
    public int Amount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data1 = new SourcePosDelta1[Amount];
        _data2 = new SourcePosDelta2[Amount];
        _data3 = new SourcePosDelta2[Amount];
        _data4 = new SourcePosDelta2[Amount];

        var random = new Random(123);
        for (var i = 0; i < _data1.Length; i++)
        {
            var lines = random.Next(0, 1) == 0 ? random.Next() : 0;
            var cols = random.Next();
            _data1[i] = new SourcePosDelta1(lines, cols);
            _data2[i] = new SourcePosDelta2(lines, cols);
            _data3[Deal(i)] = new SourcePosDelta2(lines, cols);
            _data4[Deal2(i)] = new SourcePosDelta2(lines, cols);
        }

        static int Deal(int i)
        {
            var (chunk, chunkRem) = Math.DivRem(i, _countSquared);
            var (offset, bucket) = Math.DivRem(chunkRem, _count);
            return chunk * _countSquared + bucket * _count + offset;
        }
        int Deal2(int i)
        {
            var chunkSize = _data4.Length / Vector<long>.Count;
            var (offset, bucket) = Math.DivRem(i, chunkSize);
            return bucket * _count + offset;
        }
    }


    [Benchmark(Baseline = true)]
    public SourcePosDelta1 Old()
    {
        var sum = new SourcePosDelta1(0, 0);
        foreach (var d in _data1!)
        {
            sum = sum.Plus(d);
        }
        return sum;
    }

    [Benchmark]
    public SourcePosDelta1 RTL()
    {
        var lines = 0;
        var cols = 0;

        int i;
        for (i = _data1!.Length - 1; i >= 0; i--)
        {
            cols += _data1[i].Cols;
            if (_data1[i].Lines != 0)
            {
                break;
            }
        }
        for (; i >= 0; i--)
        {
            lines += _data1[i].Lines;
        }
        return new SourcePosDelta1(lines, cols);
    }

    [Benchmark]
    public SourcePosDelta2 Montster()
    {
        var sum = new SourcePosDelta2(0, 0);
        foreach (var d in _data2!)
        {
            sum = sum.PlusMonty(d);
        }
        return sum;
    }

    [Benchmark]
    public SourcePosDelta2 Benjemima()
    {
        var sum = new SourcePosDelta2(0, 0);
        foreach (var d in _data2!)
        {
            sum = sum.Plus(d);
        }
        return sum;
    }

    [Benchmark]
    public unsafe SourcePosDelta2 GatherLoop()
    {
        var result = new SourcePosDelta2(0);
        var scratch = stackalloc SourcePosDelta2[4];

        fixed (SourcePosDelta2* ptr = _data2)
        {
            for (var i = 0; i < _data2!.Length / 16; i++)
            {
                var sum = Vector256<ulong>.Zero;
                for (var offset = 0L; offset < 4; offset++)
                {
                    var ix = Avx2.Add(Vector256.Create(0, 4, 8, 12), Vector256.Create(offset));
                    var data = Avx2.GatherVector256((ulong*)(ptr + i * 16), ix, 1);
                    var mask = Avx2.BlendVariable(
                        Vector256.AsByte(Vector256.Create(-1L)),
                        Vector256.AsByte(Vector256.Create(_linesMask)),
                        Vector256.AsByte(Avx2.CompareEqual(Avx2.And(data, Vector256.Create(_linesMask)), Vector256<ulong>.Zero))
                    );
                    sum = Avx2.Add(Avx2.And(sum, Vector256.AsUInt64(mask)), data);
                }
                Avx.Store((ulong*)scratch, sum);
                for (var j = 0; j < 4; j++)
                {
                    result = result.Plus(*(scratch + j));
                }
            }
        }
        return result;
    }
    
    private static readonly Vector256<long> _ix = Vector256.Create(0, 4, 8, 12);
    private static readonly Vector256<ulong> _linesMaskAvx = Vector256.Create(_linesMask);
    private static readonly Vector256<byte> _allMaskAvx = Vector256.Create(byte.MaxValue);

    [Benchmark]
    public unsafe SourcePosDelta2 GatherNested()
    {
        var result = new SourcePosDelta2(0);

        var scratch = stackalloc SourcePosDelta2[4];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<ulong> Add(Vector256<ulong> l, Vector256<ulong> r)
        {
            var mask = Avx2.BlendVariable(
                Vector256.AsByte(_linesMaskAvx),
                _allMaskAvx,
                Vector256.AsByte(Avx2.CompareEqual(Avx2.And(r, _linesMaskAvx), Vector256<ulong>.Zero))
            );
            return Avx2.Add(Avx2.And(l, Vector256.AsUInt64(mask)), r);
        }

        fixed (SourcePosDelta2* ptr = _data2)
        {
            for (var i = 0; i < _data2!.Length / 16; i++)
            {
                var chunkStart = (ulong*)(ptr + i * 16);

                var data0 = Avx2.GatherVector256(chunkStart, _ix, 1);
                var data1 = Avx2.GatherVector256(chunkStart + 1, _ix, 1);
                var data2 = Avx2.GatherVector256(chunkStart + 2, _ix, 1);
                var data3 = Avx2.GatherVector256(chunkStart + 3, _ix, 1);

                var sum = Add(Add(data0, data1), Add(data2, data3));
                Avx.Store((ulong*)scratch, sum);
                result = result.Plus(
                    scratch[0].Plus(scratch[1])
                        .Plus(
                            scratch[2].Plus(scratch[3])
                        )
                );
            }
        }
        return result;
    }

    private static readonly Vector<ulong> _linesMaskVec
        = new(Enumerable.Repeat(_linesMask, Vector<ulong>.Count).ToArray());

    private static readonly int _count = Vector<ulong>.Count;
    private static readonly int _countSquared = _count * _count;

    [Benchmark]
    public unsafe SourcePosDelta2 SquaresLoop()
    {
        var result = new SourcePosDelta2(0);

        Span<ulong> scratch = stackalloc ulong[_count];
        var scratchSPD = MemoryMarshal.Cast<ulong, SourcePosDelta2>(scratch);

        var data2Span = MemoryMarshal.Cast<SourcePosDelta2, ulong>(_data2.AsSpan());
        for (var i = 0; i < _data2!.Length / _countSquared; i++)
        {
            var sum = Vector<ulong>.Zero;
            var slice = data2Span[(i * _countSquared)..];

            for (var offset = 0; offset < _count; offset++)
            {
                var data = new Vector<ulong>(slice[(offset*_count) ..]);

                var mask = Vector.ConditionalSelect(
                    Vector.Equals(data & _linesMaskVec, Vector<ulong>.Zero),
                    -Vector<ulong>.One,
                    _linesMaskVec
                );
                sum = (sum & mask) + data;
            }

            sum.CopyTo(scratch);

            foreach (var d in scratchSPD)
            {
                result = result.Plus(d);
            }
        }
        return result;
    }

    [Benchmark]
    public unsafe SourcePosDelta2 SquaresNested()
    {
        var result = new SourcePosDelta2(0);

        Span<ulong> scratch = stackalloc ulong[_count];
        var scratchSPD = MemoryMarshal.Cast<ulong, SourcePosDelta2>(scratch);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector<ulong> Add(Vector<ulong> l, Vector<ulong> r)
        {
            var mask = Vector.ConditionalSelect(
                Vector.Equals(r & _linesMaskVec, Vector<ulong>.Zero),
                -Vector<ulong>.One,
                _linesMaskVec
            );
            return (l & mask) + r;
        }

        var data2Span = MemoryMarshal.Cast<SourcePosDelta2, ulong>(_data2.AsSpan());
        for (var i = 0; i < _data2!.Length / _countSquared; i++)
        {
            var slice = data2Span[(i * _countSquared)..];

            var data0 = new Vector<ulong>(slice);
            var data1 = new Vector<ulong>(slice[_count..]);
            var data2 = new Vector<ulong>(slice[(_count*2)..]);
            var data3 = new Vector<ulong>(slice[(_count*3)..]);

            var sum = Add(Add(data0, data1), Add(data2, data3));

            sum.CopyTo(scratch);

            result = result.Plus(
                scratchSPD[0].Plus(scratchSPD[1])
                    .Plus(
                        scratchSPD[2].Plus(scratchSPD[3])
                    )
            );
        }
        return result;
    }
    [Benchmark]
    public unsafe SourcePosDelta2 Stride()
    {
        var sum = Vector<ulong>.Zero;
        var data2Span = MemoryMarshal.Cast<SourcePosDelta2, ulong>(_data2.AsSpan());

        for (var i = 0; i < _data2!.Length / _count; i++)
        {
            var data = new Vector<ulong>(data2Span[(i * _count)..]);
            
            var mask = Vector.ConditionalSelect(
                Vector.Equals(data & _linesMaskVec, Vector<ulong>.Zero),
                -Vector<ulong>.One,
                _linesMaskVec
            );
            sum = (sum & mask) + data;
        }
        
        var result = new SourcePosDelta2(0, 0);
        for (var i = 0; i < Vector<ulong>.Count; i++)
        {
            result = result.Plus(new SourcePosDelta2(sum[0]));
        }
        return result;
    }
    
    public struct SourcePosDelta1
    {
        public int Lines { get; }
        public int Cols { get; }

        public SourcePosDelta1(int lines, int cols)
        {
            Lines = lines;
            Cols = cols;
        }

        public SourcePosDelta1 Plus(SourcePosDelta1 other)
            => new(Lines + other.Lines, (other.Lines == 0 ? Cols : 0) + other.Cols);
    }

    public struct SourcePosDelta2
    {
        public ulong Data { get; }

        public int Lines => (int)((Data & _linesMask) >> 32);
        public int Cols => (int)(Data & _colsMask);

        public SourcePosDelta2(int lines, int cols)
        {
            Data = ((ulong)lines) << 32 | (uint)cols;
        }
        public SourcePosDelta2(ulong data)
        {
            Data = data;
        }

        public SourcePosDelta2 Plus(SourcePosDelta2 other)
        {
            var mask = (other.Data & _linesMask) == 0
                ? ulong.MaxValue
                : _linesMask;
            // the columns can overflow into the lines,
            // but i don't really care cos
            // the columns would've overflowed anyway
            return new((Data & mask) + other.Data);
        }

        public SourcePosDelta2 PlusMonty(SourcePosDelta2 other)
        {
            var hasNoLinesBool = other.Data < (1 << 32);
            var hasNoLinesByte = Unsafe.As<bool, byte>(ref hasNoLinesBool);
            var hasNoLinesMask = -(long)hasNoLinesByte;
            var mask = _linesMask | (_colsMask & (ulong)hasNoLinesMask);
            return new((Data & mask) + other.Data);
        }
    }
}
