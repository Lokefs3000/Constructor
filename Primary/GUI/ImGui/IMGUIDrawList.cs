using Arch.LowLevel;
using System.Numerics;

using ImGuiIndex = ushort;

namespace Primary.GUI.ImGui
{
    public class IMGUIDrawList : IDisposable
    {
        private bool _disposedValue;

        private UnsafeList<ImGuiVertex> _vertices;
        private UnsafeList<ImGuiIndex> _indices;

        internal IMGUIDrawList()
        {
            _vertices = new UnsafeList<ImGuiVertex>(32 * 4);
            _indices = new UnsafeList<ImGuiIndex>(32 * 6);
        }

        public void DrawFilledRect(Vector2 min, Vector2 max, uint color = 0xffffffff)
        {
            _vertices.EnsureCapacity(_vertices.Count + 4);
            _indices.EnsureCapacity(_indices.Count + 6);

            int baseCount = _vertices.Count;

            _vertices.Add(new ImGuiVertex { Position = min, UV = new Vector2(0.0f, 0.0f), Color = color });
            _vertices.Add(new ImGuiVertex { Position = new Vector2(max.X, min.Y), UV = new Vector2(1.0f, 0.0f), Color = color });
            _vertices.Add(new ImGuiVertex { Position = new Vector2(min.X, max.Y), UV = new Vector2(0.0f, 1.0f), Color = color });
            _vertices.Add(new ImGuiVertex { Position = max, UV = new Vector2(1.0f, 1.0f), Color = color });

            _indices.Add((ushort)(baseCount + 2));
            _indices.Add((ushort)baseCount);
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 3));
            _indices.Add((ushort)(baseCount + 1));
            _indices.Add((ushort)(baseCount + 2));
        }

        public void DrawFilledArc(Vector2 center, float radius, float arcStart, float arcEnd, int segments, uint color = 0xffffffff)
        {
            if (arcEnd > arcStart)
            {
                float temp = arcStart;

                arcStart = arcEnd;
                arcEnd = temp;
            }

            Vector2 min = center - new Vector2(radius);
            Vector2 max = center + new Vector2(radius);

            float segmentWidth = (arcEnd - arcStart) / segments;
            for (int i = 0; i < segments; i++)
            {

            }
        }

        public void DrawString(ReadOnlySpan<char> text, float scale, uint color = 0xffffffff)
        {

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _vertices.Dispose();
                _indices.Dispose();

                _disposedValue = true;
            }
        }

        ~IMGUIDrawList()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
