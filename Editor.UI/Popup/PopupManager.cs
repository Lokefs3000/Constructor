using Editor.UI.Serialization;
using Primary;
using Primary.Common;
using Primary.Mathematics;
using Primary.Polling;
using Primary.Windowing;
using SDL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Popup
{
    public sealed class PopupManager
    {
        private List<Window> _windows;
        private Stack<PopupContext> _contexts;

        internal PopupManager()
        {
            _windows = new List<Window>();
            _contexts = new Stack<PopupContext>();
        }

        private Window GetNewWindow()
        {
            if (_windows.Count <= _contexts.Count)
            {
                Window window = WindowManager.Instance.CreateWindow("POPUP", Int2.One, CreateWindowFlags.Borderless);
                //_windows.Add(window);

                return window;
            }
            else
            {
                Window window = _windows[^1];
                window.Show();

                return window;
            }
        }

        private void ReturnWindow(Window window)
        {
            if (false && _windows.Count < _contexts.Count)
            {
                window.Hide();
                _windows.Add(window);
            }
            else
            {
                WindowManager.Instance.DestroyWindow(window);
            }
        }

        private void ClosePopupsUntilOwner(IWindow owner)
        {
            while (_contexts.TryPeek(out PopupContext context) && context.Host != owner)
            {
                CleanupPopupData(context.Host);
                _contexts.Pop();
            }
        }

        private void CleanupPopupData(PopupHost host)
        {
            UIManager.Instance.RemovePopupAsHost(host);

            ReturnWindow(host.HostWindow!);
            host.Dispose();
        }

        internal T OpenPopup<T>(LayoutSnippet snippet, IWindow owner, Int2 origin) where T : SnippetPopupHost
        {
            Window? parentWindow = owner.ParentHost?.HostWindow;
            if (parentWindow == null)
                throw new NotImplementedException("Add fallback to null host window!");

            T popupHost = (T)Activator.CreateInstance(typeof(T), [parentWindow, GetNewWindow(), origin])!;
            
            popupHost.Snippet = snippet;
            popupHost.Focus();

            if (_contexts.TryPeek(out PopupContext context) && context.Host != owner)
                ClosePopupsUntilOwner(owner);

            _contexts.Push(new PopupContext(popupHost, owner));

            UIManager.Instance.AddPopupAsHost(popupHost);
            return popupHost;
        }

        internal void FocusPopup(PopupHost host)
        {
            while (_contexts.TryPeek(out PopupContext context) && context.Host != host)
            {
                CleanupPopupData(context.Host);
                _contexts.Pop();
            }
        }

        internal void BlurPopup(PopupHost host)
        {
            if (!_contexts.TryPeek(out PopupContext context) || context.Host != host)
                return;

            EventManager eventManager = Engine.GlobalSingleton.EventManager;

            CleanupPopupData(host);
            _contexts.Pop();

            while (_contexts.TryPeek(out context))
            {

                eventManager.PumpEvents();

                unsafe
                {
                    if (Flags.HasFlag(SDL3.SDL_GetWindowFlags((SDL_Window*)context.Host.HostWindow!.InternalWindowInterop), SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS))
                        break;
                }

                CleanupPopupData(context.Host);
                _contexts.Pop();
            }
        }

        internal void ClosePopup(PopupHost host)
        {
            while (_contexts.TryPeek(out PopupContext context))
            {
                CleanupPopupData(context.Host);
                _contexts.Pop();

                if (context.Host == host)
                {
                    if (_contexts.TryPeek(out context))
                        context.Host.Focus();
                    break;
                }
            }
        }

        internal Stack<PopupContext> Popups => _contexts;

        internal readonly record struct PopupContext(PopupHost Host, IWindow Owner);
    }
}
