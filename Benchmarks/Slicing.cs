using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

// ReSharper disable All

namespace Benchmarks
{
    public class Slicing
    {
        public IEnumerable<object> Arguments()
        {
            yield return "Substring vs Slice";
            yield return string.Join(", ", Enumerable.Repeat("Substring vs Slice", 1000));
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Arguments))]
        public string Substring(string text) => text.Substring(startIndex: text.Length / 2);

        [Benchmark]
        [ArgumentsSource(nameof(Arguments))]
        public ReadOnlySpan<char> Slice(string text) => text.AsSpan().Slice(start: text.Length / 2);
    }
}