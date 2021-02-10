using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;

namespace SoCSharp.Generators.RecordDefaultCtor.Tests.TestInfrastructure
{
    public abstract class GeneratorTestBase:RoslynTestBase
    {

        protected GeneratorDriver CreateDriver(Compilation compilation, params ISourceGenerator[] generators)
        {
            return CSharpGeneratorDriver.Create(
                ImmutableArray.Create(generators),
                ImmutableArray<AdditionalText>.Empty,
                (CSharpParseOptions) compilation.SyntaxTrees.First().Options
            );
        }

        protected Compilation RunGenerators(Compilation compilation, out ImmutableArray<Diagnostic> diagnostics,
            params ISourceGenerator[] generators)
        {
            CreateDriver(compilation, generators)
                .RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out diagnostics);
            return updatedCompilation;
        }
    }
}
