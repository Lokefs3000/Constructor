using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Arch.Core;
using Primary;
using Primary.Collections.ReadOnly;
using Primary.Editor;
using Primary.Scenes;
using Primary.Scenes.Components;

namespace PrimaryEditor.Inspector.Contexts.Entity
{
    public sealed class EntityInspectorContext : InspectorContext
    {
        private readonly SceneEntity _entity;

        private readonly List<InspectorGroup> _groups;

        internal EntityInspectorContext(SceneEntity entity)
        {
            _entity = entity;

            _groups = new List<InspectorGroup>();

            foreach (ComponentType type in entity.ComponentTypes)
            {
                if (type.Type.GetCustomAttribute<InspectorHiddenAttribute>() != null)
                    continue;

                Type groupType = typeof(ComponentInspectorGroup<>).MakeGenericType(type.Type);
                ComponentInspectorGroup group = (ComponentInspectorGroup)Activator.CreateInstance(groupType, BindingFlags.Instance | BindingFlags.NonPublic, null, [entity, type.Type], null)!;

                _groups.Add(group);
            }

            SceneEntityManager.Events |= EntityEvents.EntityComponents;
            SceneEntityManager.ComponentAdded += OnComponentAdded;
            SceneEntityManager.ComponentRemoved -= OnComponentRemoved;
        }

        private void OnComponentAdded(SceneEntity entity, Type type)
        {
            if (type.GetCustomAttribute<InspectorHiddenAttribute>() != null)
                return;

            Type groupType = typeof(ComponentInspectorGroup<>).MakeGenericType(type);
            ComponentInspectorGroup group = (ComponentInspectorGroup)Activator.CreateInstance(groupType, BindingFlags.Instance | BindingFlags.NonPublic, null, [entity, type], null)!;

            _groups.Add(group);
        }

        private void OnComponentRemoved(SceneEntity entity, Type type)
        {
            for (int i = 0; i < _groups.Count; ++i)
            {
                if (_groups[i].Type == type)
                {
                    _groups.RemoveAt(i);
                    break;
                }
            }
        }

        public override void UpdateValues()
        {
            World world = Engine.GlobalSingleton.SceneManager.World;

            ref EntityData entityData = ref world.GetEntityData(_entity.WrappedEntity);
            foreach (ComponentInspectorGroup group in _groups)
            {
                group.UpdateAllValues(ref entityData);
            }
        }

        public override bool MatchesContext<T>(ref T value)
        {
            return value is SceneEntity entity && entity == _entity;   
        }

        public override ROList<InspectorGroup> Groups => _groups;

        public SceneEntity Entity => _entity;
    }
}
