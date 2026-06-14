using Editor.Interaction.Controls;
using Editor.Rendering.Tools;
using Primary.Collections.ReadOnly;
using System.Numerics;

namespace Editor.Interaction.Tools
{
    public interface ITool
    {
        public bool IsInteracting { get; }
        public bool IsActive { get; }

        public void Selected(ToolManager tools);
        public void Deselected(ToolManager tools);

        public void Update(ToolManager tools);
        public bool Render(ToolManager tools, ToolDrawData drawData);
    }

    public interface IToolTransform
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }

        public Matrix4x4 WorldMatrix { get; }

        public bool IsActive { get; }

        public void SetWorldTransform(Vector3 position, Vector3 delta);
        public void CommitTransform();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ToolControlTypeAttribute(Type controlType) : Attribute
    {
        public Type ControlType { get; init; } = controlType;
    }
}
