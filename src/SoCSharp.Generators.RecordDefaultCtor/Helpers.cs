using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace SoCSharp.Generators.RecordDefaultCtor
{
    internal static class Helpers
    {
        public static bool HasDefaultCtor(this RecordDeclarationSyntax recordDeclarationSyntax)
            => recordDeclarationSyntax.ChildNodes().Any(IsDefaultCtor);

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
