using Editor.DearImGui.Properties;
using Hexa.NET.ImGui;
using Primary.Scenes;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
                { typeof(EntityProperties.TargetData), new EntityProperties() }
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
    }

    internal interface IObjectPropertiesViewer
    {
        public void Render(object target);
        public void Changed(object? target);
    }
}
