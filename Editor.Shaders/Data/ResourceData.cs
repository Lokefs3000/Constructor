using Primary.Common;

namespace Editor.Shaders.Data
{
    public readonly record struct ResourceData(string Name, ResourceType Type, bool IsReadWrite, ValueDataRef Value, AttributeData[] Attributes, IndexRange DeclerationRange);
}
