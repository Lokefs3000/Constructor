using Editor.Geometry;
using Editor.Geometry.Mesh;
using Editor.Gui.Inspector;
using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Geo.Inspector
{
    internal sealed class FaceInspectorHost : IInspectorTypeHost<BrushFace>
    {
        private Brush? _brush;
        private BrushFaceIndex _faceIndex;

        public void SetupFor(ref BrushFace value, object? arg)
        {
            if (arg is Brush brush)
            {
                _brush = brush;
                _faceIndex = value.FaceIndex;
            }
        }

        public void Cleanup()
        {
            _brush = null;
            _faceIndex = 0;
        }

        public MaterialAsset? ProxyMaterial
        {
            get => _brush!.Faces[(int)_faceIndex].Material;
            set
            {
                _brush!.Faces[(int)_faceIndex].Material = value;
                _brush.NotifyUpdate((BrushUpdateFlags)BrushMeshGenerator.BrushFaceMasks[(int)_faceIndex]);
            }
        }

        public Vector2 ProxyUVScale
        {
            get => _brush!.Faces[(int)_faceIndex].UVScale;
            set
            {
                _brush!.Faces[(int)_faceIndex].UVScale = value;
                _brush.NotifyUpdate((BrushUpdateFlags)BrushMeshGenerator.BrushFaceMasks[(int)_faceIndex]);
            }
        }

        public Vector2 ProxyUVOffset
        {
            get => _brush!.Faces[(int)_faceIndex].UVOffset;
            set
            {
                _brush!.Faces[(int)_faceIndex].UVOffset = value;
                _brush.NotifyUpdate((BrushUpdateFlags)BrushMeshGenerator.BrushFaceMasks[(int)_faceIndex]);
            }
        }

        public BrushFaceFlags ProxyFlags
        {
            get => _brush!.Faces[(int)_faceIndex].Flags;
            set
            {
                _brush!.Faces[(int)_faceIndex].Flags = value;
                _brush.NotifyUpdate((BrushUpdateFlags)BrushMeshGenerator.BrushFaceMasks[(int)_faceIndex]);
            }
        }
    }
}
