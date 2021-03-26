using FluentAssertions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using SoCSharp.Generators.RecordDefaultCtor.Analyze;
using SoCSharp.Generators.RecordDefaultCtor.Tests.TestInfrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace SoCSharp.Generators.RecordDefaultCtor.Tests
{
    public class TestAnalyzer : AnalyzerForGeneratedTestBase
    {
        [Test]
        public async Task HappyPath()
        {
            var userSource = @"
namespace MyCode.Top.Child
{
    using System;
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(new Record(1));
            Console.WriteLine(new Record{ i = 1 });
            Console.WriteLine(new Record1{ i = 1 });
        }
    }

    public partial record Record(int i)
    {
    }

    public partial record Record1(int i, int j = 1)
    {
    }
}";

            var diagnostics = await RunAnalyzer<MissingRequiredPropsInitAnalyzer>(userSource);

            diagnostics
                .Should()
                .BeEmpty();
        }

        [Test]
        public async Task EmptyInitializer_ReturnsError_AllProperties()
        {
            var userSource = @"
namespace MyCode.Top.Child
{
    using System;
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(new Record{});
        }
    }

    public partial record Record(int i, int j)
    {
    }
}";

            var diagnostics = await RunAnalyzer<MissingRequiredPropsInitAnalyzer>(userSource);
            diagnostics
                .Should()
                .HaveCount(1)
                .And.AllBeEquivalentTo(new
                {
                    Descriptor = MissingRequiredPropsInitAnalyzer.Rule,
                    Id = DiagnosticIds.MissingRequiredPropsInitAnalyzerRuleId,
                    Arguments = new[] {"MyCode.Top.Child.Record", "i, j"}
                });
        }

        [Test]
        public async Task MissingPropsInitializer_ReturnsError_Missing()
        {
            var userSource = @"
namespace MyCode.Top.Child
{
    using System;
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(new Record());
        }
    }

    public partial record Record(int i)
    {
    }
}";

            var diagnostics = await RunAnalyzer<MissingRequiredPropsInitAnalyzer>(userSource);
            diagnostics
                .Should()
                .HaveCount(1)
                .And.AllBeEquivalentTo(new
                {
                    Descriptor = MissingRequiredPropsInitAnalyzer.Rule,
                    Id = DiagnosticIds.MissingRequiredPropsInitAnalyzerRuleId,
                    Arguments = new[] {"MyCode.Top.Child.Record", "i"}
                });
        }
    }
}
