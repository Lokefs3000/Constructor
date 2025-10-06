using System.Numerics;

namespace Editor.LegacyGui.Windows
{
    public sealed class HierchyWindow : EditorWindow
    {
        public HierchyWindow() : base(new Vector2(200.0f), new Vector2(200.0f, 250.0f))
        {
            Window.Title = "Hierchy";
        }
    }
}
