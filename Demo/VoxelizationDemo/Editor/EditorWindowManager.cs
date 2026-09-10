using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Utility;
using VoxelizationDemo.Editor.Popups;
using VoxelizationDemo.Editor.Windows;

namespace VoxelizationDemo.Editor
{
    internal sealed class EditorWindowManager : IDisposable
    {
        private readonly List<IEditorWindow> _windows;
        private readonly List<EditorPopup> _popups;

        private bool _disposedValue;

        internal EditorWindowManager()
        {
            _windows = new List<IEditorWindow>();
            _popups = new List<EditorPopup>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (EditorPopup popup in _popups)
                    {
                        if (popup is IDisposable disposable)
                            disposable.Dispose();
                    }

                    foreach (IEditorWindow window in _windows)
                    {
                        if (window is IDisposable disposable)
                            disposable.Dispose();
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void RenderActiveWindows()
        {
            if (_windows.Count > 0)
            {
                using RentedArray<IEditorWindow> activeWindows = new RentedArray<IEditorWindow>(_windows.Count);
                _windows.CopyTo(activeWindows.Span);

                foreach (IEditorWindow window in activeWindows)
                {
                    if (!window.UpdateAndRender())
                    {
                        _windows.Remove(window);

                        if (window is IDisposable disposable)
                            disposable.Dispose();
                    }
                }
            }

            if (_popups.Count > 0)
            {
                using RentedArray<EditorPopup> activePopups = new RentedArray<EditorPopup>(_popups.Count);
                _popups.CopyTo(activePopups.Span);

                foreach (EditorPopup popup in activePopups)
                {
                    if (!popup.UpdateAndRender())
                    {
                        if (!popup.Task.IsCompleted)
                            popup.Cancel();

                        _popups.Remove(popup);

                        if (popup is IDisposable disposable)
                            disposable.Dispose();
                    }
                }
            }
        }

        public void AddOpenWindow(IEditorWindow window)
        {
            _windows.AddUnique(window);
        }

        public void CloseWindow(IEditorWindow window)
        {
            if (_windows.Remove(window))
            {
                if (window is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        public ValueTask<object?> AddOpenPopup(EditorPopup popup)
        {
            _popups.AddUnique(popup);
            return popup.Task;
        }

        public void ClosePopup(EditorPopup popup)
        {
            if (_popups.Remove(popup))
            {
                if (_popups is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        public ROList<IEditorWindow> Windows => _windows;
        public ROList<EditorPopup> Popups => _popups;
    }
}
