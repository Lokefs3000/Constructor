using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Arch.Core;
using Primary.Components;
using Primary.Scenes;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Inspector.Contexts.Entity
{
    public abstract class ComponentInspectorGroup : InspectorGroup
    {
        protected internal abstract void UpdateAllValues(ref EntityData entityData);
    }

    public sealed class ComponentInspectorGroup<TComponent> : ComponentInspectorGroup where TComponent :  IComponent
    {
        private readonly int _uniqueHash;

        internal ComponentInspectorGroup(SceneEntity entity, Type type)
        {
            _uniqueHash = HashCode.Combine(entity, type);

            SetupValuesFor<TComponent>();
        }

        protected internal override void UpdateAllValues(ref EntityData entityData)
        {
            ref TComponent comp = ref entityData.Get<TComponent>();
            if (!Unsafe.IsNullRef(in comp))
            {
                if (typeof(TComponent).IsClass)
                {
                    UpdateValuesOfAll(comp);
                }
                else
                {
                    UpdateValuesOfAll(ref Unsafe.As<TComponent, OpaqueRef>(ref comp));
                }
            }
        }

        public override int UniqueHash => _uniqueHash;
    }
}
