using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EditorUI.Dock;
using EditorUI.Layout;
using EditorUI.Reflection;
using EditorUI.Scheduling;
using EditorUI.Serialization;
using EditorUI.Visual;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Common;

namespace EditorUI
{
    public sealed class UIManager : IDisposable
    {
        // initial universal objects
        private ActionScheduler _actionScheduler;
        private ValueSerializer _valueSerializer;

        // core functionality objects
        private WindowManager _windowManager;
        private DockManager _dockManager;
        private ReflectionManager _reflectionManager;
        private WidgetManager _widgetManager;

        // data operators
        private LayoutManager _layoutManager;
        private VisualManager _visualManager;

        private bool _disposedValue;

        public UIManager()
        {
            s_instance.Target = this;

            UILog.CreateDefault();

            _actionScheduler = new ActionScheduler();
            _valueSerializer = new ValueSerializer();

            _windowManager = new WindowManager(this);
            _dockManager = new DockManager(this);
            _reflectionManager = new ReflectionManager();
            _widgetManager = new WidgetManager();

            _layoutManager = new LayoutManager();
            _visualManager = new VisualManager(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void UpdateInternalData()
        {
            _widgetManager.DestroyAllInQueue();
            _actionScheduler.FlushScheduledActions();

            CheckDockHostsUpdateStates();

            _layoutManager.RecalculateAll();
            _visualManager.RenderAll();
        }

        private void CheckDockHostsUpdateStates()
        {
            foreach (DockHost host in _dockManager.DockHosts)
            {
                // this or somthing under it is invalid
                if (host.RootStateFlags != StateFlags.None)
                {
                    if (host.RootStateFlags.HasFlags(StateFlags.ThisLayout))
                    {
                        _layoutManager.RecalculateNext(host);
                    }

                    foreach (DockBase dock in host.Docked)
                    {
                        WindowBase? currentWindow = dock.CurrentWindow;
                        if (currentWindow != null && currentWindow.StateFlags.HasFlags(StateFlags.InvalidLayout))
                        {
                            _layoutManager.RecalculateNext(currentWindow.RootWidget);
                        }
                    }
                }
            }
        }

        public ActionScheduler ActionScheduler => _actionScheduler;
        public ValueSerializer ValueSerializer => _valueSerializer;

        public WindowManager WindowManager => _windowManager;
        public DockManager DockManager => _dockManager;
        public ReflectionManager ReflectionManager => _reflectionManager;
        public WidgetManager WidgetManager => _widgetManager;

        public LayoutManager LayoutManager => _layoutManager;
        public VisualManager VisualManager => _visualManager;

        private static WeakReference s_instance = new WeakReference(null);

        public static UIManager Instance => Unsafe.As<UIManager>(s_instance.Target)!;
    }
}
