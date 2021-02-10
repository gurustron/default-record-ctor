using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace SoCSharp.Generators.RecordDefaultCtor.Generate
{
    internal class RecordSyntaxReceiver : ISyntaxReceiver
    {
        public List<RecordDeclarationSyntax> RecordDeclarations { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is RecordDeclarationSyntax record)
            {
                //TODO: filter out nodes with compilation errors?

                if (!record.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    return;
                }

                if (record.HasDefaultCtor())
                {
                    return;
                }

                if (record.ParameterList is null)
                {
                    return;
                }

                RecordDeclarations.Add(record);
            }
        }

    }
}
