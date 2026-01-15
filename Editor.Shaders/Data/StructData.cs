using Primary.Common;

namespace Editor.Shaders.Data
{
    public readonly record struct StructData(string Name, AttributeData[] Attributes, VariableData[] Variables, IndexRange DeclerationRange);
}
