using Editor.Geometry;
using Editor.Gui.Inspector;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo.Inspector
{
    [InspectorTarget(typeof(BrushFace))]
    internal sealed class FaceInspector : ICustomInspector<BrushFace>
    {
        public void SetupInspectorData(InspectorContext context, Type type)
        {
            context.PropertyField("Material", nameof(BrushFace.Material));

            context.PropertyField("UVScale", nameof(BrushFace.UVScale));
            context.PropertyField("UVOffset", nameof(BrushFace.UVOffset));

            context.PropertyField("Flags", nameof(BrushFace.Flags));
        }
    }
}
