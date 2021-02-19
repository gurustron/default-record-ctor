using FluentAssertions;
using NUnit.Framework;
using SoCSharp.Generators.RecordDefaultCtor.Analyze;
using SoCSharp.Generators.RecordDefaultCtor.Tests.TestInfrastructure;
using System.Threading.Tasks;

namespace SoCSharp.Generators.RecordDefaultCtor.Tests
{
    public class TestAnalyzer : AnalyzerTestBase
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
        }
    }

    public partial record Record(int i)
    {
        public Record() : this(0) { }
    }
}";
            var diagnostics = await RunAnalyzer<MissingRequiredPropsInitAnalyzer>(userSource);

            diagnostics
                .Should()
                .NotBeEmpty();
        }
    }
}
