using Primary.Common;
using Primary.Editor;
using System.Numerics;
using System.Runtime.Serialization;

namespace Primary.Components
{
    [ComponentRequirements(typeof(Transform)), ComponentConnections(typeof(CameraProjectionData))]
    public record struct Camera : IComponent
    {
        [IgnoreDataMember]
        private bool _isDirty;

        private CameraClear _clear;
        private Color _clearColor;

        private float _fov;
        private float _nearClip;
        private float _farClip;

        public Camera()
        {
            _isDirty = true;

            _clear = CameraClear.Solid;
            _clearColor = Color.Black;

            _fov = float.DegreesToRadians(70.0f);
            _nearClip = 0.02f;
            _farClip = 1000.0f;
        }

        public CameraClear Clear { get => _clear; set => _clear = value; }
        public Color ClearColor { get => _clearColor; set => _clearColor = value; }

        public float FieldOfView { get => _fov; set { _fov = value; _isDirty = true; } }
        public float NearClip { get => _nearClip; set { _nearClip = value; _isDirty = true; } }
        public float FarClip { get => _farClip; set { _farClip = value; _isDirty = true; } }

        [IgnoreDataMember]
        internal bool IsDirty { get => _isDirty; set => _isDirty = value; }
    }

    [ComponentUsage(CanBeAdded: false), DontSerializeComponent]
    [InspectorHidden]
    public record struct CameraProjectionData : IComponent
    {
        public Vector2 ClientSize;
        public Matrix4x4 ProjectionMatrix;
        public Matrix4x4 ViewMatrix;
    }

    public enum CameraClear : byte
    {
        None = 0,
        Solid
    }
}
