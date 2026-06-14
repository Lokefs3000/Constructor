using Primary.Editor;

namespace Primary.Components
{
    [Component, DontSerializeComponent]
    [InspectorHidden]
    internal record struct EntityScene : IComponent
    {
        public int SceneId;
    }
}
