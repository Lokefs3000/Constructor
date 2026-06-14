using Primary.Editor;

namespace Primary.Components
{
    [Component, DontSerializeComponent]
    [InspectorHidden]
    internal record struct EntityName : IComponent
    {
        public string Name;
    }
}
