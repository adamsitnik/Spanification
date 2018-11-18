using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
// ReSharper disable All

namespace Benchmarks
{
    [DryJob] // this takes a LOT time to execute,  doing this once is enough
    public class ParsingUtf8File
    {
        private const string FilePath = @"C:\Users\adsitnik\AppData\Local\mlnet-resources\WordVectors\wiki.en.vec";

        [GlobalSetup]
        public void Setup() => Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; // this file uses . as decimal separators

        [Benchmark(Baseline = true)]
        public float OldWay()
        {
            float result = 0;
        
            foreach (var line in File.ReadLines(FilePath))
            {
                string[] splitted = line.Split(' ');
                foreach (string toParse in splitted)
                    if (float.TryParse(toParse, out float parsed))
                        result += parsed;
            }
        
            return result;
        }

        [Benchmark]
        public float NewWay()
        {
            float result = 0.0f;
            
            using (var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[16000 * 8]; // 128 KB
                int bytesRead = 0;
            
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    var slice = new ReadOnlySpan<byte>(buffer, start: 0, length: bytesRead); // bytesRead can be != buffer.Length
            
                    int newLineLength = 0;
                    while ((newLineLength = slice.IndexOf((byte)0x0A)) > 0) // 0x0A = new line
                    {
                        ReadOnlySpan<byte> utf8Line = slice.Slice(0, newLineLength);
            
                        result += Parse(utf8Line);
                        
                        slice = slice.Slice(start: newLineLength + 1);
                    }
                    
                    fileStream.Seek(-1 * slice.Length, SeekOrigin.Current); // we go back to the line end
                }
            }
            
            return result;
        }

        public delegate T ParsingFunc<T>(ReadOnlySpan<byte> input);

        [Benchmark]
        public IEnumerable<T> ParseFile<T>(string path, ParsingFunc<T> lineParser)
        {
            using (var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[16000 * 8]; // 128 KB
                int bytesRead = 0;
            
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    var slice = new ReadOnlySpan<byte>(buffer, start: 0, length: bytesRead); // bytesRead can be != buffer.Length
            
                    int newLineLength = 0;
                    while ((newLineLength = slice.IndexOf((byte)0x0A)) > 0) // 0x0A = new line
                    {
                        ReadOnlySpan<byte> utf8Line = slice.Slice(0, newLineLength);

                        yield return lineParser(utf8Line);
                        
                        slice = slice.Slice(start: newLineLength + 1);
                    }
                    
                    fileStream.Seek(-1 * slice.Length, SeekOrigin.Current); // we go back to the line end
                }
            }
        }
        
        [Benchmark]
        public float NewWayArrayPool()
        {
            float result = 0.0f;
            
            using (var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                ArrayPool<byte> pool = ArrayPool<byte>.Shared;
                byte[] buffer = pool.Rent(16000 * 8); // 128 KB
                int bytesRead = 0;
            
                try
                {
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        var slice = new ReadOnlySpan<byte>(buffer, start: 0, length: bytesRead); // bytesRead can be != buffer.Length
            
                        int newLineLength = 0;
                        while ((newLineLength = slice.IndexOf((byte)0x0A)) > 0) // 0x0A = new line
                        {
                            ReadOnlySpan<byte> utf8Line = slice.Slice(0, newLineLength);
            
                            result += Parse(utf8Line);
                        
                            slice = slice.Slice(start: newLineLength + 1);
                        }
                    
                        fileStream.Seek(-1 * slice.Length, SeekOrigin.Current); // we go back to the line end
                    }
                }
                finally
                {
                    pool.Return(buffer);
                }
            }
            
            return result;
        }
        
        [Benchmark]
        public async Task<float> NewWayArrayPoolAsync()
        {
            float result = 0.0f;
            
            using (var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                ArrayPool<byte> pool = ArrayPool<byte>.Shared;
                byte[] buffer = pool.Rent(16000 * 8); // 128 KB
                int bytesRead = 0;
            
                try
                {
                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        ParseBlock(new ReadOnlyMemory<byte>(buffer, start: 0, length: bytesRead), fileStream, ref result);
                    }
                }
                finally
                {
                    pool.Return(buffer);
                }
            }
            
            return result;
        }

#if !NETFRAMEWORK
        [Benchmark]
        public async ValueTask<float> NewWayArrayPoolValueTask()
        {
            float result = 0.0f;
            
            using (var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                const int bufferSize = 16000 * 8;
                ArrayPool<byte> pool = ArrayPool<byte>.Shared;
                byte[] buffer = pool.Rent(bufferSize); // 128 KB
                int bytesRead = 0;
    
                Memory<byte> memory = new Memory<byte>(buffer, 0, bufferSize);

                try
                {
                    while ((bytesRead = await fileStream.ReadAsync(memory)) > 0)
                    {
                        ParseBlock(memory.Slice(start: 0, length: bytesRead), fileStream, ref result);
                    }
                }
                finally
                {
                    pool.Return(buffer);
                }
            }
            
            return result;
        }
#endif

        private void ParseBlock(ReadOnlyMemory<byte> memory, FileStream fileStream, ref float result)
        {
            ReadOnlySpan<byte> slice = memory.Span;
                    
            int newLineLength = 0;
            while ((newLineLength = slice.IndexOf((byte)0x0A)) > 0) // 0x0A = new line
            {
                ReadOnlySpan<byte> utf8Line = slice.Slice(0, newLineLength);
                    
                result += Parse(utf8Line);
                        
                slice = slice.Slice(start: newLineLength + 1);
            }
                    
            fileStream.Seek(-1 * slice.Length, SeekOrigin.Current); // we go back to the line end
        }

        private float Parse(in ReadOnlySpan<byte> utf8Line)
        {
            float result = 0.0f;
            
            int index = utf8Line.IndexOf((byte)' ') + 1; // this particular line starts with a key (string), we skip it 
            ReadOnlySpan<byte> toParse = utf8Line.Slice(start: index);
            while (true)
            {
                if (!Utf8Parser.TryParse(toParse, out float parsed, out int bytesConsumed) || bytesConsumed == toParse.Length)
                    break;
        
                result += parsed;
                toParse = toParse.Slice(start: bytesConsumed + 1); // 1 is for ' '
            }

            return result;
        }
    }
}