using Primary.Editor;
using System.Numerics;
using System.Runtime.Serialization;

namespace Primary.Components
{
    [ComponentConnections(typeof(LocalTransform), typeof(WorldTransform))]
    public record struct Transform : IComponent
    {
        private Vector3 _position = Vector3.Zero;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;

        private bool _invalid = true;
        private bool _selfInvalid = true;

        public Transform()
        {
            _position = Vector3.Zero;
            _rotation = Quaternion.Identity;
            _scale = Vector3.One;

            _invalid = true;
            _selfInvalid = true;
        }

        public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            _position = position;
            _rotation = rotation;
            _scale = scale;

            _invalid = true;
            _selfInvalid = true;
        }

        public Vector3 Position { get => _position; set { _position = value; _invalid = true; _selfInvalid = true; } }
        public Quaternion Rotation { get => _rotation; set { _rotation = value; _invalid = true; _selfInvalid = true; } }
        public Vector3 Scale { get => _scale; set { _scale = value; _invalid = true; _selfInvalid = true; } }

        [IgnoreDataMember]
        internal bool Invalid { get => _invalid; set => _invalid = value; }
        [IgnoreDataMember]
        internal bool SelfInvalid { get => _selfInvalid; set => _selfInvalid = value; }
    }

    [ComponentUsage(CanBeAdded: false), DontSerializeComponent]
    [InspectorHidden]
    public record struct LocalTransform : IComponent
    {
        public Matrix4x4 Transformation;

        public Vector3 ForwardVector => new Vector3(Transformation.M31, Transformation.M32, Transformation.M33);
        public Vector3 UpVector => new Vector3(Transformation.M21, Transformation.M22, Transformation.M23);
        public Vector3 RightVector => new Vector3(Transformation.M11, Transformation.M12, Transformation.M13);
    }

    [ComponentUsage(CanBeAdded: false), DontSerializeComponent]
    [InspectorHidden]
    public record struct WorldTransform : IComponent
    {
        public Matrix4x4 Transformation;

        public Vector3 ForwardVector => new Vector3(Transformation.M31, Transformation.M32, Transformation.M33);
        public Vector3 UpVector => new Vector3(Transformation.M21, Transformation.M22, Transformation.M23);
        public Vector3 RightVector => new Vector3(Transformation.M11, Transformation.M12, Transformation.M13);
    }
}
