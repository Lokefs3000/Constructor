using CommunityToolkit.HighPerformance;
using Editor.LegacyGui.Data;
using Editor.LegacyGui.Elements;
using Editor.LegacyGui.Managers;
using Editor.Rendering.Gui;
using Primary.Common;
using Primary.Polling;
using Primary.Profiling;
using SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.LegacyGui
{
    public sealed class EditorGuiManager
    {
        private static EditorGuiManager? s_instance;

        private GuiEventManager _eventManager;
        private GuiDockingManager _dockingManager;
        private GuiDragManager _dragManager;

        private GuiFont _defaultFont;

        private List<ActiveUIWindowData> _activeWindows;

        private CoreWindow _coreWindow;
        private UIWindow? _currentFocus;

        internal EditorGuiManager(Editor editor)
        {
            s_instance = this;

            _eventManager = new GuiEventManager(this);
            _dockingManager = new GuiDockingManager();
            _dragManager = new GuiDragManager();

            _defaultFont = new GuiFont("Content/Lato_Font.dds", "Content/Lato_Font.json");

            _activeWindows = new List<ActiveUIWindowData>();

            _coreWindow = OpenWindow<CoreWindow>()!;
            _currentFocus = null;
        }

        public T? OpenWindow<T>() where T : EditorWindow, new() => OpenWindow(typeof(T)) as T;
        public EditorWindow? OpenWindow(Type type)
        {
            if (!type.IsSubclassOf(typeof(EditorWindow)))
                return null;

            int find = _activeWindows.FindIndex((x) => x.Window.GetType() == type);
            if (find != -1)
                return _activeWindows[find].Window;

            EditorWindow? newWindow = Activator.CreateInstance(type) as EditorWindow;
            if (newWindow == null)
                return null;

            _activeWindows.Add(new ActiveUIWindowData
            {
                Window = newWindow,
            });

            return newWindow;
        }

        private Queue<Element> _layoutStack = new Queue<Element>();

        private void UpdateWindows()
        {
            using (new ProfilingScope("EdUpdateWindows"))
            {
                for (int i = 0; i < _activeWindows.Count; i++)
                {
                    ActiveUIWindowData windowData = _activeWindows[i];
                    if (windowData.Window.Window.LayoutInvalid)
                    {

                        using (new ProfilingScope(windowData.Window.GetType().Name))
                        {
                            _layoutStack.Clear();
                            _layoutStack.Enqueue(windowData.Window.Window);

                            while (_layoutStack.TryDequeue(out Element? element))
                            {
                                if (element.RecalculateLayout())
                                {
                                    ReadOnlySpan<Element> children = windowData.Window.Window.Children;
                                    for (int j = 0; j < children.Length; j++)
                                    {
                                        if (children[j].LayoutInvalid)
                                            _layoutStack.Enqueue(children[j]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void SwitchWindowFocus(UIWindow? newFocus)
        {
            if (_currentFocus == newFocus)
                return;

            _currentFocus?.ReleaseFocus();
            newFocus?.TakeFocus();

            _currentFocus = newFocus;

            if (newFocus != null)
                _dockingManager.SetDockSpaceNewFocus(newFocus.CurrentDockSpace);

            NewWindowFocused?.Invoke(newFocus);
        }

        internal void UpdateGui()
        {
            using (new ProfilingScope("UpdateGui"))
            {
                UpdateWindows();

                _eventManager.PumpFrame();
            }
        }

        internal ReadOnlySpan<ActiveUIWindowData> ActiveWindows => _activeWindows.AsSpan();

        internal GuiEventManager EventManager => _eventManager;
        internal GuiDockingManager DockingManager => _dockingManager;
        internal GuiDragManager DragManager => _dragManager;

        public GuiFont DefaultFont => _defaultFont;

        public event Action<UIWindow?>? NewWindowFocused;

        public static EditorGuiManager Instance => NullableUtility.ThrowIfNull(s_instance);

        private sealed class CoreWindow : EditorWindow
        {
            public CoreWindow() : base(new Vector2(0.0f), new Vector2(1336.0f, 726.0f))
            {
                
            }
        }
    }

    internal record struct ActiveUIWindowData(EditorWindow Window);
}
