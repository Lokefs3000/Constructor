using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Primary.SourceGenerator
{
    [Generator]
    public class CompInspectorSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "InspectableComponentAttribute.g.cs",
                SourceText.From(Attribute, Encoding.UTF8)));

        }

        public const string Attribute = @"
namespace Primary.SourceGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
    public class InspectableComponentAttribute : System.Attribute
    {
    }
}";
    }
}
