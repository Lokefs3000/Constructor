namespace Editor.Shaders.Data
{
    public readonly record struct VariableData(string Name, AttributeData[] Attributes, ValueDataRef Generic, VarSemantic? Semantic);
}
