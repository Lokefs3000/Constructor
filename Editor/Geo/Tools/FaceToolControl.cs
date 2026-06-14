using Editor.Geometry;
using Editor.Geometry.Mesh;
using Editor.Interaction;
using Editor.Interaction.Controls;
using Editor.Interaction.Tools;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.Geo.Tools
{
    //[ToolControlTypes(typeof(SelectedFace))]
    internal sealed class FaceToolControl : IToolControl
    {
        private List<FaceTransform> _transforms;

        public FaceToolControl()
        {
            _transforms = new List<FaceTransform>();

            ToolManager.SetTypeState<SelectedFace>(false);
        }

        public IToolTransform? Selected(object obj)
        {
            if (obj is SelectedFace face)
            {
                FaceTransform transform = new FaceTransform(face.Brush, face.FaceIndex);

                _transforms.Add(transform);
                return transform;
            }

            return null;
        }

        public void Deselected(object obj, IToolTransform transform)
        {
            if (transform is FaceTransform faceTransform)
            {
                _transforms.Remove(faceTransform);
            }
        }

        private sealed class FaceTransform : IToolTransform
        {
            private readonly Brush _brush;
            private readonly BrushFaceIndex _faceIndex;
            private readonly Vector3[] _vertices;

            private Vector3 _center;

            public FaceTransform(Brush brush, BrushFaceIndex faceIndex)
            {
                _brush = brush;
                _faceIndex = faceIndex;
                _vertices = new Vector3[4];

                for (int i = 0; i < 4; ++i)
                    _vertices[i] = brush.Vertices[BrushMeshGenerator.BrushFaceVertices[(int)faceIndex][i]];

                _center = Vector3.Zero;

                CalculateCenter();
            }

            public void SetWorldTransform(Vector3 position, Vector3 delta)
            {
                int[] indices = BrushMeshGenerator.BrushFaceVertices[(int)_faceIndex];

                Span<Vector3> span = _brush.Vertices;
                for (int i = 0; i < 4; i++)
                {
                    span[indices[i]] = _vertices[i] + delta;
                }

                _brush.NotifyUpdate(BrushUpdateFlags.All);
            }

            public void CommitTransform()
            {
                int[] indices = BrushMeshGenerator.BrushFaceVertices[(int)_faceIndex];

                Span<Vector3> span = _brush.Vertices;
                for (int i = 0; i < 4; i++)
                {
                    _vertices[i] = span[indices[i]];
                }
            }

            private void CalculateCenter()
            {
                Vector3 min = _vertices[0];
                Vector3 max = _vertices[0];

                for (int i = 1; i < _vertices.Length; ++i)
                {
                    Vector3 v = _vertices[i];

                    min = Vector3.Min(min, v);
                    max = Vector3.Max(max, v);
                }

                _center = Vector3.Lerp(min, max, 0.5f);
            }

            public Vector3 Position => _center;
            public Quaternion Rotation => Quaternion.Identity;
            public Vector3 Scale => Vector3.One;

            public Matrix4x4 WorldMatrix => Matrix4x4.CreateTranslation(_center);

            public bool IsActive => throw new NotImplementedException();
        }
    }
}
