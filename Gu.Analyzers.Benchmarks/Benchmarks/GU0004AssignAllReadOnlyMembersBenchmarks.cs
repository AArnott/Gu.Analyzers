// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Gu.Analyzers.Benchmarks.Benchmarks
{
    public class GU0004AssignAllReadOnlyMembersBenchmarks
    {
        private static readonly Gu.Roslyn.Asserts.Benchmark Benchmark = Gu.Roslyn.Asserts.Benchmark.Create(Code.AnalyzersProject, new Gu.Analyzers.GU0004AssignAllReadOnlyMembers());

        [BenchmarkDotNet.Attributes.Benchmark]
        public void RunOnGuAnalyzersProject()
        {
            Benchmark.Run();
        }
    }
}