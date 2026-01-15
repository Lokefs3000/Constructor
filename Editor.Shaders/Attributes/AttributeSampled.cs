namespace Editor.Shaders.Attributes
{
    public sealed class AttributeSampled : AttributeSignature
    {
        public AttributeSampled() : base(
            "sampled",
            AttributeUsage.GenericTexture,
            [new AttributeRelation(typeof(AttributeProperty), AttributeRelationFlags.Required)],
            s_variables)
        {
        }

        private static readonly AttributeVariable[] s_variables = [
            new AttributeVariable("Sampler", typeof(string), null, AttributeFlags.None),
            ];
    }
}
