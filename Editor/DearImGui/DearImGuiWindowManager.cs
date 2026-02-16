using Hexa.NET.ImGui;
using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;

namespace Editor.DearImGui
{
    public sealed class DearImGuiWindowManager
    {
        private List<OpenWindowData> _windows;
        private List<OpenPopupData> _popups;
        private int _idCounter;

        private HashSet<IDearImGuiPopup> _unopenedPopups;

        internal DearImGuiWindowManager()
        {
            _windows = new List<OpenWindowData>();
            _popups = new List<OpenPopupData>();
            _idCounter = 0;

            _unopenedPopups = new HashSet<IDearImGuiPopup>();
        }

        internal void RenderOpenWindows()
        {
            foreach (OpenWindowData windowData in _windows)
            {
                ImGui.PushID((int)windowData.CustomId);
                windowData.Window.Render();
                ImGui.PopID();
            }

            for (int i = 0; i < _popups.Count; ++i)
            {
                OpenPopupData popupData = _popups[i];

                ImGui.PushID((int)popupData.CustomId);

                if (_unopenedPopups.Contains(popupData.Popup))
                {
                    popupData.Popup.OpenPopup();
                    _unopenedPopups.Remove(popupData.Popup);
                }

                bool isOpen = true;
                popupData.Popup.Render(ref isOpen);

                if (!isOpen)
                {
                    popupData.Action?.Invoke(popupData.Popup);
                    _popups.RemoveAt(i--);
                }

                ImGui.PopID();
            }
        }

        internal T Open<T>() where T : class, IDearImGuiWindow, new()
        {
            T val = new T();
            _windows.Add(new OpenWindowData(val, GetId()));

            return val;
        }

        internal T OpenPopup<T>(Action<T>? callback = null) where T : class, IDearImGuiPopup, new()
        {
            T val = new T();
            _popups.Add(new OpenPopupData(val, GetId(), callback == null ? null : ((x) => callback(Unsafe.As<T>(x)))));

            _unopenedPopups.Add(_popups.Last().Popup);
            return val;
        }

        internal T OpenPopup<T>(T val, Action<T>? callback = null) where T : class, IDearImGuiPopup
        {
            _popups.Add(new OpenPopupData(val, GetId(), callback == null ? null : ((x) => callback(Unsafe.As<T>(x)))));

            _unopenedPopups.Add(_popups.Last().Popup);
            return val;
        }

        private uint GetId()
        {
            while (true)
            {
                uint id = (uint)_idCounter++;
                if (!_windows.Exists((x) => x.CustomId == id) && !_popups.Exists((x) => x.CustomId == id))
                    return id;
            }
        }

        private readonly record struct OpenWindowData(IDearImGuiWindow Window, uint CustomId);
        private readonly record struct OpenPopupData(IDearImGuiPopup Popup, uint CustomId, Action<IDearImGuiPopup>? Action);
    }

    internal interface IDearImGuiWindow
    {
        public abstract void Render();
    }

    internal interface IDearImGuiPopup
    {
        public DearImGuiPopupFlags Flags { get; }

        public abstract void Render(ref bool windowOpen);
        public abstract void OpenPopup();
    }

    internal enum DearImGuiPopupFlags : byte
    {
        None = 0,

        Unique = 1 << 0
    }
}
