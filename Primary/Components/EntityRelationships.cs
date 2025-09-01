using Arch.Core;
using Primary.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Components
{
    [InspectorHidden]
    internal record class EntityRelationships : IComponent
    {
        internal Entity Parent;
        internal List<Entity> Children;

        public EntityRelationships()
        {
            Parent = Entity.Null;
            //TODO: use a pool for SortedList to allow for less GC when creating and destroying alot of entities
            Children = new List<Entity>();
        }
    }

    internal readonly record struct ParentOf;
}
