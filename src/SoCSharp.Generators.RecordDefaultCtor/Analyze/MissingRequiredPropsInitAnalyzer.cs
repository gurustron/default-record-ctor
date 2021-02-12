using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
            new(
                DiagnosticIds.MissingRequiredPropsInitAnalyzerRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.MissingRequiredPropsInitAnalyzer,
                DiagnosticSeverity.Warning,
                true,
                Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            var analyzer = new CompilationAnalyzer();
            context.RegisterCompilationStartAction(analyzer.OnStarted);
            context.RegisterSymbolAction(analyzer.AnalyzeSymbol, SymbolKind.NamedType);
            // context.RegisterSyntaxNodeAction();
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
                    var syntax = namedType.DeclaringSyntaxReferences
                        .Select(sr => sr.GetSyntax())
                        .OfType<RecordDeclarationSyntax>();
                    RecordDeclarationSyntax? suitable = null;
                    // bool HasDefaultCtor = false;
                    foreach (var record in syntax)
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();
                        // var generatedKind = context.Compilation.Options.SyntaxTreeOptionsProvider.IsGenerated(record.SyntaxTree,
                        //     context.CancellationToken);
                        // SyntaxNode? parent = record;
                        // while (parent.Parent is not null)
                        // {
                        //     parent = parent.Parent;
                        // }
                        // var generatedKind1 = context.Compilation.Options.SyntaxTreeOptionsProvider.IsGenerated(parent.SyntaxTree,
                        //     context.CancellationToken);

                        // if(record.HasDefaultCtor())
                        if (record.IsSuitable())
                        {
                            suitable = record;
                            break;
                        }
                    }
                }
            }

            public void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
            {
                if (context.Node is RecordDeclarationSyntax)
                {

                }
            }
        }
    }
}
