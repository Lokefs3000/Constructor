using System.Numerics;
using System.Runtime.InteropServices;

namespace Primary.GUI.ImGui
{
    internal class ImGuiContext : IDisposable
    {
        private bool _disposedValue;

        internal ImGuiContext()
        {

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                _disposedValue = true;
            }
        }

        ~ImGuiContext()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ImGuiVertex
    {
        public Vector2 Position;
        public Vector2 UV;
        public uint Color;
    }
}
