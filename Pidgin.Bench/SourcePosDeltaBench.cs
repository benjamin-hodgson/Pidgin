using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using BenchmarkDotNet.Attributes;

namespace Pidgin.Bench;

public class SourcePosDeltaBench
{
    private const ulong _linesMask = 0xFFFFFFFF00000000;
    private const ulong _colsMask = 0x00000000FFFFFFFF;
    private SourcePosDelta1[]? _data1;
    private SourcePosDelta2[]? _data2;

    [Params(256,1024,4096)]
    public int Amount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data1 = new SourcePosDelta1[Amount];
        _data2 = new SourcePosDelta2[Amount];

        var random = new Random(123);
        for (var i = 0; i < _data1.Length; i++)
        {
            var lines = random.Next(0, 1) == 0 ? random.Next() : 0;
            var cols = random.Next();
            _data1[i] = new SourcePosDelta1(lines, cols);
            _data2[i] = new SourcePosDelta2(lines, cols);
        }
    }


    [Benchmark(Baseline = true)]
    public void Old()
    {
        var sum = new SourcePosDelta1(0, 0);
        foreach (var d in _data1!)
        {
            sum = sum.Plus(d);
        }
    }

    [Benchmark]
    public void Montster()
    {
        var sum = new SourcePosDelta2(0, 0);
        foreach (var d in _data2!)
        {
            sum = sum.PlusMonty(d);
        }
    }

    [Benchmark]
    public void Craveable()
    {
        var sum = new SourcePosDelta2(0, 0);
        foreach (var d in _data2!)
        {
            sum = sum.PlusCraver(d);
        }
    }

    [Benchmark]
    public void Benjemima()
    {
        var sum = new SourcePosDelta2(0, 0);
        foreach (var d in _data2!)
        {
            sum = sum.PlusBenjamin(d);
        }
    }

    [Benchmark]
    public unsafe void Simd()
    {
        var sum = Vector256<ulong>.Zero;
        fixed (SourcePosDelta2* ptr = _data2)
        {
            for (var i = 0; i < _data2!.Length / 16; i++)
            {
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
            }
        }
        var result = new SourcePosDelta2(0);
        var output = stackalloc SourcePosDelta2[4];
        Avx.Store((ulong*)output, sum);
        for (var i = 0; i < 4; i++)
        {
            result = result.PlusBenjamin(*(output + i));
        }
    }

    private struct SourcePosDelta1
    {
        public int Lines;
        public int Cols;

        public SourcePosDelta1(int lines, int cols)
        {
            Lines = lines;
            Cols = cols;
        }

        public SourcePosDelta1 Plus(SourcePosDelta1 other)
            => new()
            {
                Lines = Lines + other.Lines,
                Cols = (other.Lines == 0 ? Cols : 0) + other.Cols
            };
    }

    private struct SourcePosDelta2
    {

        public ulong Data;

        public SourcePosDelta2(int lines, int cols)
        {
            Data = ((ulong)lines) << 32 | (uint)cols;
        }
        public SourcePosDelta2(ulong data)
        {
            Data = data;
        }

        public SourcePosDelta2 PlusBenjamin(SourcePosDelta2 other)
        {
            var mask = (other.Data & _linesMask) == 0
                ? ulong.MaxValue
                : _linesMask;
            // the columns can overflow into the lines,
            // but i don't really care cos
            // the columns would've overflowed anyway
            return new((Data & mask) + other.Data);
        }

        public SourcePosDelta2 PlusCraver(SourcePosDelta2 other)
        {
            return (other.Data & _linesMask) == 0
                ? new(Data + other.Data)
                : new((Data & _linesMask) + other.Data);
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
