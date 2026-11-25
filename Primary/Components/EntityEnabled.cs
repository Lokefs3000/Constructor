using Primary.Editor;

namespace Primary.Components
{
    [Component, DontSerializeComponent]
    [InspectorHidden]
    public record struct EntityEnabled : IComponent
    {
        public bool Enabled;
    }
}
