using CommunityToolkit.HighPerformance;
using Editor.Gui.Elements;
using Editor.Gui.Graphics;
using Editor.Gui.Resources;
using Primary;
using Primary.Common;
using Primary.Profiling;
using Primary.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Gui
{
    public sealed class EditorGuiManager : IDisposable
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private List<DockingContainer> _activeContainers;

        private GuiFont _latoFont;

        private UIEventManager _eventManager;
        private UIDragManager _dragManager;

        private bool _disposedValue;

        public EditorGuiManager()
        {
            _activeContainers = new List<DockingContainer>();

            _latoFont = new GuiFont("Content/Lato_Font.bundle");

            _eventManager = new UIEventManager();
            _dragManager = new UIDragManager();

            s_instance.Target = this;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _eventManager.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Update()
        {
            _eventManager.Update();
            _dragManager.Update();

            RecalculateLayouts();
        }

        private Queue<Element> _recalculateQueue = new Queue<Element>();
        private void RecalculateLayouts()
        {
            using (new ProfilingScope("Layout"))
            {
                _recalculateQueue.Clear();

                for (int i = 0; i < _activeContainers.Count; i++)
                {
                    DockingContainer container = _activeContainers[i];
                    if (container.FocusedEditorWindow?.RootElement?.IsLayoutInvalid ?? false)
                    {
                        _recalculateQueue.Clear();
                        _recalculateQueue.Enqueue(container.FocusedEditorWindow.RootElement);
                        
                        while (_recalculateQueue.TryDequeue(out Element? element))
                        {
                            if (element.RecalculateLayoutInternal())
                            {
                                for (int j = 0; j < element.Children.Count; j++)
                                {
                                    if (element.Children[j].IsLayoutInvalid)
                                    {
                                        _recalculateQueue.Enqueue(element.Children[j]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public DockingContainer CreateStaticContainer(Window window)
        {
            DockingContainer container = new DockingContainer(window);
            _activeContainers.Add(container);

            return container;
        }

        public EditorWindow OpenWindow<T>(DockingContainer? dockParent = null, DockSpace dockSpace = DockSpace.Tab) where T : EditorWindow, new()
        {
            EditorWindow window = new T();

            if (dockParent != null)
            {
                if (dockSpace > DockSpace.Tab)
                {
                    DockingContainer container = new DockingContainer();
                    container.Size = new Vector2(300.0f);

                    _activeContainers.Add(container);

                    dockParent.DockContainer(container, dockSpace);
                    dockParent.RecomputeDockingSpace();

                    dockParent = container;
                }
            }

            dockParent!.DockWindowAsTab(window);
            dockParent.RecomputeDockingSpace();

            return window;
        }

        internal Span<DockingContainer> ActiveContainers => _activeContainers.AsSpan();

        public GuiFont DefaultFont => _latoFont;

        public static EditorGuiManager Instance => NullableUtility.ThrowIfNull((EditorGuiManager?)s_instance.Target);
    }
}
