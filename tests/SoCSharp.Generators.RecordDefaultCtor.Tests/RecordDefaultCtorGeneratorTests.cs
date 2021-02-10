using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using SoCSharp.Generators.RecordDefaultCtor.Generate;
using SoCSharp.Generators.RecordDefaultCtor.Tests.TestInfrastructure;

namespace SoCSharp.Generators.RecordDefaultCtor.Tests
{
    public class RecordDefaultCtorGeneratorRoslynTests : GeneratorTestBase
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void SimpleGeneratorTest()
        {
            var userSource = @"
namespace MyCode.Top.Child
{
    using System;
    public class Program { public static void Main(string[] args) => Console.WriteLine(); }

    public partial record TestRecord(TestRecord1 Foo);

    public partial record TestRecord1(string Foo, int Bar);

    [AttributeUsage(AttributeTargets.Parameter)]
    public class MyAttribute:Attribute{}

    public partial record TestRecord2([MyAttribute]string Foo, int Bar)
    {
    }

    public record NotPartialRecord(string Foo);
}";
            var comp = CreateCompilation(userSource);
            var newComp = RunGenerators(comp, out var generatorDiags, new RecordDefaultCtorGenerator());

            Assert.IsEmpty(generatorDiags);
            var immutableArray = newComp.GetDiagnostics();
            Assert.IsEmpty(immutableArray);
            Assert.AreEqual(4, newComp.SyntaxTrees.Count());
        }

        [Test]
        public void RecordWithDefaultArgumentsTest()
        {
            var cases = new[]
                {
                    "(int I = default)",
                    "(int I = default(int))",
                    "(int I = 3)",
                    "(string I = Program.Constant)"
                }
                .Select((s, i) => $"public partial record Record{i}{s};")
                .ToList();

            var userSource = $@"
using System;
namespace MyCode.Top.Child
{{
#pragma warning disable CS8019
    using System.Collections.Generic;
#pragma warning restore CS8019
    public class Program
    {{
        public const string Constant = ""Test"";
        public static void Main(string[] args) => Console.WriteLine();
    }}

    {string.Join(Environment.NewLine, cases)}
}}";
            var comp = CreateCompilation(userSource);
            var newComp = RunGenerators(comp, out var generatorDiags, new RecordDefaultCtorGenerator());

            Assert.IsEmpty(generatorDiags);
            var immutableArray = newComp.GetDiagnostics();
            Assert.IsEmpty(immutableArray);
            Assert.AreEqual(cases.Count + 1, newComp.SyntaxTrees.Count());
        }

        [Test]
        public void RecordWithTypeParamsGeneratorTest()
        {
            var cases = new[]
                {
                    "<T>(string Foo)",
                    "<T>(int I, T Foo)",
                    "<T>(string Foo)",
                    "<T>(int I, T Foo)",
                    "<T, R>(int I, T Foo, T Foo1, R Bar)",
                    "(List<int> Ints)",
                    "<T>(List<T> Ts)",
                    "<T>(Dictionary<int,T> Ts)",
                    "<T,R>(Dictionary<T,R> Rs)"
                }
                .Select((s, i) => $"public partial record Record{i}{s};")
                .ToList();

            var userSource = $@"
namespace MyCode.Top.Child
{{
    using System;
    using System.Collections.Generic;
    public class Program {{ public static void Main(string[] args) => Console.WriteLine(); }}

    {string.Join(Environment.NewLine, cases)}
}}";
            var comp = CreateCompilation(userSource);
            var newComp = RunGenerators(comp, out var generatorDiags, new RecordDefaultCtorGenerator());

            Assert.IsEmpty(generatorDiags);
            var immutableArray = newComp.GetDiagnostics();
            Assert.IsEmpty(immutableArray);
            Assert.AreEqual(cases.Count + 1, newComp.SyntaxTrees.Count());
        }

        [Test]
        public void RecordWithDefaultCtor_Skipped()
        {
            var userSource = @"
namespace MyCode.Top.Child
{
    using System;
    public class Program { public static void Main(string[] args) => Console.WriteLine(); }

    public partial record Record(int i)
    {
        public Record() : this(1){}
    }
}";
            var comp = CreateCompilation(userSource);
            var newComp = RunGenerators(comp, out var generatorDiags, new RecordDefaultCtorGenerator());

            Assert.IsEmpty(generatorDiags);
            var immutableArray = newComp.GetDiagnostics();
            Assert.IsEmpty(immutableArray);
            Assert.AreEqual(1, newComp.SyntaxTrees.Count());
        }

        [Test]
        public void RecordNonPartial_Skipped()
        {
            var userSource = @"
namespace MyCode.Top.Child
{
    using System;
    public class Program { public static void Main(string[] args) => Console.WriteLine(); }

    public record Record(int i)
    {
    }
}";
            var comp = CreateCompilation(userSource);
            var newComp = RunGenerators(comp, out var generatorDiags, new RecordDefaultCtorGenerator());

            Assert.IsEmpty(generatorDiags);
            var immutableArray = newComp.GetDiagnostics();
            Assert.IsEmpty(immutableArray);
            Assert.AreEqual(1, newComp.SyntaxTrees.Count());
        }

        [Test]
        public void RecordNested_Generates()
        {
            var userSource = @"
namespace MyCode.Top.Child
{
    using System;
    public class Program { public static void Main(string[] args) => Console.WriteLine(); }
    namespace InnerNamespace
    {
        public partial class Outer
        {
            public partial record Record(int I);
        }
    }
}";
            var comp = CreateCompilation(userSource);
            var newComp = RunGenerators(comp, out var generatorDiags, new RecordDefaultCtorGenerator());

            DefaultAssert(generatorDiags, newComp, 2);
        }

        [Test]
        public void RecordMultipleFiles_Generates()
        {
            var firstFile = @"
namespace MyCode.Top.Child
{
    using System;
    public class Program { public static void Main(string[] args) => Console.WriteLine(); }
    public partial record Record(int I);
}";
            var secondFile = @"
namespace MyCode.Top.Child
{
    public partial record Record {}
}";

            var comp = CreateCompilation(firstFile, secondFile);
            var newComp = RunGenerators(comp, out var generatorDiags, new RecordDefaultCtorGenerator());

            DefaultAssert(generatorDiags, newComp, 3);
        }

        [Test]
        public void RecordMultipleFiles_DoesNotGenerate()
        {
            var firstFile = @"
namespace MyCode.Top.Child
{
    using System;
    public class Program { public static void Main(string[] args) => Console.WriteLine(); }
    public partial record Record(int I);
}";
            var secondFile = @"
namespace MyCode.Top.Child
{
    public partial record Record
    {
        public Record():this(1){}
    }
}";

            var comp = CreateCompilation(firstFile, secondFile);
            var newComp = RunGenerators(comp, out var generatorDiags, new RecordDefaultCtorGenerator());

            DefaultAssert(generatorDiags, newComp, 2);
        }

        private static void DefaultAssert(
            ImmutableArray<Diagnostic> generatorDiagnostics,
            Compilation compilation,
            int expectedSyntaxTreesCount)
        {
            Assert.IsEmpty(generatorDiagnostics);
            var immutableArray = compilation.GetDiagnostics();
            Assert.IsEmpty(immutableArray);
            Assert.AreEqual(expectedSyntaxTreesCount, compilation.SyntaxTrees.Count());
        }

        // - create analyzer for required fields
        // - multiples files
        // - global namespace
        // - namespace collision ??
        // - custom ctor with same number of parameters but
    }
}
