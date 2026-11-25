namespace Editor.Shaders.Attributes
{
    public sealed class AttributeBindGroup : AttributeSignature
    {
        public AttributeBindGroup() : base(
            "bindgroup",
            AttributeUsage.GenericResource,
            Array.Empty<AttributeRelation>(),
            s_variables)
        {
        }

        private static readonly AttributeVariable[] s_variables = [
            new AttributeVariable("Group", typeof(string), null, AttributeFlags.Required),
            ];
    }
}
