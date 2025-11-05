using Primary.Editor;

namespace Primary.Components
{
    [InspectorHidden]
    public record struct EntityEnabled : IComponent
    {
        public bool Enabled;
    }
}
