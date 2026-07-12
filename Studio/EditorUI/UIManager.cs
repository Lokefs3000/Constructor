using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EditorUI.Dock;
using EditorUI.Input;
using EditorUI.Layout;
using EditorUI.Popup;
using EditorUI.Reflection;
using EditorUI.Scheduling;
using EditorUI.Serialization;
using EditorUI.Styling;
using EditorUI.Text;
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
        private PopupManager _popupManager;

        // data operators
        private LayoutManager _layoutManager;
        private VisualManager _visualManager;
        private TextManager _textManager;
        private FontRenderer _fontRenderer;
        private StyleManager _styleManager;
        private InputManager _inputManager;

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
            _popupManager = new PopupManager();

            _layoutManager = new LayoutManager();
            _visualManager = new VisualManager(this);
            _textManager = new TextManager();
            _fontRenderer = new FontRenderer();
            _styleManager = new StyleManager(this);
            _inputManager = new InputManager();
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

            _popupManager.Update();
            _styleManager.UpdateStylesForPending();
            _layoutManager.RecalculateAll();
            _visualManager.RenderAll();
            _textManager.ClearPreviousTextData();
            _fontRenderer.RenderGlyphs();
        }

        private void CheckDockHostsUpdateStates()
        {
            foreach (DockHost host in _dockManager.DockHosts)
            {
                host.UpdateData();

                // this or somthing under it is invalid
                if (host.RootStateFlags != StateFlags.None)
                {
                    if (host.RootStateFlags.HasFlags(StateFlags.ThisLayout))
                    {
                        _layoutManager.RecalculateNext(host);
                    }
                    else if (host.RootStateFlags.HasFlags(StateFlags.InvalidStyle) || host.StylesheetProvider.HasChangedStylesheets)
                    {
                        _styleManager.UpdateSingleStyling(host);
                    }

                    foreach (DockBase dock in host.Docked)
                    {
                        WindowBase? currentWindow = dock.CurrentWindow;
                        if (currentWindow != null)
                        {
                            if (currentWindow.StateFlags.HasFlag(StateFlags.InvalidLayout))
                                _layoutManager.RecalculateNext(currentWindow.RootWidget);
                            if ((currentWindow.StylesheetProvider.HasChangedStylesheets || currentWindow.StateFlags.HasFlag(StateFlags.InvalidStyle)) && currentWindow is WidgetWindow ww)
                                _styleManager.UpdateWindowStyling(ww);
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
        public PopupManager PopupManager => _popupManager;

        public LayoutManager LayoutManager => _layoutManager;
        public VisualManager VisualManager => _visualManager;
        public TextManager TextManager => _textManager;
        public FontRenderer FontRenderer => _fontRenderer;
        public StyleManager StyleManager => _styleManager;
        public InputManager InputManager => _inputManager;

        private static WeakReference s_instance = new WeakReference(null);

        public static UIManager Instance => Unsafe.As<UIManager>(s_instance.Target)!;
    }
}
