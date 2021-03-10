using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SoCSharp.Generators.RecordDefaultCtor.Generate;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace SoCSharp.Generators.RecordDefaultCtor.Tests.TestInfrastructure
{
    public class AnalyzerForGeneratedTestBase : GeneratorTestBase
    {
        protected Task<ImmutableArray<Diagnostic>> RunAnalyzer<T>(string source, params string[] additionalSources)
            where T : DiagnosticAnalyzer, new() => RunAnalyzer(new T(), source, additionalSources);

        protected Task<ImmutableArray<Diagnostic>> RunAnalyzer<T>(T analyzer,
            string source,
            params string[] additionalSources) where T : DiagnosticAnalyzer
        {
            var comp = CreateCompilation(source, additionalSources);
            var compilationWithAnalyzers = RunGenerators(comp, out _, new RecordDefaultCtorGenerator())
                .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

            return compilationWithAnalyzers.GetAllDiagnosticsAsync();
        }
    }
}
