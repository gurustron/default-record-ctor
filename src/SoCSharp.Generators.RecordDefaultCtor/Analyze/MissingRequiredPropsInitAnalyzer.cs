using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace SoCSharp.Generators.RecordDefaultCtor.Analyze
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MissingRequiredPropsInitAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Record should initialize all required properties.";
        public const string MessageFormat = "Record '{0}' has missing required properties: {1}.";
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
            context.RegisterSemanticModelAction(analyzer.AnalyzeSemanticModel);
            context.RegisterSymbolAction(analyzer.AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSyntaxNodeAction(analyzer.AnalyzeSyntaxNode, SyntaxKind.ObjectCreationExpression);
        }

        private class CompilationAnalyzer
        {
            public SemanticModelAnalysisContext SemanticModelContext { get; internal set; }
            public HashSet<RecordDeclarationSyntax> RecordDeclarations { get; } = new();
            public HashSet<ObjectCreationExpressionSyntax> ObjectCreationExpressions { get; } = new();

            public void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
            {
                SemanticModelContext = context;
            }

            // TODO: move to AnalyzeSyntaxNode?
            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                if (context.Symbol is INamedTypeSymbol namedType)
                {
                    var recordDeclarations = namedType.DeclaringSyntaxReferences
                        .Select(sr => sr.GetSyntax())
                        .OfType<RecordDeclarationSyntax>();
                    foreach (var record in recordDeclarations)
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();
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
                        if (record.IsSuitable(false))
                        {
                            RecordDeclarations.Add(record);
                            break;
                        }
                    }
                }
            }

            public void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
            {
                if (context.Node is ObjectCreationExpressionSyntax {Initializer: not null} oce)
                {
                    // var typeInfo = context.SemanticModel.GetTypeInfo(oce);
                    ObjectCreationExpressions.Add(oce);
                }
            }

            public void OnCompilationEnd(CompilationAnalysisContext context)
            {
                if (RecordDeclarations.Any() && ObjectCreationExpressions.Any())
                {
                    var requiredParams = RecordDeclarations
                        .Select(rds => (ti: SemanticModelContext.SemanticModel.GetDeclaredSymbol(rds), rds))
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

                        var typeInfo = SemanticModelContext.SemanticModel.GetTypeInfo(oce);

                        if (typeInfo.Type is INamedTypeSymbol nts && requiredParams.TryGetValue(nts, out var @params))
                        {
                            var present = oce.Initializer.Expressions
                                .OfType<AssignmentExpressionSyntax>()
                                .Where(e => e.Left is IdentifierNameSyntax)
                                .Select(e => e.Left.ToString());

                            var missing = @params
                                .Except(present)
                                .ToList();

                            context.ReportDiagnostic(Diagnostic.Create(Rule,
                                oce.GetLocation(),
                                typeInfo.Type,
                                string.Join(", ", missing)));
                        }
                    }
                }
            }
        }
    }
}
