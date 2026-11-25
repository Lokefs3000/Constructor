using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Arch.Core;
using Editor.Inspector.V2.Values;
using Primary.Components;

namespace Editor.Inspector.V2.Entity
{
    public sealed class EntityInspectorGroup<T> : EntityInspectorGroup where T : struct, IComponent
    {
        internal EntityInspectorGroup(Type componentType) : base(componentType)
        {
        }

        internal override void UpdateValues(ref EntityData ed)
        {
            ref T comp = ref ed.Get<T>();
            for (int i = 0; i < _values.Count; i++)
            {
                IInspectorValue inspectorValue = _values[i];
                inspectorValue.UpdateValueFromValueType(ref Unsafe.As<T, OpaqueRef>(ref comp));
            }
        }
    }

    public abstract class EntityInspectorGroup : InspectorGroup
    {
        protected readonly Type _componentType;

        internal EntityInspectorGroup(Type componentType)
        {
            _componentType = componentType;
        }

        internal abstract void UpdateValues(ref EntityData ed);

        public Type ComponentType => _componentType;
    }
}
