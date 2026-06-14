using Editor.Geo.History;
using Editor.Geometry;
using Editor.History;
using Editor.Interaction;
using Editor.Interaction.Tools;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Geo.Tools
{
    internal sealed class BrushToolTransform : IToolTransform
    {
        private Brush _brush;
        private Vector3[] _vertices;

        private byte _selectedVertices;

        private Vector3 _center;
        private Vector3 _deltaCenter;

        internal BrushToolTransform(Brush brush, byte selected)
        {
            _brush = brush;
            _vertices = brush.Vertices.ToArray();

            _selectedVertices = selected;

            _center = CalculateCenter(brush.Vertices);
            _deltaCenter = _center;
        }

        internal void UpdateVertexSelection(byte selectedVertices)
        {
            _selectedVertices = selectedVertices;

            _center = CalculateCenter(_brush.Vertices);
            _deltaCenter = _center;
        }

        internal void UpdateVertices()
        {
            if (!ToolManager.IsCurrentToolActive)
            {
                _brush.Vertices.CopyTo(_vertices);

                _center = CalculateCenter(_brush.Vertices);
                _deltaCenter = _center;
            }
        }

        public void SetWorldTransform(Vector3 position, Vector3 delta)
        {
            for (int i = 0; i < _vertices.Length; i++)
            {
                if (!Flags.HasFlag(_selectedVertices, 1 << i))
                    continue;

                _brush.Vertices[i] = _vertices[i] + delta;
            }

            _brush.NotifyUpdate((BrushUpdateFlags)_selectedVertices);

            _deltaCenter = _center + delta;
        }

        public void CommitTransform()
        {
            HistoryManager.AddStep(new BrushTranslatedStep(_brush, _selectedVertices, _vertices));

            _brush.Vertices.CopyTo(_vertices);

            _center = CalculateCenter(_brush.Vertices);
            _deltaCenter = _center;
        }

        private Vector3 CalculateCenter(Span<Vector3> vertices)
        {
            if (_selectedVertices == 0)
                return Vector3.Zero;

            Vector3 min = Vector3.PositiveInfinity;
            Vector3 max = Vector3.NegativeInfinity;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (!Flags.HasFlag(_selectedVertices, 1 << i))
                    continue;

                Vector3 v = vertices[i];

                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

            return Vector3.Lerp(min, max, 0.5f);
        }

        public Vector3 Position => _deltaCenter;
        public Quaternion Rotation => _brush.Transform.Rotation;
        public Vector3 Scale => Vector3.One;

        public Matrix4x4 WorldMatrix => Matrix4x4.CreateTranslation(_deltaCenter);

        public bool IsActive => _selectedVertices > 0;
    }
}
