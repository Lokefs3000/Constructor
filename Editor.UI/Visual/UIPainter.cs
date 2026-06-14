using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Helpers;
using Editor.UI.Text;
using Primary.Assets;
using Primary.Common;
using Primary.Common.Memory;
using Primary.Mathematics;
using Primary.RHI;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Editor.UI.Visual
{
    public sealed class UIPainter
    {
        private List<PaintDrawSegment> _segments;
        private List<PaintCmd> _cmds;

        private MutableSegment _currentSegment;

        private Stack<int> _matricies;
        private Stack<int> _clipRects;
        private Stack<UIBlendMode> _blendModes;

        private DataIdentifier<Matrix3x2> _matrixData;
        private DataIdentifier<Boundaries> _clipRectData;

        private LinearBlockAllocator _tempAllocator;
        private DataIdentifier<object> _storedObjects;
        private StringHandleAllocator _stringAllocator;

        internal UIPainter()
        {
            _segments = new List<PaintDrawSegment>();
            _cmds = new List<PaintCmd>();

            _currentSegment = default;

            _matricies = new Stack<int>();
            _clipRects = new Stack<int>();
            _blendModes = new Stack<UIBlendMode>();

            _matrixData = new DataIdentifier<Matrix3x2>();
            _clipRectData = new DataIdentifier<Boundaries>();

            _tempAllocator = new LinearBlockAllocator(1024);
            _storedObjects = new DataIdentifier<object>();
            _stringAllocator = new StringHandleAllocator(_tempAllocator);
        }

        internal void Clear()
        {
            _segments.Clear();
            _cmds.Clear();

            _currentSegment = new MutableSegment(0);

            _matricies.Clear();
            _clipRects.Clear();
            _blendModes.Clear();

            _matrixData.Clear();
            _clipRectData.Clear();

            _tempAllocator.Reset();
            _storedObjects.Clear();
            _stringAllocator.Clear();
        }

        internal void ClearStoredObjects()
        {
            _storedObjects.Clear();
        }

        internal void FinishDrawing()
        {
            if (_currentSegment.BaseCommandIndex < _cmds.Count)
                _segments.Add(_currentSegment);
        }

        public void PushMatrix(Matrix3x2 matrix, bool multiplyWithPrevious = false)
        {
            if (_currentSegment.BaseCommandIndex < _cmds.Count)
            {
                _segments.Add(_currentSegment);
                _currentSegment = new MutableSegment(_cmds.Count);
            }

            if (multiplyWithPrevious && _matricies.Count > 0)
                matrix *= _matrixData.Get(_matricies.Peek());

            int id = _matrixData.AddOrGet(matrix);

            _matricies.Push(id);
            _currentSegment.MatrixId = id;
        }
        public void PopMatrix()
        {
            if (_currentSegment.BaseCommandIndex < _cmds.Count)
            {
                _segments.Add(_currentSegment);
                _currentSegment = new MutableSegment(_cmds.Count);
            }

            _matricies.TryPop(out int _);

            if (_matricies.TryPeek(out int id))
                _currentSegment.MatrixId = id;
            else
                _currentSegment.MatrixId = -1;
        }

        public void PushClipRect(Boundaries rect, bool clipWithPreviousRect = false)
        {
            if (_currentSegment.BaseCommandIndex < _cmds.Count)
            {
                _segments.Add(_currentSegment);
                _currentSegment = new MutableSegment(_cmds.Count);
            }

            if (clipWithPreviousRect && _clipRects.Count > 0)
                rect = Boundaries.Clip(rect, _clipRectData.Get(_clipRects.Peek()));

            int id = _clipRectData.AddOrGet(rect);

            _clipRects.Push(id);
            _currentSegment.ClipId = id;
        }
        public void PopClipRect()
        {
            if (_currentSegment.BaseCommandIndex < _cmds.Count)
            {
                _segments.Add(_currentSegment);
                _currentSegment = new MutableSegment(_cmds.Count);
            }

            _clipRects.TryPop(out int _);

            if (_clipRects.TryPeek(out int id))
                _currentSegment.ClipId = id;
            else
                _currentSegment.ClipId = -1;
        }

        public void BeginBlendMode(UIBlendMode blendMode)
        {
            if (_currentSegment.BaseCommandIndex < _cmds.Count)
            {
                _segments.Add(_currentSegment);
                _currentSegment = new MutableSegment(_cmds.Count);
            }

            _blendModes.Push(blendMode);
            _currentSegment.BlendMode = blendMode;
        }
        public void EndBlendMode()
        {
            if (_currentSegment.BaseCommandIndex < _cmds.Count)
            {
                _segments.Add(_currentSegment);
                _currentSegment = new MutableSegment(_cmds.Count);
            }

            _blendModes.TryPop(out _);

            if (_blendModes.TryPeek(out UIBlendMode result))
                _currentSegment.BlendMode = result;
            else
                _currentSegment.BlendMode = UIBlendMode.Undefined;
        }

        internal void AddCmd(ushort zIndex, ushort objectIndex, CmdPointsData data) => _cmds.Add(new PaintCmd(zIndex, objectIndex, (uint)_cmds.Count, data));
        internal void AddCmd(ushort zIndex, ushort objectIndex, CmdLinesData data) => _cmds.Add(new PaintCmd(zIndex, objectIndex, (uint)_cmds.Count, data));
        internal void AddCmd(ushort zIndex, ushort objectIndex, CmdRectData data) => _cmds.Add(new PaintCmd(zIndex, objectIndex, (uint)_cmds.Count, data));
        internal void AddCmd(ushort zIndex, ushort objectIndex, CmdRoundedRectData data) => _cmds.Add(new PaintCmd(zIndex, objectIndex, (uint)_cmds.Count, data));
        internal void AddCmd(ushort zIndex, ushort objectIndex, CmdCircleData data) => _cmds.Add(new PaintCmd(zIndex, objectIndex, (uint)_cmds.Count, data));
        internal void AddCmd(ushort zIndex, ushort objectIndex, CmdTriangleData data) => _cmds.Add(new PaintCmd(zIndex, objectIndex, (uint)_cmds.Count, data));
        internal void AddCmd(ushort zIndex, ushort objectIndex, CmdImageData data) => _cmds.Add(new PaintCmd(zIndex, objectIndex, (uint)_cmds.Count, data));
        internal void AddCmd(ushort zIndex, ushort objectIndex, CmdTextData data) => _cmds.Add(new PaintCmd(zIndex, objectIndex, (uint)_cmds.Count, data));

        internal int GetObjectIndex(object obj) => _storedObjects.AddOrGet(obj);
        
        internal unsafe Ptr<T> AllocateTemporary<T>() where T : unmanaged => (T*)_tempAllocator.Allocate(Unsafe.SizeOf<T>());
        internal unsafe Ptr<T> AllocateTemporary<T>(int count) where T : unmanaged => (T*)_tempAllocator.Allocate(Unsafe.SizeOf<T>() * count);

        internal StringHandle GetStringHandle(ReadOnlySpan<char> text) => _stringAllocator.GetStringHandle(text);

        internal ReadOnlySpan<PaintDrawSegment> Segments => _segments.AsSpan();
        internal Span<PaintCmd> Cmds => _cmds.AsSpan();

        internal DataIdentifier<Matrix3x2> Matricies => _matrixData;
        internal DataIdentifier<Boundaries> ClipRects => _clipRectData;

        internal DataIdentifier<object> StoredObjects => _storedObjects;

        internal record struct MutableSegment(int BaseCommandIndex, int MatrixId = -1, int ClipId = -1, UIBlendMode BlendMode = UIBlendMode.Undefined)
        {
            public static implicit operator PaintDrawSegment(MutableSegment segment) => new PaintDrawSegment(segment.BaseCommandIndex, segment.MatrixId, segment.ClipId, segment.BlendMode);
        }
    }

    public readonly record struct UIPainterContext(UIPainter Painter, ushort ZIndex, ushort ObjectIndex)
    {
        public void DrawPoints(ReadOnlySpan<Vector2> points, UIPaint paint)
        {
            if (points.IsEmpty)
                return;

            Ptr<Vector2> ptr = Painter.AllocateTemporary<Vector2>(points.Length);
            points.CopyTo(MemoryMarshal.CreateSpan(ref ptr.Ref, points.Length));

            Painter.AddCmd(ZIndex, ObjectIndex, new CmdPointsData(ptr, points.Length, paint.ToRaw(Painter)));
        }
        public void DrawLines(ReadOnlySpan<Vector2> sections, UIPaint paint, UILineMode lineMode, float width = 1.0f)
        {
            if (lineMode == UILineMode.Strip)
            {
                if (sections.Length <= 1)
                    return;

                Ptr<Vector2> ptr = Painter.AllocateTemporary<Vector2>(sections.Length);
                sections.CopyTo(ptr.AsSpan(0, sections.Length));

                Painter.AddCmd(ZIndex, ObjectIndex, new CmdLinesData(ptr, lineMode, sections.Length, paint.ToRaw(Painter), width * 0.75f));
            }
            else
            {
                if (sections.Length < 2)
                    return;
                int minCount = sections.Length - (sections.Length % 2);

                Ptr<Vector2> ptr = Painter.AllocateTemporary<Vector2>(minCount);
                sections.Slice(0, minCount).CopyTo(ptr.AsSpan(0, minCount));

                Painter.AddCmd(ZIndex, ObjectIndex, new CmdLinesData(ptr, lineMode, minCount, paint.ToRaw(Painter), width * 0.75f));
            }
        }
        public void DrawRect(Boundaries rect, UIPaint paint)
        {
            Painter.AddCmd(ZIndex, ObjectIndex, new CmdRectData(rect, paint.ToRaw(Painter)));
        }
        public void DrawRoundedRect(Boundaries rect, UIPaint paint, float radius)
        {
            Painter.AddCmd(ZIndex, ObjectIndex, new CmdRoundedRectData(rect, paint.ToRaw(Painter), radius));
        }
        public void DrawCircle(Vector2 center, float radius, UIPaint paint)
        {
            Painter.AddCmd(ZIndex, ObjectIndex, new CmdCircleData(center, radius, paint.ToRaw(Painter)));
        }
        public void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, UIPaint paint)
        {
            Painter.AddCmd(ZIndex, ObjectIndex, new CmdTriangleData(a, b, c, paint.ToRaw(Painter)));
        }
        public void DrawImage(Boundaries rect, UIPaint paint, Vector2 uvMin, Vector2 uvMax, RHITexture texture)
        {
            int imageIdx = Painter.GetObjectIndex(texture);
            Painter.AddCmd(ZIndex, ObjectIndex, new CmdImageData(rect, paint.ToRaw(Painter), uvMin, uvMax, imageIdx));
        }
        public void DrawImage(Boundaries rect, UIPaint paint, Vector2 uvMin, Vector2 uvMax, TextureAsset image)
        {
            int imageIdx = Painter.GetObjectIndex(image);
            Painter.AddCmd(ZIndex, ObjectIndex, new CmdImageData(rect, paint.ToRaw(Painter), uvMin, uvMax, imageIdx));
        }
        public void DrawText(Vector2 position, UIPaint paint, TextBuilder builder, UIFontTypeData? typeData, float fontSize, ReadOnlySpan<char> text)
        {
            if (typeData == null || text.IsEmpty)
                return;

            TextManager textManager = UIManager.Instance.TextManager;

            RawPaintData paintData = paint.ToRaw(Painter);
            RawTextBuilderData builderData = builder.ToRaw();

            TextVisualInfo visualInfo = new TextVisualInfo(paintData.Color, fontSize, typeData);
            TextWrapInfo wrapInfo = new TextWrapInfo(builderData.Origin, builderData.MaxExtents, builderData.AllowRichText, visualInfo);

            int textDataIdx = Painter.GetObjectIndex(textManager.ShapeTextDeferred(wrapInfo, builderData.Overflow, text));

            Painter.AddCmd(ZIndex, ObjectIndex, new CmdTextData(position, paintData, builderData, textDataIdx));
        }
        public void DrawText<T>(Vector2 position, UIPaint paint, TextBuilder builder, UIFontTypeData? typeData, float fontSize, T text) where T : IJaggedString
        {
            if (typeData == null)
                return;

            TextManager textManager = UIManager.Instance.TextManager;

            RawPaintData paintData = paint.ToRaw(Painter);
            RawTextBuilderData builderData = builder.ToRaw();

            TextVisualInfo visualInfo = new TextVisualInfo(paintData.Color, fontSize, typeData);
            TextWrapInfo wrapInfo = new TextWrapInfo(builderData.Origin, builderData.MaxExtents, builderData.AllowRichText, visualInfo);

            int textDataIdx = Painter.GetObjectIndex(textManager.ShapeTextDeferred(wrapInfo, builderData.Overflow, text));

            Painter.AddCmd(ZIndex, ObjectIndex, new CmdTextData(position, paintData, builderData, textDataIdx));
        }

        // Output modification
        public void PushClippingRect(Boundaries boundaries)
        {
            Painter.PushClipRect(boundaries, true);
        }

        public void PopClippingRect()
        {
            Painter.PopClipRect();
        }

        public void PushMatrix(Matrix3x2 matrix)
        {
            Painter.PushMatrix(matrix, true);
        }

        public void PopMatrix()
        {
            Painter.PopMatrix();
        }

        // Extended helper api
        public void DrawPoint(Vector2 position, UIPaint paint)
        {
            Ptr<Vector2> ptr = Painter.AllocateTemporary<Vector2>();
            ptr.Ref = position;

            Painter.AddCmd(ZIndex, ObjectIndex, new CmdPointsData(ptr, 1, paint.ToRaw(Painter)));
        }
        public void DrawLine(Vector2 from, Vector2 to, UIPaint paint, float width = 1.0f)
        {
            Ptr<(Vector2, Vector2)> ptr = Painter.AllocateTemporary<(Vector2, Vector2)>(1);
            ptr.Ref = (from, to);

            Painter.AddCmd(ZIndex, ObjectIndex, new CmdLinesData(ptr.As<Vector2>(), UILineMode.List, 2, paint.ToRaw(Painter), width * 0.75f));
        }
        public void DrawRect(Boundaries rect, UIPaint color, float radius = 0.0f)
        {
            if (radius > 0.0f)
                DrawRoundedRect(rect, color, radius);
            else
                DrawRect(rect, color);
        }
        public void DrawImage(Boundaries rect, UIPaint paint, RHITexture image)
        {
            int imageIdx = Painter.GetObjectIndex(image);
            Painter.AddCmd(ZIndex, ObjectIndex, new CmdImageData(rect, paint.ToRaw(Painter), Vector2.Zero, Vector2.One, imageIdx));
        }
        public void DrawImage(Boundaries rect, UIPaint paint, TextureAsset image)
        {
            int imageIdx = Painter.GetObjectIndex(image);
            Painter.AddCmd(ZIndex, ObjectIndex, new CmdImageData(rect, paint.ToRaw(Painter), Vector2.Zero, Vector2.One, imageIdx));
        }
        public void DrawImage(Boundaries rect, UIPaint paint, Sprite image)
        {
            int imageIdx = Painter.GetObjectIndex(image.Texture);
            Painter.AddCmd(ZIndex, ObjectIndex, new CmdImageData(rect, paint.ToRaw(Painter), image.UVMin, image.UVMax, imageIdx));
        }
    }

    public enum UIClipOp : byte
    {
        Difference = 0,
        Intersect
    }

    public enum UILineMode : byte
    {
        List = 0,
        Strip
    }

    public enum UIBlendMode : byte
    {
        Source = 0,
        SourceOver,
        SourceIn,
        SourceOut,

        Clip,
        ClipOver,
        ClipIn,
        ClipOut,

        Clear,
        Xor,

        Undefined = byte.MaxValue
    }

    public enum RectCorner : byte
    {
        TopLeft = 1 << 0,
        TopRight = 1 << 1,
        BottomLeft = 1 << 2,
        BottomRight = 1 << 3,

        Top = TopLeft | TopRight,
        Bottom = BottomLeft | BottomRight,
        Left = TopLeft | BottomLeft,
        Right = TopRight | BottomRight,

        None = 0,
        All = TopLeft | TopRight | BottomLeft | BottomRight
    }

    public enum TextOrigin : byte
    {
        Top = 0,
        Bottom
    }
}
