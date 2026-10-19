using Primary.Common;

namespace Editor.Shaders.Data
{
    public record struct FunctionData(string Name, AttributeData[] Attributes, VariableData[] Arguments, FunctionIncludeData IncludeData, IndexRange BodyRange, IndexRange DeclerationRange);
}
