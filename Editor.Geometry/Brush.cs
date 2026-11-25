using CommunityToolkit.HighPerformance;
using Editor.Geometry.Mesh;
using Primary.Common;
using Primary.Mathematics;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.Geometry
{
    public sealed class Brush
    {
        private readonly BrushGroup _group;

        private readonly BrushId _id;
        private readonly BrushTransform _transform;

        private readonly Vector3[] _vertices;
        private readonly BrushFace[] _faces;

        private AABB _boundingBox;

        private int _updateIndex;
        private BrushUpdateFlags _updateFlags;

        internal Brush(BrushGroup group, BrushId id, BrushTransform transform)
        {
            _group = group;

            _id = id;
            _transform = transform;

            _vertices = [
                new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(1.0f, 1.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 1.0f, 1.0f),
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(0.0f, 0.0f, 1.0f),
                new Vector3(1.0f, 0.0f, 1.0f)
                ];

            _faces = [
                new BrushFace(BrushFaceIndex.XPlus),        // X+
                new BrushFace(BrushFaceIndex.XNegative),    // X-
                new BrushFace(BrushFaceIndex.YPlus),        // Y+
                new BrushFace(BrushFaceIndex.YNegative),    // Y-
                new BrushFace(BrushFaceIndex.ZPlus),        // Z+
                new BrushFace(BrushFaceIndex.ZNegative)     // Z-
                ];

            _boundingBox = AABB.Zero;

            NotifyUpdate(BrushUpdateFlags.All);
        }

        public void NotifyUpdate(BrushUpdateFlags updateFlags)
        {
            _updateFlags |= updateFlags;
            ++_updateIndex;

            _group.Scene.InvalidateBrush();
        }

        public void RecalculateBoundingBox()
        {
            Vector3 min = _vertices.DangerousGetReferenceAt(0);
            Vector3 max = _vertices.DangerousGetReferenceAt(0);

            for (int i = 1; i < _vertices.Length; ++i)
            {
                Vector3 v = _vertices.DangerousGetReferenceAt(i);

                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

            _boundingBox = new AABB(min, max);
        }

        public BrushGroup Group => _group;

        public BrushId Id => _id;
        public BrushTransform Transform => _transform;

        public Span<Vector3> Vertices => _vertices.AsSpan();
        public Span<BrushFace> Faces => _faces.AsSpan();

        public AABB BoundingBox => _boundingBox;

        public int UpdateIndex => _updateIndex;
        public BrushUpdateFlags UpdateFlags => _updateFlags;
    }

    public readonly record struct BrushId(int SceneId, int LocalId)
    {
        public override string ToString() => $"{{s{SceneId},l{LocalId}}}";
        public override int GetHashCode() => HashCode.Combine(SceneId, LocalId);
    }

    public enum BrushFaceIndex : byte
    {
        XPlus = 0,
        XNegative,
        YPlus,
        YNegative,
        ZPlus,
        ZNegative,

        Front = ZPlus,
        Back = ZNegative,
        Left = XPlus,
        Right = XNegative,
        Top = YPlus,
        Bottom = YNegative
    }

    public enum BrushVertexIndex : byte
    {
        BackTopLeft = 0,
        BackTopRight,
        BackBottomLeft,
        BackBottomRight,

        FrontTopLeft,
        FrontTopRight,
        FrontBottomLeft,
        FrontBottomRight,
    }

    [Flags]
    public enum BrushUpdateFlags : byte
    {
        None = 0,

        FrontTopLeftChanged = 1 << 0,
        FrontTopRightChanged = 1 << 1,
        FrontBottomLeftChanged = 1 << 2,
        FrontBottomRightChanged = 1 << 3,

        BackTopLeftChanged = 1 << 4,
        BackTopRightChanged = 1 << 5,
        BackBottomLeftChanged = 1 << 6,
        BackBottomRightChanged = 1 << 7,

        All = 0xff,

        XPlus = BackTopLeftChanged | FrontTopLeftChanged | BackBottomLeftChanged | FrontBottomLeftChanged,
        XNegative = BackTopRightChanged | FrontTopRightChanged | BackBottomRightChanged | FrontBottomRightChanged,

        YPlus = FrontTopLeftChanged | FrontTopRightChanged | BackTopLeftChanged | BackTopRightChanged,
        YNegative = FrontBottomLeftChanged | FrontBottomRightChanged | BackBottomLeftChanged | BackBottomRightChanged,

        ZPlus = FrontTopLeftChanged | FrontTopRightChanged | FrontBottomLeftChanged | FrontBottomRightChanged,
        ZNegative = BackTopLeftChanged | BackTopRightChanged | BackBottomLeftChanged | BackBottomRightChanged,
    }
}
