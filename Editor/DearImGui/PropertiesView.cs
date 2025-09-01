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

        private FrozenDictionary<Type, IObjectPropertiesViewer> _viewers;

        internal PropertiesView()
        {
            _target = null;

            _viewers = new Dictionary<Type, IObjectPropertiesViewer>
            {
                { typeof(TextureProperties.TargetData), new TextureProperties() },
                { typeof(EntityProperties.TargetData), new EntityProperties() }
            }.ToFrozenDictionary();
        }

        internal void Render()
        {
            if (ImGui.Begin("Properties"))
            {
                if (_target != null)
                {
                    Type type = _target.GetType();
                    if (_viewers.TryGetValue(type, out IObjectPropertiesViewer? viewer))
                        viewer.Render(_target);
                }
            }
            ImGui.End();
        }

        internal void SetInspected(object? data)
        {
            _target = data;
        }
    }

    internal interface IObjectPropertiesViewer
    {
        public void Render(object target);
    }
}
