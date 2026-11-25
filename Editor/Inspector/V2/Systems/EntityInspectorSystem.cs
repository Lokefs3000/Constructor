using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Editor.Inspector.V2.Entity;
using Editor.Inspector.V2.Values;

namespace Editor.Inspector.V2.Systems
{
    internal sealed class EntityInspectorSystem
    {
        private List<EntityInspectorContext> _contexts;

        private Dictionary<Type, bool> _componentTypeEquality;
        private Dictionary<ComponentValueKey, bool> _componentValueEquality;

        private HashSet<ComponentValueKey> _modifiedComponents;

        private bool _allAreEqual;
        private bool _needsFullEqualityUpdate;

        public EntityInspectorSystem()
        {
            _contexts = new List<EntityInspectorContext>();

            _componentTypeEquality = new Dictionary<Type, bool>();
            _componentValueEquality = new Dictionary<ComponentValueKey, bool>();

            _modifiedComponents = new HashSet<ComponentValueKey>();

            _allAreEqual = false;
            _needsFullEqualityUpdate = false;
        }

        public bool TryInspectObject(object obj)
        {
            _contexts.Add(new EntityInspectorContext(entity));

            _allAreEqual = false;
            _needsFullEqualityUpdate = true;
        }

        public void StopInspectingObject(object obj)
        {

        }

        public void UpdateInspectorValues()
        {
            if (_needsFullEqualityUpdate)
            {
                ResolveTypeEqualities();
                _needsFullEqualityUpdate = false;
            }

            for (int i = 0; i < _contexts.Count; i++)
            {
                EntityInspectorContext context = _contexts[i];
                context.UpdateValues();
            }

            if (_modifiedComponents.Count > 0)
            {
                ResolveValueEqualities();
                _modifiedComponents.Clear();
            }
        }

        private void ResolveTypeEqualities()
        {
            _componentTypeEquality.Clear();
            _allAreEqual = false;

            if (_contexts.Count <= 1)
            {
                _allAreEqual = true;
                return;
            }

            EntityInspectorContext baseline = _contexts[0];

            for (int i = 1; i < _contexts.Count; i++)
            {
                EntityInspectorContext compare = _contexts[i];

                for (int j = 0; j < baseline.ComponentTypes.Count; j++)
                {
                    Type type = baseline.ComponentTypes[j];

                    ref bool equalityValue = ref CollectionsMarshal.GetValueRefOrAddDefault(_componentTypeEquality, type, out bool exists);
                    if (exists && !equalityValue)
                        continue;

                    if (compare.HasComponentOfType(type))
                        equalityValue = true;
                    else
                        equalityValue = false;
                }
            }
        }

        private void ResolveValueEqualities()
        {
            if (_contexts.Count <= 1)
            {
                _componentValueEquality.Clear();
                return;
            }

            EntityInspectorContext baseline = _contexts[0];
            foreach (ComponentValueKey key in _modifiedComponents)
            {
                ref bool componentEquality = ref CollectionsMarshal.GetValueRefOrNullRef(_componentTypeEquality, key.ComponentType);
                if (Unsafe.IsNullRef(in componentEquality) || !componentEquality)
                    continue;

                if (baseline.TryGetInspectorValue(key, out IInspectorValue? value))
                {
                    bool wereAllEqual = true;

                    for (int i = 1; i < _contexts.Count; i++)
                    {
                        EntityInspectorContext compared = _contexts[i];
                        if (compared.TryGetInspectorValue(key, out IInspectorValue? toCompare))
                        {
                            if (!value.Equals(toCompare))
                            {
                                wereAllEqual = false;
                                break;
                            }
                        }
                    }

                    _componentValueEquality[key] = wereAllEqual;
                }
                else
                {
                    _componentValueEquality.Remove(key);
                }
            }
        }
    }

    internal readonly record struct ComponentValueKey(Type ComponentType, string FieldPath);
}
