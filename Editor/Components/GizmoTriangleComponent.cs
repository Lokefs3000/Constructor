using Primary.Common;
using Primary.Components;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Components
{
    [Component]
    public record struct GizmoTriangleComponent : IComponent
    {
        private Color _color;
        private Vector3 _a;
        private Vector3 _b;
        private Vector3 _c;

        public GizmoTriangleComponent()
        {
            _color = Color.White;
            _a = Vector3.Zero;
            _b = Vector3.Zero;
            _c = Vector3.Zero;
        }

        public Color Color { get => _color; set => _color = value; }
        public Vector3 PointA { get => _a; set => _a = value; }
        public Vector3 PointB { get => _b; set => _b = value; }
        public Vector3 PointC { get => _c; set => _c = value; }
    }
}
