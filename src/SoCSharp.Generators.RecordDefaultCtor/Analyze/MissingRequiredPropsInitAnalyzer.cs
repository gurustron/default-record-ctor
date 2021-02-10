using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace SoCSharp.Generators.RecordDefaultCtor.Analyze
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingRequiredPropsInitAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Record should initialize all required properties.";
        public const string MessageFormat = "Record '{0}' has next missing required properties {1}.";
        private const string Description = "Record should initialize all required properties.";

        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.MissingRequiredPropsInitAnalyzerRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.MissingRequiredPropsInitAnalyzer,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            var analyzer = new CompilationAnalyzer();
            context.RegisterCompilationStartAction(analyzer.OnStarted);
            context.RegisterSymbolAction(analyzer.AnalyzeSymbol, SymbolKind.NamedType);
        }

        private class CompilationAnalyzer
        {
            public void OnStarted(CompilationStartAnalysisContext context)
            {

            }

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                if (context.Symbol is INamedTypeSymbol namedType)
                {
                }
            }
        }
    }
}
