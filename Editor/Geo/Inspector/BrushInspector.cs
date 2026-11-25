using Editor.Geometry;
using Editor.Gui.Inspector;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo.Inspector
{
    [InspectorTarget(typeof(Brush))]
    internal sealed class BrushInspector : ICustomInspector<Brush>
    {
        public void SetupInspectorData(InspectorContext context, Type type)
        {
            context.PropertyField("Front", nameof(Brush.Faces), (int)BrushFaceIndex.Front);
            context.PropertyField("Back", nameof(Brush.Faces), (int)BrushFaceIndex.Back);
            context.PropertyField("Left", nameof(Brush.Faces), (int)BrushFaceIndex.Left);
            context.PropertyField("Right", nameof(Brush.Faces), (int)BrushFaceIndex.Right);
            context.PropertyField("Top", nameof(Brush.Faces), (int)BrushFaceIndex.Top);
            context.PropertyField("Bottom", nameof(Brush.Faces), (int)BrushFaceIndex.Bottom);
        }
    }
}
