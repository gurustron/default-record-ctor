using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SoCSharp.Generators.RecordDefaultCtor.Analyze
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingRequiredPropsInitAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Record should initialize all required properties.";
        public const string MessageFormat = "Record's '{0}' initializer has missing required properties: {1}.";
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
            context.RegisterCompilationStartAction(analysisContext => analysisContext.RegisterCompilationEndAction(analyzer.OnCompilationEnd));
            context.RegisterSymbolAction(analyzer.AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSyntaxNodeAction(analyzer.AnalyzeSyntaxNode, SyntaxKind.ObjectCreationExpression);
        }

        private class CompilationAnalyzer
        {
            public HashSet<RecordDeclarationSyntax> RecordDeclarations { get; } = new();
            public HashSet<ObjectCreationExpressionSyntax> ObjectCreationExpressions { get; } = new();


            // TODO: move to AnalyzeSyntaxNode?
            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                if (context.Symbol is INamedTypeSymbol namedType)
                {
                    var recordDeclarations = namedType.DeclaringSyntaxReferences
                        .Select(sr => sr.GetSyntax())
                        .OfType<RecordDeclarationSyntax>();
                    RecordDeclarationSyntax? suitable = null;
                    var isGenerated = false;
                    foreach (var record in recordDeclarations)
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();
                        if (isGenerated && suitable is not null)
                        {
                            break;
                        }
                        // TODO: need to check that file was generated?
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
                        // if (record.IsSuitable(false))
                        // {
                        //     RecordDeclarations.Add(record);
                        //     break;
                        // }
                        if (record.IsSuitable(false))
                        {
                            suitable = record;
                        }

                        isGenerated |= record.SyntaxTree.FilePath.EndsWith(Helpers.Suffix);
                    }

                    if (isGenerated && suitable is null)
                    {
                        // todo - report bug
                    }

                    if (isGenerated && suitable is not null)
                    {
                        RecordDeclarations.Add(suitable);
                    }
                }
            }

            public void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
            {
                if (context.Node is ObjectCreationExpressionSyntax oce && (oce.ArgumentList is null || !oce.ArgumentList.Arguments.Any()))
                {
                    ObjectCreationExpressions.Add(oce);
                }
            }

            public void OnCompilationEnd(CompilationAnalysisContext context)
            {
                if (RecordDeclarations.Any() && ObjectCreationExpressions.Any())
                {
                    var requiredParams = RecordDeclarations
                        .Select(rds =>
                        {
                            context.CancellationToken.ThrowIfCancellationRequested();
                            var semanticModel = context.Compilation.GetSemanticModel(rds.SyntaxTree);
                            var namedTypeSymbol = semanticModel.GetDeclaredSymbol(rds);
                            if (namedTypeSymbol is null)
                            {
                                // TODO: report error
                            }
                            return (ti: namedTypeSymbol, rds);
                        })
                        .ToDictionary(
                            t => t.ti,
                            t => t.rds.ParameterList!
                                .ChildNodes()
                                .OfType<ParameterSyntax>()
                                .Where(ps => ps.Default is null)
                                .Select(ps => ps.Identifier.ToString())
                                .ToList());

                    foreach (var oce in ObjectCreationExpressions)
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();

                        var typeInfo = context.Compilation.GetSemanticModel(oce.SyntaxTree).GetTypeInfo(oce);

                        if (typeInfo.Type is INamedTypeSymbol nts && requiredParams.TryGetValue(nts, out var @params))
                        {
                            var present = oce.Initializer?
                                .Expressions
                                .OfType<AssignmentExpressionSyntax>()
                                .Where(e => e.Left is IdentifierNameSyntax)
                                .Select(e => e.Left.ToString());

                            var missing = present is null
                                ? @params
                                : @params
                                    .Except(present)
                                    .ToList();

                            if (missing.Any())
                            {
                                context.ReportDiagnostic(Diagnostic.Create(Rule,
                                    oce.GetLocation(),
                                    typeInfo.Type.ToDisplayString(),
                                    string.Join(", ", missing)));
                            }
                        }
                    }
                }
            }
        }
    }
}
