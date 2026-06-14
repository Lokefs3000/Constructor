using Editor.Geo.Selection;
using Editor.Geometry;
using Editor.Interaction;
using Editor.Interaction.Controls;
using Editor.Interaction.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo.Tools
{
    [ToolControlTypes(typeof(Brush))]
    internal sealed class GeoToolControl : IToolControl, IGeoTool, IDisposable
    {
        private readonly GeoSelectionGroup _selectionGroup;
        private Dictionary<Brush, BrushToolTransform> _transforms;

        public GeoToolControl()
        {
            GeoSceneManager manager = EditorRuntime.GlobalSingleton.GeoSceneManager;

            _transforms = new Dictionary<Brush, BrushToolTransform>();
            _selectionGroup = manager.SelectionGroup;

            _selectionGroup.SelectionUpdated += OnSelectionUpdated;
        }

        public void Dispose()
        {

        }

        public IToolTransform? Selected(object obj)
        {
            if (obj is not Brush brush)
                return null;

            if (!_selectionGroup.TryGetSelectionData(brush, out BrushSelectionData activeData))
                return null;

            if (!_transforms.TryGetValue(brush, out BrushToolTransform? toolTransform))
            {
                toolTransform = new BrushToolTransform(brush, activeData.VertexData);
                _transforms.Add(brush, toolTransform);
            }

            return toolTransform;
        }

        public void Deselected(object obj, IToolTransform transform)
        {
            if (obj is not Brush brush)
                return;

            _transforms.Remove(brush);
        }

        private void OnSelectionUpdated(Brush brush, BrushSelectionData activeData)
        {
            if (_transforms.TryGetValue(brush, out BrushToolTransform? toolTransform))
                toolTransform.UpdateVertexSelection(activeData.VertexData);
        }

        internal void UpdateVertices(Brush brush)
        {
            if (_transforms.TryGetValue(brush, out BrushToolTransform? toolTransform))
                toolTransform.UpdateVertices();
        }
    }
}
