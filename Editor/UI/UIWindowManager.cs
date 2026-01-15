using Primary.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Editor.UI
{
    public sealed class UIWindowManager
    {
        private Dictionary<int, UIWindow> _activeWindows;

        internal UIWindowManager()
        {
            _activeWindows = new Dictionary<int, UIWindow>();
        }

        private int GetNewWindowId()
        {
            int id = (int)Stopwatch.GetTimestamp();
            while (_activeWindows.ContainsKey(id) && id != int.MaxValue)
            {
                id = (int)Stopwatch.GetTimestamp();
            }

            return id;
        }

        internal T OpenWindow<T>() where T : UIWindow
        {
            T window = (T)Activator.CreateInstance(typeof(T), [GetNewWindowId()])!;

            _activeWindows.Add(window.UniqueWindowId, window);
            return window;
        }
    }
}
