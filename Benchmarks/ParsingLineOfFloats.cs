using System;
using System.Buffers.Text;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;

// ReSharper disable All

namespace Spanification
{
    public class ParsingLineOfFloats
    {
        string line;
        byte[] utf8line;

        [Params(100, 1_000)] public int Count;

        [GlobalSetup]
        public void Setup()
        {
            line = string.Join(" ", Enumerable.Repeat(0.023167f.ToString(NumberFormatInfo.InvariantInfo), Count)) + " ";
            utf8line = Encoding.UTF8.GetBytes(line);

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [Benchmark(Baseline = true)]
        public float OldWay()
        {
            float result = 0;

            string[] splitted = line.Split(' ');
            foreach (string toParse in splitted)
                if (float.TryParse(toParse, out float parsed))
                    result += parsed;

            return result;
        }

        [Benchmark]
        public float NewWay()
        {
            float result = 0;
        
            ReadOnlySpan<byte> toParse = new ReadOnlySpan<byte>(utf8line);
            while (!toParse.IsEmpty)
            {
                if (!Utf8Parser.TryParse(toParse, out float parsed, out int bytesConsumed))
                    break;
        
                result += parsed;
                toParse = toParse.Slice(start: bytesConsumed + 1); // 1 is for ' '
            }
        
            return result;
        }
    }
}