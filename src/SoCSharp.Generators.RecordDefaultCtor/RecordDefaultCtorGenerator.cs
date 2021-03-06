using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SoCSharp.Generators.RecordDefaultCtor
{
    [Generator]
    public class RecordDefaultCtorGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new RecordSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not RecordSyntaxReceiver receiver)
            {
                throw new Exception();
            }

            foreach (var recordDeclaration in receiver.RecordDeclarations)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (recordDeclaration.ParameterList is null)
                {
                    continue;
                }

                var semanticModel = context.Compilation.GetSemanticModel(recordDeclaration.SyntaxTree);
                var currLocation = recordDeclaration.GetLocation();
                var currDeclaredSymbol = semanticModel.GetDeclaredSymbol(recordDeclaration);
                var canProcess = true;
                foreach (var location in currDeclaredSymbol!.Locations)
                {
                    if (currLocation.SourceSpan.Contains(location.SourceSpan) || location.SourceSpan.Contains(currLocation.SourceSpan))
                    {
                        continue;
                    }

                    var otherRecordDeclaration = location.SourceTree?.GetRoot()
                        .DescendantNodesAndSelf()
                        .OfType<RecordDeclarationSyntax>()
                        .Where(syntax =>
                            SymbolEqualityComparer.Default.Equals(
                                context.Compilation.GetSemanticModel(syntax.SyntaxTree).GetDeclaredSymbol(syntax),
                                currDeclaredSymbol))
                        .ToArray() ?? Enumerable.Empty<RecordDeclarationSyntax>();
                    if (otherRecordDeclaration.Any(syntax => syntax.HasDefaultCtor()))
                    {
                        canProcess = false;
                        break;
                    }
                }

                if (!canProcess)
                {
                    continue;
                }

                var recordName = recordDeclaration.Identifier.ToString();
                SyntaxNode root = recordDeclaration;
                List<UsingDirectiveSyntax> usings = new();
                List<string> wrappers = new();
                while (root.Parent != null)
                {
                    root = root.Parent;
                    if (root is TypeDeclarationSyntax tds)
                    {
                        if (!tds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                        {
                            throw new Exception("TADA"); // TODO
                        }
                        wrappers.Add($"{GetTypeDeclarationHeader(tds)}{Environment.NewLine}{{");
                    }

                    if (root is NamespaceDeclarationSyntax namespaceDeclaration)
                    {
                        var @namespace = namespaceDeclaration.Name.ToString();
                        wrappers.Add($"namespace {@namespace}{Environment.NewLine}{{");
                    }

                    usings.AddRange(root.ChildNodes().OfType<UsingDirectiveSyntax>());
                }

                wrappers.Reverse();

                // process parameters
                List<string> @params = new();
                var syntaxNodes = recordDeclaration.ParameterList.ChildNodes().ToList();

                foreach (var parameter in syntaxNodes.OfType<ParameterSyntax>())
                {
                    switch (parameter.Default?.Value)
                    {
                        case null:
                        case DefaultExpressionSyntax: // check if type actually matches
                        case LiteralExpressionSyntax lexs when lexs.IsKind(SyntaxKind.DefaultLiteralExpression):
                            var typeSymbol = ModelExtensions.GetTypeInfo(semanticModel, parameter.Type!).Type;
                            @params.Add($"default({typeSymbol})");
                            break;
                        default:  @params.Add(parameter.Default.Value.ToString());
                           break;
                    }
                }

                var code =
// @formatter:off
@$"
#pragma warning disable CS8019
    {string.Join(Environment.NewLine + "\t", usings)}
#pragma warning restore CS8019

    {string.Join(Environment.NewLine + "\t", wrappers)}
    {GetTypeDeclarationHeader(recordDeclaration)}
    {{
        public {recordName}() : this({string.Join(",", @params)})
        {{
        }}
    }}
    {string.Join(Environment.NewLine + "\t", Enumerable.Repeat("}", wrappers.Count))}
";
// @formatter:on
                context.AddSource($"{recordName}.Ctor.{Guid.NewGuid():N}.cs", code);
            }
        }

        private static string GetTypeDeclarationHeader(TypeDeclarationSyntax tds)
        {
            return $"{tds.Modifiers.ToString()} {tds.Keyword} {tds.Identifier}{tds.TypeParameterList?.ToString()}";
        }
    }
}
