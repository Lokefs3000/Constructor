using Editor.GeoEdit;
using Editor.Geometry;
using Editor.Interaction;
using Hexa.NET.ImGui;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui.Properties
{
    internal class GeoEditProperties : IObjectPropertiesViewer
    {
        private GeoBrush? _activeBrush;

        internal GeoEditProperties()
        {
            SelectionManager.NewSelected += (@base) =>
            {
                if (@base is SelectedGeoBrush selected)
                {
                    Editor.GlobalSingleton.PropertiesView.SetInspected(new TargetData(selected.Brush));
                }
            };

            SelectionManager.OldDeselected += (@base) =>
            {
                if (@base is SelectedGeoBrush selected)
                {
                    if (selected.Brush == _activeBrush)
                    {
                        Editor.GlobalSingleton.PropertiesView.SetInspected(null);
                        _activeBrush = null;
                    }
                }
            };
        }

        public void Render(object target)
        {
            TargetData td = (TargetData)target;

            
        }

        public void Changed(object? target)
        {
            if (target is TargetData td)
            {
                _activeBrush = td.Brush;
            }
            else
            {
                _activeBrush = null;
            }
        }

        internal sealed record class TargetData(GeoBrush Brush);
    }
}
