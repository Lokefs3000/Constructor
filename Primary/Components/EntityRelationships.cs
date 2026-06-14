using Arch.Core;
using Primary.Editor;

namespace Primary.Components
{
    [Component, DontSerializeComponent]
    [InspectorHidden]
    internal record struct EntityRelationships : IComponent
    {
        internal Entity Parent;
        internal readonly List<Entity> Children;

        public EntityRelationships()
        {
            Parent = Entity.Null;
            Children = [];
        }
    }

    internal readonly record struct ParentOf;
}
