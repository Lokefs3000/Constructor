using Primary.Editor;

namespace Primary.Components
{
    [InspectorHidden]
    internal record struct EntityEnabled : IComponent
    {
        public bool Enabled;
    }
}
