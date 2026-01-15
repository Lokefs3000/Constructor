namespace Editor.Shaders.Attributes
{
    public sealed class AttributePixel : AttributeSignature
    {
        public AttributePixel() : base(
            "pixel",
            AttributeUsage.Function,
            [new AttributeRelation(typeof(AttributeVertex), AttributeRelationFlags.Incompatible)],
            Array.Empty<AttributeVariable>())
        {
        }
    }
}
