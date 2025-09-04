using Editor.PropertiesViewer;
using Hexa.NET.ImGui;
using Primary.Components;
using Primary.Scenes;
using Serilog;
using System.Collections.Frozen;
using System.Numerics;
using System.Reflection;

namespace Editor.DearImGui.Properties
{
    internal sealed class EntityProperties : IObjectPropertiesViewer
    {
        private PropReflectionCache _reflectionCache;
        private Queue<IComponent> _modifiedComponents;

        internal EntityProperties()
        {
            _reflectionCache = new PropReflectionCache();
            _modifiedComponents = new Queue<IComponent>();
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
            foreach (object component in entity.Components)
            {
                Type type = component.GetType();
                CachedReflection reflection = _reflectionCache.Get(type);

                if (reflection.IsSerialized)
                {
                    if (ImGui.CollapsingHeader(type.Name))
                    {
                        bool hasModifiedLocalValue = false;
                        object localValue = component;

                        for (int i = 0; i < reflection.Fields.Length; i++)
                        {
                            ref ReflectionField field = ref reflection.Fields[i];
                            if (s_inspectorTypes.TryGetValue(field.Type, out InspectorDelegate? @delegate))
                            {
                                object? ret = @delegate.Invoke(field, localValue);
                                if (ret != null)
                                {
                                    hasModifiedLocalValue = true;
                                    localValue = ret;
                                }
                            }
                        }

                        if (hasModifiedLocalValue)
                            _modifiedComponents.Enqueue((IComponent)localValue);
                    }
                }
            }

            if (_modifiedComponents.Count > 0)
            {
                while (_modifiedComponents.TryDequeue(out IComponent? result))
                {
                    entity.SetComponent(result, result.GetType());
                }
            }
        }

        public void Changed(object? target)
        {

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
            } }
        }.ToFrozenDictionary();

        internal sealed record class TargetData(SceneEntity Entity);

        private delegate object? InspectorDelegate(ReflectionField Field, object Instance);
    }
}
