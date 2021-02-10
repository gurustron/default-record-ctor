using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;

namespace SoCSharp.Generators.RecordDefaultCtor.Tests.TestInfrastructure
{
    public abstract class RoslynTestBase
    {
        protected Compilation CreateCompilation(string source, params string[] additionalSources)
        {
            return CSharpCompilation.Create(
                "compilation",
                new[] {ParseText(source)}
                    .Union(additionalSources.Select(ParseText)),
                GetGlobalReferences(),
                new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            );

            SyntaxTree ParseText(string s)
            {
                return CSharpSyntaxTree.ParseText(s, new CSharpParseOptions(LanguageVersion.Preview));
            }
        }

        protected PortableExecutableReference[] GetGlobalReferences()
        {
            var assemblies = new[]
            {
                typeof(object).Assembly,
                typeof(Console).Assembly
            };

            var returnList = assemblies
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .ToList();

            //The location of the .NET assemblies
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            /*
                * Adding some necessary .NET assemblies
                * These assemblies couldn't be loaded correctly via the same construction as above,
                * in specific the System.Runtime.
                */
            returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")));
            returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")));
            returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")));
            returnList.Add(MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")));

            return returnList.ToArray();
        }
    }
}
