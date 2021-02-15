using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace SoCSharp.Generators.RecordDefaultCtor
{
    internal static class Helpers
    {
        public static bool HasDefaultCtor(this RecordDeclarationSyntax recordDeclarationSyntax)
            => recordDeclarationSyntax.ChildNodes().Any(IsDefaultCtor);

        public static bool IsSuitable(this RecordDeclarationSyntax recordSyntax, bool shouldNotHaveDefaultCtor = true)
        {
            if (!recordSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                return false;
            }

            if (shouldNotHaveDefaultCtor && recordSyntax.HasDefaultCtor())
            {
                return false;
            }

            return recordSyntax.ParameterList is not null;
        }

        private static bool IsDefaultCtor(SyntaxNode node)
        {
            if (node is ConstructorDeclarationSyntax ctr && !ctr.ParameterList.ChildNodes().Any())
            {
                return true;
            }

            return false;
        }
    }
}
