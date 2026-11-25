using Editor.Geometry;
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
    //[ToolControlTypes(typeof(Brush))]
    internal sealed class BrushToolControl : IToolControl
    {
        private List<BrushTransform> _transforms;

        public BrushToolControl()
        {
            _transforms = new List<BrushTransform>();

            ToolManager.SetTypeState<Brush>(false);
        }

        public IToolTransform? Selected(object obj)
        {
            if (obj is Brush brush)
            {
                BrushTransform transform = new BrushTransform(brush);

                _transforms.Add(transform);
                return transform;
            }

            return null;
        }

        public void Deselected(object obj, IToolTransform transform)
        {
            if (transform is BrushTransform brushTransform)
            {
                _transforms.Remove(brushTransform);
            }
        }

        private sealed class BrushTransform : IToolTransform
        {
            private readonly Brush _brush;
            private readonly Vector3[] _vertices;

            private Vector3 _center;

            public BrushTransform(Brush brush)
            {
                _brush = brush;
                _vertices = brush.Vertices.ToArray();

                _center = Vector3.Zero;

                CalculateCenter();
            }

            public void SetWorldTransform(Vector3 position, Vector3 delta)
            {
                Span<Vector3> span = _brush.Vertices;
                for (int i = 0; i < span.Length; i++)
                {
                    span[i] = _vertices[i] + delta;
                }

                _brush.NotifyUpdate(BrushUpdateFlags.All);
            }

            public void CommitTransform()
            {
                Span<Vector3> span = _brush.Vertices;
                for (int i = 0; i < span.Length; i++)
                {
                    _vertices[i] = span[i];
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

            public Vector3 Position => _brush.BoundingBox.Center;
            public Quaternion Rotation => Quaternion.Identity;
            public Vector3 Scale => Vector3.One;

            public Matrix4x4 WorldMatrix => Matrix4x4.CreateTranslation(_brush.BoundingBox.Center);

            public bool IsActive => throw new NotImplementedException();
        }
    }
}
