using CommunityToolkit.HighPerformance;
using Editor.GeoEdit;
using Editor.Geometry;
using Editor.Geometry.Shapes;
using Editor.Interaction.Tools;
using System.Numerics;

namespace Editor.Interaction.Controls
{
    internal class GeoEditControlTool : IControlTool
    {
        private List<IToolTransform> _transformList;

        internal GeoEditControlTool(ToolManager tools)
        {
            _transformList = new List<IToolTransform>();
        }

        public void Activated()
        {
            _transformList.Clear();

            SelectionManager.NewSelected += Event_NewSelected;
            SelectionManager.OldDeselected += Event_OldDeselected;
        }

        public void Deactivated()
        {
            _transformList.Clear();

            SelectionManager.NewSelected -= Event_NewSelected;
            SelectionManager.OldDeselected -= Event_OldDeselected;
        }

        private void Event_NewSelected(SelectedBase @base)
        {
            if (@base is SelectedGeoBrush selected1)
            {
                AddIfNewInstance(new GeoBrushToolTransform(selected1.Brush));
            }
            else if (@base is SelectedGeoBoxShape selected2)
            {
                AddIfNewInstance(new GeoBoxShapeToolTransform(selected2.Brush, selected2.Shape, selected2.FaceIndex));
            }

            void AddIfNewInstance<T>(T value) where T : struct, IToolTransform, IEquatable<T>
            {
                if (!_transformList.Exists((x) => x is T transform && transform.Equals(value)))
                {
                    _transformList.Add(value);
                    NewTransformSelected?.Invoke(_transformList.Last());
                }
            }
        }

        private void Event_OldDeselected(SelectedBase @base)
        {
            if (@base is SelectedGeoBrush selected1)
            {
                RemoveOldInstance(new GeoBrushToolTransform(selected1.Brush));
            }
            else if (@base is SelectedGeoBoxShape selected2)
            {
                RemoveOldInstance(new GeoBoxShapeToolTransform(selected2.Brush, selected2.Shape, selected2.FaceIndex));
            }

            void RemoveOldInstance<T>(T value) where T : struct, IToolTransform, IEquatable<T>
            {
                int idx = _transformList.FindIndex((x) => x is T transform && transform.Equals(value));
                if (idx != -1)
                {
                    OldTransformDeselected?.Invoke(_transformList[idx]);
                    _transformList.RemoveAt(idx);
                }
            }
        }

        public ReadOnlySpan<IToolTransform> Transforms => _transformList.AsSpan();

        public event Action<IToolTransform>? NewTransformSelected;
        public event Action<IToolTransform>? OldTransformDeselected;
    }

    internal struct GeoBrushToolTransform : IToolTransform, IEquatable<GeoBrushToolTransform>
    {
        private readonly GeoBrush _brush;

        internal GeoBrushToolTransform(GeoBrush brush)
        {
            _brush = brush;
        }

        public void SetWorldTransform(Vector3 position, Vector3 delta)
        {
            _brush.Transform.Position = position;
        }

        public void CommitTransform()
        {

        }

        public bool Equals(GeoBrushToolTransform other)
        {
            return _brush.Equals(other._brush);
        }

        internal GeoBrush Brush => _brush;

        public Vector3 Position { get => _brush.Transform.Position; set => _brush.Transform.Position = value; }
        public Quaternion Rotation { get => _brush.Transform.Rotation; set => _brush.Transform.Rotation = value; }
        public Vector3 Scale { get => Vector3.One; set { } }

        public Matrix4x4 WorldMatrix => Matrix4x4.CreateTranslation(_brush.Transform.Position);
    }
    internal struct GeoBoxShapeToolTransform : IToolTransform, IEquatable<GeoBoxShapeToolTransform>
    {
        private readonly GeoBrush _brush;
        private readonly GeoBoxShape _shape;
        private readonly GeoBoxShapeFace _faceIndex;

        private Vector3 _position;
        private Vector3 _extents;

        internal GeoBoxShapeToolTransform(GeoBrush brush, GeoBoxShape shape, int faceIndex)
        {
            _brush = brush;
            _shape = shape;
            _faceIndex = (GeoBoxShapeFace)faceIndex;

            _position = brush.Transform.Position;
            _extents = shape.Extents;
        }

        public void SetWorldTransform(Vector3 position, Vector3 delta)
        {
            bool hasModifiedExtents = false;

            _brush.Transform.Position = _position;
            _shape.Extents = _extents;

            switch (_faceIndex)
            {
                case GeoBoxShapeFace.Left:
                    {
                        _brush.Transform.Position = new Vector3(_position.X + delta.X, _position.Y, _position.Z);
                        break;
                    }
                case GeoBoxShapeFace.Right:
                    {
                        _shape.Extents = new Vector3(_extents.X + delta.X, _extents.Y, _extents.Z);
                        hasModifiedExtents = true;
                        break;
                    }
                case GeoBoxShapeFace.Front:
                    {
                        _shape.Extents = new Vector3(_extents.X, _extents.Y, _extents.Z + delta.Z);
                        hasModifiedExtents = true;
                        break;
                    }
                case GeoBoxShapeFace.Back:
                    {
                        _brush.Transform.Position = new Vector3(_position.X, _position.Y, _position.Z + delta.Z);
                        break;
                    }
                case GeoBoxShapeFace.Top:
                    {
                        _shape.Extents = new Vector3(_extents.X, _extents.Y + delta.Y, _extents.Z);
                        hasModifiedExtents = true;
                        break;
                    }
                case GeoBoxShapeFace.Bottom:
                    {
                        _brush.Transform.Position = new Vector3(_position.X, _position.Y + delta.Y, _position.Z);
                        break;
                    }
            }

            if (ToolManager.IsSnappingActive)
            {
                float snapScale = ToolManager.SnapScale;

                _shape.Extents = Vector3.Round(_shape.Extents / snapScale) * snapScale;
                _brush.Transform.Position = Vector3.Round(_brush.Transform.Position / snapScale) * snapScale;
            }

            if (hasModifiedExtents)
            {
                Vector3 min = Vector3.Min(_brush.Transform.Position, _shape.Extents);
                Vector3 max = Vector3.Max(_brush.Transform.Position, _shape.Extents);

                _brush.Transform.Position = min;
                _shape.Extents = max;
            }
        }

        public void CommitTransform()
        {
            _position = _brush.Transform.Position;
            _extents = _shape.Extents;
        }

        public bool Equals(GeoBoxShapeToolTransform other)
        {
            return _brush == other.Brush && _shape == other._shape && _faceIndex == other._faceIndex;
        }

        public override string ToString() => $"GeoBoxShape {{ {_shape.GetType().Name}[{_faceIndex}] }}";

        internal GeoBrush Brush => _brush;
        internal GeoBoxShape Shape => _shape;
        internal GeoBoxShapeFace FaceIndex => _faceIndex;

        public Vector3 Position { get => WorldMatrix.Translation; }
        public Quaternion Rotation { get => Quaternion.Identity; }
        public Vector3 Scale { get => Vector3.One; }

        public Matrix4x4 WorldMatrix
        {
            get
            {
                Vector3 halfExtents = _extents * 0.5f;
                switch (_faceIndex)
                {
                    case GeoBoxShapeFace.Left: return Matrix4x4.CreateTranslation(_brush.Transform.Position + new Vector3(0.0f, halfExtents.Y, halfExtents.Z));
                    case GeoBoxShapeFace.Right: return Matrix4x4.CreateTranslation(_brush.Transform.Position + new Vector3(_shape.Extents.X, halfExtents.Y, halfExtents.Z));
                    case GeoBoxShapeFace.Front: return Matrix4x4.CreateTranslation(_brush.Transform.Position + new Vector3(halfExtents.X, halfExtents.Y, _shape.Extents.Z));
                    case GeoBoxShapeFace.Back: return Matrix4x4.CreateTranslation(_brush.Transform.Position + new Vector3(halfExtents.X, halfExtents.Y, 0.0f));
                    case GeoBoxShapeFace.Top: return Matrix4x4.CreateTranslation(_brush.Transform.Position + new Vector3(halfExtents.X, _shape.Extents.Y, halfExtents.Z));
                    case GeoBoxShapeFace.Bottom: return Matrix4x4.CreateTranslation(_brush.Transform.Position + new Vector3(halfExtents.X, 0.0f, halfExtents.Z));
                    default: return Matrix4x4.Identity;
                }
            }
        }
    }
}
