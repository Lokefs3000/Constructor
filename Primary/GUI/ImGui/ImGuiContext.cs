using CommunityToolkit.HighPerformance;
using Primary.Pooling;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.GUI.ImGui
{
    public sealed class ImGuiContext : IDisposable
    {
        private ImGuiStateController _stateController;
        private ImGuiStyle _style;

        private ImGuiFont _font;

        private DisposableObjectPool<ImGuiDrawList> _pooledDrawLists;
        private List<(ImGuiDrawList, int)> _drawLists;

        private bool _disposedValue;

        internal ImGuiContext()
        {
            _stateController = new ImGuiStateController(this);
            _style = new ImGuiStyle();

            _font = new ImGuiFont();

            _pooledDrawLists = new DisposableObjectPool<ImGuiDrawList>(new ImGuiDrawList.Policy(_font));
            _drawLists = new List<(ImGuiDrawList, int)>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach ((ImGuiDrawList drawList, _) in _drawLists)
                        _pooledDrawLists.Return(drawList);
                    _drawLists.Clear();

                    _pooledDrawLists.Dispose();
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

        internal ImGuiDrawList GetDrawList() => _pooledDrawLists.Get();
        internal void ReturnDrawList(ImGuiDrawList drawList, int layer) => _drawLists.Add((drawList, layer));

        internal void ClearAllDrawLists()
        {
            foreach (var kvp in _drawLists)
            {
                _pooledDrawLists.Return(kvp.Item1);
            }

            _drawLists.Clear();
        }

        internal void SortDrawLists() => _drawLists.Sort(static (x, y) => x.Item2.CompareTo(y.Item2));

        public ImGuiStateController StateController => _stateController;
        public ImGuiStyle Style => _style;

        public ImGuiFont Font => _font;

        internal ReadOnlySpan<(ImGuiDrawList, int)> DrawLists => _drawLists.AsSpan();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ImGuiVertex(ImGuiVector2 Position, ImGuiVector2 UV, uint Color);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ImGuiVector2(float X, float Y)
    {
        public static implicit operator ImGuiVector2(Vector2 vector) => Unsafe.ReadUnaligned<ImGuiVector2>(ref Unsafe.As<Vector2, byte>(ref vector));
    }
}
