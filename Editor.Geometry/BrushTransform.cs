using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Geometry
{
    public sealed class BrushTransform
    {
        private Quaternion _rotation;
        private Vector3 _origin;

        private Matrix4x4 _model;

        public BrushTransform()
        {
            _rotation = Quaternion.Identity;
            _origin = Vector3.Zero;

            _model = Matrix4x4.Identity;
        }

        public BrushTransform(Quaternion rotation, Vector3 origin)
        {
            _rotation = rotation;
            _origin = origin;

            _model = Matrix4x4.Identity;
        }

        public ref Quaternion Rotation => ref _rotation;
        public ref Vector3 Origin => ref _origin;

        public Matrix4x4 Model => _model;
    }
}
