namespace Editor.Shaders.Attributes
{
    public sealed class AttributeConstants : AttributeSignature
    {
        public AttributeConstants() : base(
            "constants",
            AttributeUsage.ConstantBuffer,
            Array.Empty<AttributeRelation>(),
            Array.Empty<AttributeVariable>())
        {
        }
    }
}
