using Arch.Core;
using CommunityToolkit.HighPerformance;
using Editor.Inspector.Entities;
using Editor.PropertiesViewer;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Components;
using Primary.Scenes;
using Serilog;
using System.Buffers;
using System.Collections.Frozen;
using System.Numerics;
using System.Reflection;
using Vortice.Mathematics;

namespace Editor.DearImGui.Properties
{
    internal sealed class EntityProperties : IObjectPropertiesViewer
    {
        private PropReflectionCache _reflectionCache;
        private Queue<IComponent> _modifiedComponents;

        private FrozenDictionary<Type, Func<object, ReflectionField, ValueStorageBase>> _valueInspectors;

        private SceneEntity _activeEntity;
        private List<InspectedComponent> _activeValues;

        internal EntityProperties()
        {
            _reflectionCache = new PropReflectionCache();
            _modifiedComponents = new Queue<IComponent>();

            _valueInspectors = new Dictionary<Type, Func<object, ReflectionField, ValueStorageBase>>()
            {
                { typeof(Vector3), (x, y) => new VSVector3(x, y) }
            }.ToFrozenDictionary();

            _activeEntity = SceneEntity.Null;
            _activeValues = new List<InspectedComponent>();
        }

        public unsafe void Render(object target)
        {
            TargetData td = (TargetData)target;
            SceneEntity entity = td.Entity;

            bool enabled = entity.Enabled;
            if (ImGui.Checkbox("##ENABLED", ref enabled))
                entity.Enabled = enabled;

            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

            string name = entity.Name;
            if (ImGui.InputText("##NAME", ref name, 127))
                entity.Name = name;

            _modifiedComponents.Clear();

            Span<InspectedComponent> components = _activeValues.AsSpan();
            for (int i = 0; i < components.Length; i++)
            {
                ref InspectedComponent inspected = ref components[i];
                if (ImGui.CollapsingHeader(inspected.Component.GetType().Name))
                {
                    bool hasModifiedLocalValue = false;
                    object? localValue = _activeEntity.GetComponent(inspected.Component.GetType());
                    if (localValue == null)
                    {
                        ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "null");
                    }
                    else
                    {
                        for (int j = 0; j < inspected.Values.Length; j++)
                        {
                            ValueStorageBase @base = inspected.Values[i];

                            object? updated = @base.Render(@base.Field.Name, ref localValue);
                            if (updated != null)
                            {
                                hasModifiedLocalValue = true;
                                localValue = updated;
                            }
                        }

                        if (hasModifiedLocalValue)
                            _modifiedComponents.Enqueue((IComponent)localValue);
                    }
                }
            }
        }

        public void Changed(object? target)
        {
            if (_modifiedComponents.Count > 0 && !_activeEntity.IsNull)
            {
                UpdateEntityComponents();
            }

            if (target is TargetData td && !td.Entity.IsNull)
            {
                _modifiedComponents.Clear();

                _activeEntity = SceneEntity.Null;
                _activeValues.Clear();

                foreach (object component in td.Entity.Components)
                {
                    Type type = component.GetType();
                    CachedReflection reflection = _reflectionCache.Get(type);

                    if (reflection.IsSerialized)
                    {
                        using PoolArray<ValueStorageBase> values = new PoolArray<ValueStorageBase>(ArrayPool<ValueStorageBase>.Shared.Rent(reflection.Fields.Length), true);

                        int length = 0;
                        for (int i = 0; i < reflection.Fields.Length; i++)
                        {
                            ReflectionField field = reflection.Fields[i];
                            if (_valueInspectors.TryGetValue(field.Type, out var constructor))
                                values[length++] = constructor(component, field);
                        }

                        if (length == 0)
                        {
                            _activeValues.Add(new InspectedComponent(component, Array.Empty<ValueStorageBase>()));
                        }
                        else
                        {
                            _activeValues.Add(new InspectedComponent(component, values.AsSpan(0, length).ToArray()));
                        }
                    }
                }
            }
            else
            {
                _modifiedComponents.Clear();

                _activeEntity = SceneEntity.Null;
                _activeValues.Clear();
            }
        }

        private void UpdateEntityComponents()
        {
            if (_activeEntity.IsNull)
                return;

            while (_modifiedComponents.TryDequeue(out IComponent? result))
            {
                _activeEntity.SetComponent(result, result.GetType());
            }
        }

        private static readonly FrozenDictionary<Type, InspectorDelegate> s_inspectorTypes = new Dictionary<Type, InspectorDelegate>
        {
            { typeof(float), (field, inst) =>
            {
                float value = (float)field.GetValue(inst)!;
                if (ImGuiWidgets.InputFloat(field.Name, ref value))
                {
                    field.SetValue(inst, value);
                    return inst;
                }

                return null;
            } },
            { typeof(Vector2), (field, inst) =>
            {
                Vector2 value = (Vector2)field.GetValue(inst)!;
                if (ImGuiWidgets.InputVector2(field.Name, ref value))
                {
                    field.SetValue(inst, value);
                    return inst;
                }

                return null;
            } },
            { typeof(Vector3), (field, inst) =>
            {
                Vector3 value = (Vector3)field.GetValue(inst)!;
                if (ImGuiWidgets.InputVector3(field.Name, ref value))
                {
                    field.SetValue(inst, value);
                    return inst;
                }

                return null;
            } },
            { typeof(Quaternion), (field, inst) =>
            {
                Quaternion value = (Quaternion)field.GetValue(inst)!;
                Vector3 v3 = Vector3.RadiansToDegrees(value.ToEuler());
                if (ImGuiWidgets.InputVector3(field.Name, ref v3))
                {
                    v3 = Vector3.DegreesToRadians(v3);
                    field.SetValue(inst, Quaternion.CreateFromYawPitchRoll(v3.Y, v3.X, v3.Z));
                    return inst;
                }

                return null;
            } }
        }.ToFrozenDictionary();

        internal sealed record class TargetData(SceneEntity Entity);

        private readonly record struct InspectedComponent(object Component, ValueStorageBase[] Values);

        private delegate object? InspectorDelegate(ReflectionField Field, object Instance);
    }
}
