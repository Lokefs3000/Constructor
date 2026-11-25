using Editor.Geometry;
using Editor.Geometry.Mesh;
using Editor.Gui.Inspector;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo.Inspector
{
    [InspectorHostTarget(typeof(Brush))]
    internal class BrushInspectorHost : IInspectorTypeHost<Brush>
    {
        private Brush? _brush;

        public void SetupFor(ref Brush value, object? arg)
        {
            _brush = value;
        }

        public void Cleanup()
        {
            _brush = null;
        }

        public ref BrushFace ProxyFront => ref _brush!.Faces[(int)BrushFaceIndex.Front];
        public ref BrushFace ProxyBack => ref _brush!.Faces[(int)BrushFaceIndex.Back];
        public ref BrushFace ProxyLeft => ref _brush!.Faces[(int)BrushFaceIndex.Left];
        public ref BrushFace ProxyRight => ref _brush!.Faces[(int)BrushFaceIndex.Right];
        public ref BrushFace ProxyTop => ref _brush!.Faces[(int)BrushFaceIndex.Top];
        public ref BrushFace ProxyBottom => ref _brush!.Faces[(int)BrushFaceIndex.Bottom];
    }
}
