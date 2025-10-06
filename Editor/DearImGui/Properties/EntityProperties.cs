using Arch.Core;
using Arch.Core.Extensions;
using Editor.Inspector;
using Editor.Inspector.Editors;
using Editor.Inspector.Entities;
using Editor.Interaction;
using Hexa.NET.ImGui;
using Primary.Components;
using Primary.Editor;
using Primary.Scenes;
using System.Numerics;
using System.Reflection;

namespace Editor.DearImGui.Properties
{
    internal sealed class EntityProperties : IObjectPropertiesViewer
    {
        private PropReflectionCache _reflectionCache;
        private ComponentEditorCache _componentEditorCache;

        private Queue<IComponent> _modifiedComponents;

        private SceneEntity _activeEntity;
        private List<ComponentEditorData> _componentEditors;

        internal EntityProperties()
        {
            _reflectionCache = new PropReflectionCache();
            _componentEditorCache = new ComponentEditorCache();

            _modifiedComponents = new Queue<IComponent>();

            //_valueInspectors = new Dictionary<Type, Func<ReflectionField, ValueStorageBase>>()
            //{
            //    { typeof(float), (x) => new VSSingle(x) },
            //    { typeof(Vector2), (x) => new VSVector2(x) },
            //    { typeof(Vector3), (x) => new VSVector3(x) },
            //    { typeof(Quaternion), (x) => new VSQuaternion(x) },
            //    { typeof(Enum), (x) => new VSEnum(x) },
            //    { typeof(RenderMesh), (x) => new VSMesh(x) },
            //    { typeof(MaterialAsset), (x) => new VSMaterial(x) },
            //}.ToFrozenDictionary();

            _activeEntity = SceneEntity.Null;

            _componentEditors = new List<ComponentEditorData>();

            SelectionManager selection = Editor.GlobalSingleton.SelectionManager;
            selection.Selected += (@base) =>
            {
                if (@base is SelectedSceneEntity selected)
                {
                    if (selected.Entity != _activeEntity)
                        Editor.GlobalSingleton.PropertiesView.SetInspected(new TargetData(selected.Entity));
                }
            };

            selection.Deselected += (@base) =>
            {
                if (@base is SelectedSceneEntity selected)
                {
                    if (selected.Entity == _activeEntity)
                    {
                        Editor.GlobalSingleton.PropertiesView.SetInspected(null);
                        _activeEntity = SceneEntity.Null;
                    }
                }
            };
        }

        public unsafe void Render(object target)
        {
            TargetData td = (TargetData)target;
            SceneEntity entity = td.Entity;

            if (entity.IsNull)
            {
                Editor.GlobalSingleton.PropertiesView.SetInspected(null);
                return;
            }

            CheckForComponentUpdates();

            bool enabled = entity.Enabled;
            if (ImGui.Checkbox("##ENABLED", ref enabled))
                entity.Enabled = enabled;

            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

            string name = entity.Name;
            if (ImGui.InputText("##NAME", ref name, 127))
                entity.Name = name;

            _modifiedComponents.Clear();

            foreach (ComponentEditorData editor in _componentEditors)
            {
                if (ImGui.CollapsingHeader(editor.ComponentType.Name, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    editor.Editor?.DrawInspector();
                    ImGui.Separator();
                }
            }

            if (_modifiedComponents.Count > 0)
            {
                UpdateEntityComponents();
            }

            ImGui.Button("Add component"u8, new Vector2(-1.0f, 0.0f));
            ImGui.OpenPopupOnItemClick("##ADDCOMP"u8, ImGuiPopupFlags.MouseButtonLeft);

            if (ImGui.BeginPopup("##ADDCOMP"u8, ImGuiWindowFlags.NoMove))
            {
                foreach (Type type in SceneEntityManager.RegisteredComponents)
                {
                    if (ImGui.Selectable(type.Name))
                    {
                        entity.AddComponent(type);
                        ImGui.CloseCurrentPopup();
                        break;
                    }
                }

                ImGui.EndPopup();
            }
        }

        public void Changed(object? target)
        {
            if (_modifiedComponents.Count > 0 && !_activeEntity.IsNull)
            {
                UpdateEntityComponents();
            }

            if (target != null && target is TargetData td && !td.Entity.IsNull)
            {
                _modifiedComponents.Clear();

                _activeEntity = td.Entity;
                _componentEditors.Clear();

                Signature types = _activeEntity.WrappedEntity.GetComponentTypes();
                for (int i = 0; i < types.Components.Length; i++)
                {
                    ComponentType type = types.Components[i];
                    AddActiveComponent(type);
                }
            }
            else
            {
                _modifiedComponents.Clear();

                _activeEntity = SceneEntity.Null;
                _componentEditors.Clear();
            }
        }

        private void AddActiveComponent(Type type)
        {
            if (type.GetCustomAttribute<InspectorHiddenAttribute>() != null)
                return;

            Type? customEditor = _componentEditorCache.FindCustomEditor(type);
            if (customEditor == null)
            {
                customEditor = typeof(DefaultComponentEditor);
            }

            try
            {
                if (Activator.CreateInstance(customEditor) is not ComponentEditor editor)
                {
                    EdLog.Gui.Error("Failed to create component editor: {ed}", customEditor);
                    return;
                }

                editor.SetupInspectorFields(_activeEntity, type);
                _componentEditors.Add(new ComponentEditorData(editor, type));
            }
            catch (Exception ex)
            {
                EdLog.Gui.Error(ex, "Exception occured creating component editor: {ed}", customEditor);
                _componentEditors.Add(new ComponentEditorData(null, type));
            }
        }

        private void RemoveActiveComponent(Type type)
        {
            int idx = _componentEditors.FindIndex((x) => x.ComponentType == type);
            if (idx != -1)
            {
                _componentEditors.RemoveAt(idx);
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

        private List<ComponentUpdate> _updates = new List<ComponentUpdate>();
        private HashSet<Type> _readyTypes = new HashSet<Type>();
        private void CheckForComponentUpdates()
        {
            _updates.Clear();
            _readyTypes.Clear();

            for (int i = 0; i < _componentEditors.Count; i++)
            {
                _readyTypes.Add(_componentEditors[i].ComponentType);
            }

            Signature types = _activeEntity.WrappedEntity.GetComponentTypes();
            for (int i = 0; i < types.Components.Length; i++)
            {
                ComponentType type = types.Components[i];
                _readyTypes.Remove(type.Type);

                if (!_componentEditors.Exists((x) => x.ComponentType == type.Type))
                {
                    _updates.Add(new ComponentUpdate(type.Type, true));
                }
            }

            foreach (Type removed in _readyTypes)
            {
                _updates.Add(new ComponentUpdate(removed, false));
            }

            if (_updates.Count > 0)
            {
                for (int i = 0; i < _updates.Count; i++)
                {
                    ComponentUpdate update = _updates[i];
                    if (update.IsNew)
                        AddActiveComponent(update.ComponentType);
                    else
                        RemoveActiveComponent(update.ComponentType);
                }
            }
        }

        internal PropReflectionCache ReflectionCache => _reflectionCache;

        internal sealed record class TargetData(SceneEntity Entity);

        private readonly record struct InspectedComponent(Type ComponentType, ValueStorageBase[] Values);
        private readonly record struct ComponentUpdate(Type ComponentType, bool IsNew);
        private readonly record struct ComponentEditorData(ComponentEditor? Editor, Type ComponentType);

        private delegate object? InspectorDelegate(ReflectionField Field, object Instance);
    }
}
