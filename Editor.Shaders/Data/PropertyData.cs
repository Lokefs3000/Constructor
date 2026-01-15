using Primary.Common;

namespace Editor.Shaders.Data
{
    public readonly record struct PropertyData(string Name, ValueDataRef Generic, AttributeData[] Attributes, IndexRange DeclerationRange);
}
