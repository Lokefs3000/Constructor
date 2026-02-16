using Editor.DearImGui.Properties;
using Hexa.NET.ImGui;
using System.Collections.Frozen;

namespace Editor.DearImGui
{
    internal sealed class PropertiesView
    {
        private object? _target;
        private bool _targetChanged;

        private FrozenDictionary<Type, IObjectPropertiesViewer> _viewers;

        internal PropertiesView()
        {
            _target = null;
            _targetChanged = false;

            _viewers = new Dictionary<Type, IObjectPropertiesViewer>
            {
                { typeof(TextureProperties.TargetData), new TextureProperties() },
                { typeof(ShaderProperties.TargetData), new ShaderProperties() },
                { typeof(ModelProperties.TargetData), new ModelProperties() },
                { typeof(EntityProperties.TargetData), new EntityProperties() },
                { typeof(GeoEditProperties.TargetData), new GeoEditProperties() },
                { typeof(MaterialProperties.TargetData), new MaterialProperties() },
                { typeof(EffectVolumeProperties.TargetData), new EffectVolumeProperties() },
            }.ToFrozenDictionary();
        }

        internal void Render()
        {
            if (ImGui.Begin("Properties"))
            {
                if (_target != null)
                {
                    if (_targetChanged)
                    {
                        foreach (var kvp in _viewers)
                        {
                            kvp.Value.Changed(_target);
                        }

                        _targetChanged = false;
                    }

                    Type type = _target.GetType();
                    if (_viewers.TryGetValue(type, out IObjectPropertiesViewer? viewer))
                    {
                        viewer.Render(_target);
                    }
                }
            }
            ImGui.End();
        }

        internal void SetInspected(object? data)
        {
            _targetChanged = _targetChanged || _target != data;
            _target = data;
        }

        internal T? GetPropertiesViewer<T>(Type targetData) where T : class, IObjectPropertiesViewer
        {
            if (_viewers.TryGetValue(targetData, out IObjectPropertiesViewer? value))
                return value as T;
            return null;
        }
    }

    internal interface IObjectPropertiesViewer
    {
        public void Render(object target);
        public void Changed(object? target);
    }
}
