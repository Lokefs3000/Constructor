namespace Editor.Shaders.Attributes
{
    public sealed class AttributeVertex : AttributeSignature
    {
        public AttributeVertex() : base(
            "vertex",
            AttributeUsage.Function,
            [new AttributeRelation(typeof(AttributePixel), AttributeRelationFlags.Incompatible)],
            Array.Empty<AttributeVariable>())
        {
        }
    }
}
