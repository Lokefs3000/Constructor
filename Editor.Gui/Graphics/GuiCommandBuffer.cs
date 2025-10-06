using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using Editor.Gui.Resources;
using Primary.Assets;
using Primary.Common;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Editor.Gui.Graphics
{
    public sealed class GuiCommandBuffer : IDisposable
    {
        private TextureAsset _plainAtlasTexture;

        private List<GuiRenderFocusRegion> _focusRegions;
        private List<GuiDrawCommand> _drawCommands;

        private UnsafeList<GuiVertex> _vertices;
        private UnsafeList<ushort> _indices;

        private int _lastIndexCount;
        private int _globalVertexOffset;
        private int _currentVertexOffset;

        private bool _forceNewCommand;

        private bool _disposedValue;

        internal GuiCommandBuffer()
        {
            _plainAtlasTexture = AssetManager.LoadAsset<TextureAsset>("Content/EdGui_PlainAtlas.dds", true)!;

            _focusRegions = new List<GuiRenderFocusRegion>();
            _drawCommands = new List<GuiDrawCommand>();

            _vertices = new UnsafeList<GuiVertex>(32 * 4);
            _indices = new UnsafeList<ushort>(32 * 6);

            _lastIndexCount = 0;
            _globalVertexOffset = 0;
            _currentVertexOffset = 0;

            _forceNewCommand = false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _vertices.Dispose();
                _indices.Dispose();

                _disposedValue = true;
            }
        }

        ~GuiCommandBuffer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Clear()
        {
            _lastIndexCount = 0;
            _globalVertexOffset = 0;
            _currentVertexOffset = 0;

            _forceNewCommand = false;

            _vertices.Clear();
            _indices.Clear();

            _focusRegions.Clear();
            _drawCommands.Clear();
        }

        internal void End()
        {
            if (_drawCommands.Count > 0)
                _drawCommands.AsSpan()[_drawCommands.Count - 1].IndexCount = (uint)(_indices.Count - _lastIndexCount);
            if (_focusRegions.Count > 0)
                _focusRegions.AsSpan()[_focusRegions.Count - 1].CommandsEnd = _drawCommands.Count;
        }

        internal void SetRenderFocus(DockingContainer focus)
        {
            if (_focusRegions.Count > 0)
            {
                ref GuiRenderFocusRegion region = ref _focusRegions.AsSpan()[_focusRegions.Count - 1];
                if (region.Container == focus)
                    return;

                region.CommandsEnd = _drawCommands.Count;
            }

            _focusRegions.Add(new GuiRenderFocusRegion(focus, _drawCommands.Count, 0));
            _forceNewCommand = true;
        }

        private void PushNewDrawCommand(TextureAsset asset)
        {
            if (_drawCommands.Count > 0)
                _drawCommands.AsSpan()[_drawCommands.Count - 1].IndexCount = (uint)(_indices.Count - _lastIndexCount);

            _drawCommands.Add(new GuiDrawCommand((uint)(_globalVertexOffset + _currentVertexOffset), 0, (uint)_indices.Count, asset));

            _lastIndexCount = _indices.Count;
            _globalVertexOffset += _currentVertexOffset;
            _currentVertexOffset = 0;

            _forceNewCommand = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool NeedsNewDrawCommand(TextureAsset asset) => _forceNewCommand || _drawCommands.Count == 0 || _drawCommands[_drawCommands.Count - 1].Texture != asset;

        private void AddRectangleToMesh(Vector2 minimum, Vector2 maximum, Vector2 uvMin, Vector2 uvMax, Color color)
        {
            _vertices.Add(new GuiVertex(minimum, uvMin, color));
            _vertices.Add(new GuiVertex(new Vector2(maximum.X, minimum.Y), new Vector2(uvMax.X, uvMin.Y), color));
            _vertices.Add(new GuiVertex(new Vector2(minimum.X, maximum.Y), new Vector2(uvMin.X, uvMax.Y), color));
            _vertices.Add(new GuiVertex(maximum, uvMax, color));

            _indices.Add((ushort)(_currentVertexOffset + 2));
            _indices.Add((ushort)(_currentVertexOffset));
            _indices.Add((ushort)(_currentVertexOffset + 1));
            _indices.Add((ushort)(_currentVertexOffset + 3));
            _indices.Add((ushort)(_currentVertexOffset + 2));
            _indices.Add((ushort)(_currentVertexOffset + 1));

            _currentVertexOffset += 4;
        }

        public void StrokeRectangle(Vector2 minimum, Vector2 maximum, Color color, float thickness = 1.0f)
        {

        }
        public void StrokeRectangleRoundedPoly(Vector2 minimum, Vector2 maximum, Color color, float rounding, int resolution, CornerRoundingFlags flags = CornerRoundingFlags.All, float thickness = 1.0f)
        {

        }
        public void StrokeCircle(Vector2 center, float radius, int resolution, Color color, float thickness = 1.0f)
        {

        }
        public void StrokeArcPoly(Vector2 center, float radius, float start, float end, int resolution, Color color, float thickness = 1.0f)
        {

        }
        public void StrokeTriangle(Vector2 a, Vector2 b, Vector2 c, Color color, float thickness = 1.0f)
        {

        }

        public void FillRectangle(Vector2 minimum, Vector2 maximum, Color color)
        {
            if (NeedsNewDrawCommand(_plainAtlasTexture))
                PushNewDrawCommand(_plainAtlasTexture);

            Vector2 half = new Vector2(0.5f);

            _vertices.EnsureCapacity(_vertices.Count + 4);
            _indices.EnsureCapacity(_indices.Count + 6);

            AddRectangleToMesh(minimum, maximum, half, half, color);
        }
        public void FillRectangleRounded(Vector2 minimum, Vector2 maximum, Color color, float rounding, CornerRoundingFlags flags = CornerRoundingFlags.All)
        {
            if (NeedsNewDrawCommand(_plainAtlasTexture))
                PushNewDrawCommand(_plainAtlasTexture);

            Vector2 half = new Vector2(0.5f);

            _vertices.EnsureCapacity(_vertices.Count + 4);
            _indices.EnsureCapacity(_indices.Count + 6);

            if (flags == CornerRoundingFlags.None)
            {
                AddRectangleToMesh(minimum, maximum, half, half, color);
                return;
            }

            if (FlagUtility.HasEither(flags, CornerRoundingFlags.Top))
            {
                CornerRoundingFlags topFlags = flags & CornerRoundingFlags.Top;
                switch (topFlags)
                {
                    case CornerRoundingFlags.Top:
                        {
                            _vertices.EnsureCapacity(_vertices.Count + 12);
                            _indices.EnsureCapacity(_indices.Count + 24);

                            AddRectangleToMesh(minimum, minimum + new Vector2(rounding), new Vector2(0.0f), new Vector2(0.5f), color);
                            AddRectangleToMesh(new Vector2(maximum.X - rounding, minimum.Y), new Vector2(maximum.X, minimum.Y + rounding), new Vector2(0.5f, 0.0f), new Vector2(1.0f, 0.5f), color);

                            AddRectangleToMesh(minimum + new Vector2(rounding, 0.0f), new Vector2(maximum.X - rounding, minimum.Y + rounding), half, half, color);

                            break;
                        }
                    case CornerRoundingFlags.TopLeft:
                        {
                            _vertices.EnsureCapacity(_vertices.Count + 8);
                            _indices.EnsureCapacity(_indices.Count + 16);

                            AddRectangleToMesh(minimum, minimum + new Vector2(rounding), new Vector2(0.0f), new Vector2(0.5f), color);

                            AddRectangleToMesh(minimum + new Vector2(rounding, 0.0f), new Vector2(maximum.X, minimum.Y + rounding), half, half, color);

                            break;
                        }
                    case CornerRoundingFlags.TopRight:
                        {
                            _vertices.EnsureCapacity(_vertices.Count + 8);
                            _indices.EnsureCapacity(_indices.Count + 16);

                            AddRectangleToMesh(new Vector2(maximum.X - rounding, minimum.Y), new Vector2(maximum.X, minimum.Y + rounding), new Vector2(0.5f, 0.0f), new Vector2(1.0f, 0.5f), color);

                            AddRectangleToMesh(minimum, new Vector2(maximum.X - rounding, minimum.Y + rounding), half, half, color);

                            break;
                        }
                }
            }

            if (FlagUtility.HasEither(flags, CornerRoundingFlags.Bottom))
            {
                CornerRoundingFlags bottomFlags = flags & CornerRoundingFlags.Bottom;
                switch (bottomFlags)
                {
                    case CornerRoundingFlags.Bottom:
                        {
                            _vertices.EnsureCapacity(_vertices.Count + 12);
                            _indices.EnsureCapacity(_indices.Count + 24);

                            AddRectangleToMesh(new Vector2(minimum.X, maximum.Y - rounding), new Vector2(minimum.X + rounding, maximum.Y), new Vector2(0.0f, 0.5f), new Vector2(0.5f, 1.0f), color);
                            AddRectangleToMesh(new Vector2(maximum.X - rounding, maximum.Y - rounding), maximum, new Vector2(0.5f), new Vector2(1.0f), color);

                            AddRectangleToMesh(new Vector2(minimum.X + rounding, maximum.Y - rounding), maximum - new Vector2(rounding, 0.0f), half, half, color);

                            break;
                        }
                    case CornerRoundingFlags.BottomLeft:
                        {
                            _vertices.EnsureCapacity(_vertices.Count + 8);
                            _indices.EnsureCapacity(_indices.Count + 16);

                            AddRectangleToMesh(new Vector2(minimum.X, maximum.Y - rounding), new Vector2(minimum.X + rounding, maximum.Y), new Vector2(0.0f, 0.5f), new Vector2(0.5f, 1.0f), color);

                            AddRectangleToMesh(new Vector2(minimum.X + rounding, maximum.Y - rounding), maximum, half, half, color);

                            break;
                        }
                    case CornerRoundingFlags.BottomRight:
                        {
                            _vertices.EnsureCapacity(_vertices.Count + 8);
                            _indices.EnsureCapacity(_indices.Count + 16);

                            AddRectangleToMesh(new Vector2(maximum.X - rounding, maximum.Y - rounding), maximum, new Vector2(0.5f), new Vector2(1.0f), color);

                            AddRectangleToMesh(new Vector2(minimum.X, maximum.Y - rounding), maximum - new Vector2(rounding, 0.0f), half, half, color);

                            break;
                        }
                }
            }

            if (!FlagUtility.HasEither(flags, CornerRoundingFlags.Top))
            {
                AddRectangleToMesh(minimum, maximum - new Vector2(0.0f, rounding), half, half, color);
            }
            else if (!FlagUtility.HasEither(flags, CornerRoundingFlags.Bottom))
            {
                AddRectangleToMesh(minimum + new Vector2(0.0f, rounding), maximum, half, half, color);
            }
            else
            {
                AddRectangleToMesh(minimum + new Vector2(0.0f, rounding), maximum - new Vector2(0.0f, rounding), half, half, color);
            }
        }
        public void FillRectangleRoundedPoly(Vector2 minimum, Vector2 maximum, Color color, float rounding, int resolution, CornerRoundingFlags flags = CornerRoundingFlags.All)
        {

        }
        public void FillCircle(Vector2 center, float radius, Color color)
        {

        }
        public void FillCirclePoly(Vector2 center, float radius, int resolution, Color color)
        {

        }
        public void FillArcPoly(Vector2 center, float radius, float start, float end, int resolution, Color color)
        {

        }
        public void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {

        }

        public void Text(Vector2 start, ReadOnlySpan<char> text, float size, Color color, GuiTextAlignment alignment = GuiTextAlignment.Left, GuiFont? font = null)
        {
            font ??= EditorGuiManager.Instance.DefaultFont;

            if (font.Texture == null)
                return;

            if (NeedsNewDrawCommand(font.Texture))
                PushNewDrawCommand(font.Texture);

            if (alignment == GuiTextAlignment.Right)
                start.X -= font.CalculateTextWidth(text, size);
            else if (alignment == GuiTextAlignment.Middle)
                start.X -= font.CalculateTextWidth(text, size) * 0.5f;

            _vertices.EnsureCapacity(_vertices.Count + 4 * text.Length);
            _indices.EnsureCapacity(_indices.Count + 6 * text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                ref readonly GuiFontGlyph glyph = ref font.TryGetGlyphData(text[i]);
                if (!Unsafe.IsNullRef(in glyph))
                {
                    if (glyph.Visible)
                    {
                        Vector4 boundaries = glyph.Boundaries * size + new Vector4(start.X, start.Y, start.X, start.Y);

                        AddRectangleToMesh(
                            new Vector2(boundaries.X, boundaries.Y), new Vector2(boundaries.Z, boundaries.W),
                            new Vector2(glyph.UVs.X, glyph.UVs.Y), new Vector2(glyph.UVs.Z, glyph.UVs.W),
                            color);
                    }

                    start.X += glyph.Advance * size;
                }
            }
        }

        internal Span<GuiVertex> Vertices => _vertices.AsSpan();
        internal Span<ushort> Indices => _indices.AsSpan();

        internal Span<GuiRenderFocusRegion> FocusRegions => _focusRegions.AsSpan();
        internal Span<GuiDrawCommand> DrawCommands => _drawCommands.AsSpan();

        internal bool IsEmpty => _drawCommands.Count == 0;
    }

    [Flags]
    public enum CornerRoundingFlags : byte
    {
        None = 0,

        TopLeft = 1 << 0,
        TopRight = 1 << 1,
        BottomLeft = 1 << 2,
        BottomRight = 1 << 3,

        Top = TopLeft | TopRight,
        Bottom = BottomLeft | BottomRight,
        Left = TopLeft | BottomLeft,
        Right = TopRight | BottomRight,

        All = TopLeft | TopRight | BottomLeft | BottomRight
    }

    public enum GuiTextAlignment : byte
    {
        Left = 0,
        Middle,
        Right
    }

    internal record struct GuiVertex(Vector2 Position, Vector2 UV, Color Color, float Parameter = 0.0f);
    internal record struct GuiDrawCommand(uint BaseVertexOffset, uint IndexCount, uint IndexOffset, TextureAsset Texture);
    internal record struct GuiRenderFocusRegion(DockingContainer Container, int CommandsStart, int CommandsEnd);
}
