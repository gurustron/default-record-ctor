using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace SoCSharp.Generators.RecordDefaultCtor.Tests.TestInfrastructure
{
    public class AnalyzerTestBase : RoslynTestBase
    {
        protected Task<ImmutableArray<Diagnostic>> RunAnalyzer<T>(string source, params string[] additionalSources) where T : DiagnosticAnalyzer, new() => RunAnalyzer(new T(), source,additionalSources);

        protected Task<ImmutableArray<Diagnostic>> RunAnalyzer<T>(T analyzer, string source, params string[] additionalSources) where T : DiagnosticAnalyzer
        {
            var compilationWithAnalyzers = CreateCompilation(source, additionalSources)
                .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

            return compilationWithAnalyzers.GetAllDiagnosticsAsync();
        }
    }
}
