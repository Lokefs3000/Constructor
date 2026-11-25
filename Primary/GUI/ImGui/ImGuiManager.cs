using Primary.Profiling;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.GUI.ImGui
{
    public sealed class ImGuiManager : IDisposable
    {
        private ImGuiContext _context;

        private List<IImGuiDrawer> _drawers;
        private List<Action> _callbacks;

        private bool _isEnabled;

        private bool _disposedValue;

        internal ImGuiManager()
        {
            _context = IMGUI.CreateContext();

            _drawers = new List<IImGuiDrawer>();
            _callbacks = new List<Action>();

            _isEnabled = false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    IMGUI.DestroyContext();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void AddDrawer(IImGuiDrawer drawer) => _drawers.AddUnique(drawer);
        public void RemoveDrawer(IImGuiDrawer drawer) => _drawers.Remove(drawer);

        public void AddCallback(Action callback) => _callbacks.AddUnique(callback);
        public void RemoveCallback(Action callback) => _callbacks.Remove(callback);

        public void UpdateAndRender()
        {
            if (_isEnabled && (_drawers.Count > 0 || _callbacks.Count > 0))
            {
                using (new ProfilingScope("IMGUI"))
                {
                    _context.ClearAllDrawLists();

                    IMGUI.BeginState();

                    foreach (IImGuiDrawer drawer in _drawers)
                    {
                        drawer.Draw();
                    }

                    foreach (Action callback in _callbacks)
                    {
                        callback();
                    }

                    IMGUI.EndState();
                }
            }
        }

        public ImGuiContext Context => _context;

        public bool IsEnabled { get => _isEnabled; set => _isEnabled = value; }
    }

    public interface IImGuiDrawer
    {
        public void Draw();
    }
}
