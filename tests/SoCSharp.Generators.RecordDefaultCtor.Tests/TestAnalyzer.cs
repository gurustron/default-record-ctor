using FluentAssertions;
using NUnit.Framework;
using SoCSharp.Generators.RecordDefaultCtor.Analyze;
using SoCSharp.Generators.RecordDefaultCtor.Tests.TestInfrastructure;
using System.Threading.Tasks;

namespace SoCSharp.Generators.RecordDefaultCtor.Tests
{
    public class TestAnalyzer : AnalyzerForGeneratedTestBase
    {
        [Test]
        public async Task Test()
        {
            var userSource = @"
namespace MyCode.Top.Child
{
    using System;
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(new Record{ i = 1 });
            Console.WriteLine(new Record{});
            Console.WriteLine(new Record());
            Console.WriteLine(new Record(1));
        }
    }

    public partial record Record(int i)
    {
    }
}";

            var diagnostics = await RunAnalyzer<MissingRequiredPropsInitAnalyzer>(userSource);

            diagnostics
                .Should()
                .NotBeEmpty()
                .And
                .HaveCount(2);
        }
    }
}
