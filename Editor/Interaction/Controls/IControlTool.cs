using Editor.Interaction.Tools;

namespace Editor.Interaction.Controls
{
    internal interface IControlTool
    {
        public ReadOnlySpan<IToolTransform> Transforms { get; }

        public void Activated();
        public void Deactivated();

        public event Action<IToolTransform>? NewTransformSelected;
        public event Action<IToolTransform>? OldTransformDeselected;
    }
}
