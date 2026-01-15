using Hexa.NET.ImGui;

namespace Editor.DearImGui
{
    internal sealed class DearImGuiWindowManager
    {
        private List<OpenWindowData> _windows;
        private int _idCounter;

        internal DearImGuiWindowManager()
        {
            _windows = new List<OpenWindowData>();
        }

        internal void RenderOpenWindows()
        {
            foreach (OpenWindowData windowData in _windows)
            {
                ImGui.PushID((int)windowData.CustomId);
                windowData.Window.Render();
                ImGui.PopID();
            }
        }

        internal T Open<T>() where T : class, IDearImGuiWindow, new()
        {
            T val = new T();
            _windows.Add(new OpenWindowData(val, GetId()));

            return val;
        }

        private uint GetId()
        {
            while (true)
            {
                uint id = (uint)_idCounter++;
                if (!_windows.Exists((x) => x.CustomId == id))
                    return id;
            }
        }

        private readonly record struct OpenWindowData(IDearImGuiWindow Window, uint CustomId);
    }

    internal interface IDearImGuiWindow
    {
        public abstract void Render();
    }
}
