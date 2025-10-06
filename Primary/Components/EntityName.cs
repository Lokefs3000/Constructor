using Primary.Editor;

namespace Primary.Components
{
    [InspectorHidden]
    internal record struct EntityName : IComponent
    {
        public string Name;
    }
}
