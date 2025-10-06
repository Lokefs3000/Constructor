using System.Numerics;

namespace Editor.Interaction.Tools
{
    internal interface ITool
    {
        public bool IsInteracting { get; }
        public bool IsActive { get; }

        public void Selected();
        public void Deselected();

        public void Reset();

        public void Update();
    }

    internal interface IToolTransform
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }

        public Matrix4x4 WorldMatrix { get; }

        public void SetWorldTransform(Vector3 position, Vector3 delta);
        public void CommitTransform();
    }
}
