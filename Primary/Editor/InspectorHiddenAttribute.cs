namespace Primary.Editor
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    public sealed class InspectorHiddenAttribute : Attribute
    {
        public InspectorHiddenAttribute() { }
    }
}
