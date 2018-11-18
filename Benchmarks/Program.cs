using BenchmarkDotNet.Running;

// ReSharper disable All

namespace Benchmarks
{
    class Program
    {
        static unsafe void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
