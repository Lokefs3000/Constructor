using System;
using System.Collections.Generic;
using System.Text;
using Arch.Core;
using Editor.Inspector.V2.Systems;
using Editor.Inspector.V2.Values;
using Primary.Collections.ReadOnly;
using Primary.Scenes;

namespace Editor.Inspector.V2.Entity
{
    public sealed class EntityInspectorContext : InspectorContext
    {
        private readonly SceneEntity _entity;

        private List<EntityInspectorGroup> _groups;
        private Dictionary<ComponentValueKey, IInspectorValue> _values;

        internal EntityInspectorContext(SceneEntity entity)
        {
            _entity = entity;

            _groups = new List<EntityInspectorGroup>();
            _values = new Dictionary<ComponentValueKey, IInspectorValue>();
        }

        public override void UpdateValues()
        {
            World world = World.Worlds[_entity.WrappedEntity.WorldId];
            ref EntityData ed = ref world.GetEntityData(_entity.WrappedEntity);

            for (int i = 0; i < _groups.Count; i++)
            {
                EntityInspectorGroup group = _groups[i];
                group.UpdateValues(ref ed);
            }
        }

        public ROList<EntityInspectorGroup> Groups => _groups;
    }
}
