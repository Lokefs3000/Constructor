using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Scheduling;
using Primary.Collections.ReadOnly;

namespace EditorUI.Windowing
{
    public sealed class WindowManager
    {
        private readonly UIManager _manager;

        private List<WindowBase> _trackedWindows;

        private WindowBase? _currentWindowFocus;

        internal WindowManager(UIManager manager)
        {
            _manager = manager;

            _trackedWindows = new List<WindowBase>();

            _currentWindowFocus = null;
        }

        public T? OpenWindowBase<T>(params object?[]? args) where T : WindowBase
        {
            T window;
            try
            {
                window = (T)Activator.CreateInstance(typeof(T), args)!;
            }
            catch (Exception ex)
            {
                UILog.Logger?.Error(ex, "An exception occured while trying to create an window base of type {t}", typeof(T));
                return null;
            }
           
            _trackedWindows.Add(window);
            return window;
        }

        public T? OpenWidgetWindow<T>(params object?[]? args) where T : WidgetWindow
        {
            T window;
            try
            {
                window = (T)Activator.CreateInstance(typeof(T), [this, _manager.ValueSerializer, .. args ?? []])!;
            }
            catch (Exception ex)
            {
                UILog.Logger?.Error(ex, "An exception occured while trying to create an widget window of type {t}", typeof(T));
                return null;
            }

            _trackedWindows.Add(window);
            return window;
        }

        public bool TrySetWindowFocus(WindowBase? windowToFocus)
        {
            if (_currentWindowFocus == windowToFocus)
                return true;

            if (_currentWindowFocus != null)
            {
                _currentWindowFocus.OnFocusLost();
            }

            if (windowToFocus != null)
            {
                windowToFocus.OnFocusGained();
            }

            _currentWindowFocus = windowToFocus;
            return true;
        }

        internal void CloseWindow(WindowBase window)
        {
            if (_trackedWindows.Contains(window))
            {
                _manager.ActionScheduler.TryScheduleUnique(window, (_, _) =>
                {
                    window.DestroySelf();
                    _trackedWindows.Remove(window);
                }, KnownPriorities.CloseWindow, null);
            }
        }

        public ROList<WindowBase> Windows => _trackedWindows;
        public WindowBase? CurrentWindowFocus => _currentWindowFocus;
    }
}
