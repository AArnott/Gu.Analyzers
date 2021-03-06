// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable RedundantNameQualifier
#pragma warning disable CS0162 // Unreachable code detected
#pragma warning disable GU0011 // Don't ignore the return value.
namespace Gu.Analyzers.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;
    using Gu.Analyzers.Benchmarks.Benchmarks;

    internal class Program
    {
        public static void Main()
        {
            if (false)
            {
                var benchmark = Gu.Roslyn.Asserts.Benchmark.Create(
                    Code.AnalyzersProject,
                    new ArgumentListAnalyzer());

                // Warmup
                benchmark.Run();
                Console.WriteLine("Attach profiler and press any key to continue...");
                Console.ReadKey();
                benchmark.Run();
            }
            else if (false)
            {
                foreach (var summary in RunSingle<AllBenchmarks>())
                {
                    CopyResult(summary);
                }
            }
            else
            {
                foreach (var summary in RunAll())
                {
                    CopyResult(summary);
                }
            }
        }

        private static IEnumerable<Summary> RunAll()
        {
            var switcher = new BenchmarkSwitcher(typeof(Program).Assembly);
            var summaries = switcher.Run(new[] { "*" });
            return summaries;
        }

        private static IEnumerable<Summary> RunSingle<T>()
        {
            var summaries = new[] { BenchmarkRunner.Run<T>() };
            return summaries;
        }

        private static void CopyResult(Summary summary)
        {
            var fileName = $"{summary.Title}-report-github.md";
            Console.WriteLine(fileName);
            var sourceFileName = Directory.EnumerateFiles(summary.ResultsDirectoryPath, fileName)
                                          .Single();
            var destinationFileName = Path.Combine(summary.ResultsDirectoryPath, "..\\..\\..\\..\\..\\Benchmarks", summary.Title + ".md");
            Console.WriteLine($"Copy: {sourceFileName} -> {destinationFileName}");
            File.Copy(sourceFileName, destinationFileName, overwrite: true);
        }
    }
}
