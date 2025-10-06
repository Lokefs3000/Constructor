using Editor.LegacyGui.Elements;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Editor.LegacyGui
{
    public class EditorWindow : IDisposable
    {
        protected readonly UIWindow _window;

        private bool _disposedValue;

        public EditorWindow(Vector2 position, Vector2 size)
        {
            _window = new UIWindow(position, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void DisposeSelfManaged() { }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void DisposeSelfUnamanaged() { }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    DisposeSelfManaged();
                }

                DisposeSelfUnamanaged();

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal UIWindow Window => _window;
    }
}
