using Primary.RHI2;

namespace Editor.Shaders.Attributes
{
    public sealed class AttributeIALayout : AttributeSignature
    {
        public AttributeIALayout() : base(
            "ialayout",
            AttributeUsage.Function,
            [new AttributeRelation(typeof(AttributeVertex), AttributeRelationFlags.Required)],
            s_variables)
        {
        }

        private static readonly AttributeVariable[] s_variables = [
            new AttributeVariable("Name", typeof(string), null, AttributeFlags.Required),
            new AttributeVariable("Offset", typeof(int), -1, AttributeFlags.None),
            new AttributeVariable("Slot", typeof(int), 0, AttributeFlags.None),
            new AttributeVariable("Class", typeof(RHIInputClass), null, AttributeFlags.None),
            new AttributeVariable("Format", typeof(RHIElementFormat), null, AttributeFlags.None),
            ];
    }
}
