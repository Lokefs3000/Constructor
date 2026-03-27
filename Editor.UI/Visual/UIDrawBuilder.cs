using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Common.Memory;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using Vortice.Mathematics.PackedVector;
using static System.Net.Mime.MediaTypeNames;

namespace Editor.UI.Visual
{
    public sealed class UIDrawBuilder : IDisposable
    {
        private List<BuiltDrawGroup> _groups;
        private List<BuiltDrawSegment> _segments;

        private LinearResizingAllocator _allocator;

        private bool _disposedValue;

        internal UIDrawBuilder()
        {
            _groups = new List<BuiltDrawGroup>();
            _segments = new List<BuiltDrawSegment>();

            _allocator = new LinearResizingAllocator(2048);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _allocator.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Clear()
        {
            _groups.Clear();
            _segments.Clear();

            _allocator.Reset();
        }

        internal unsafe void BuildDraws(UIPainter painter, UIMeshBuilder meshBuilder)
        {
            UIManager ui = UIManager.Instance;

            ReadOnlySpan<PaintDrawSegment> segments = painter.Segments;
            for (int i = 0; i < segments.Length; ++i)
            {
                ref readonly PaintDrawSegment segment = ref segments[i];
                int nextOffset = i == segments.Length - 1 ? painter.Cmds.Length : segments[i + 1].BaseCommandIndex;

                Span<PaintCmd> cmds = painter.Cmds.Slice(segment.BaseCommandIndex, nextOffset - segment.BaseCommandIndex);
                if (cmds.IsEmpty)
                    continue;

                BuiltSegmentType currentSegmentType;
                PaintCmd currentCmd;

                {
                    ref readonly PaintCmd firstCmd = ref cmds[0];

                    currentSegmentType = GetBuiltSegmentType(firstCmd.Type) + 1;
                    currentCmd = firstCmd;
                }

                int startSegmentCount = _segments.Count;

                cmds.Sort(PaintCmdCompararer.Default);
                for (int j = 0; j < cmds.Length; j++)
                {
                    ref readonly PaintCmd cmd = ref cmds[j];
                    if (cmd.Type == PaintType.Text)
                    {
                        CmdTextData text = cmd.TextData;

                        ShapedTextData textData = (ShapedTextData)painter.StoredObjects.Get(text.ShapedDataIdx);

                        if (!textData.IsEmpty)
                        {
                            UIFontStyle? lastFontStyle = null;
                            int lastIndexCount = meshBuilder.IndexCount;

                            if (_segments.Count > 0)
                            {
                                BuiltDrawSegment builtSegment = _segments[_segments.Count - 1];

                                lastIndexCount = builtSegment.IndexOffset;
                                if (builtSegment.Type == BuiltSegmentType.Text)
                                    lastFontStyle = builtSegment.Value as UIFontStyle;
                            }

                            currentSegmentType = BuiltSegmentType.Text;

                            Vector2 basePosition = text.Position;
                            float lastLineOffset = float.NegativeInfinity;

                            UITextAlignment vertical = text.Builder.Alignment & UITextAlignment.TMBMask;
                            UITextAlignment horizontal = text.Builder.Alignment & UITextAlignment.LCRMask;

                            float baseYPosition = text.Position.Y;
                            switch (vertical)
                            {
                                case UITextAlignment.Middle: baseYPosition += text.Builder.MaxExtents.Y * 0.5f - textData.TotalSize.Y * 0.5f; break;
                                case UITextAlignment.Bottom: baseYPosition += text.Builder.MaxExtents.Y - textData.TotalSize.Y; break;
                            }

                            foreach (TextRenderSegment render in textData.Iterate())
                            {
                                if (render.VisualInfo.Style.AtlasTexture == null)
                                    continue;

                                if (lastFontStyle != render.VisualInfo.Style && lastIndexCount < meshBuilder.IndexCount)
                                {
                                    _segments.Add(new BuiltDrawSegment(BuiltSegmentType.Text, meshBuilder.IndexCount, render.VisualInfo.Style));

                                    lastFontStyle = render.VisualInfo.Style;
                                    lastIndexCount = meshBuilder.IndexCount;
                                }

                                uint metadataOffset = (uint)_allocator.CurrentOffset;
                                byte* ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<TextMetadata>());

                                *(TextMetadata*)ptr = new TextMetadata(
                                    text.Paint.StrokeEnabled,
                                    new Half4(render.VisualInfo.DrawColor.Solid.AsVector4()),
                                    cmd.ZIndex);

                                if (lastLineOffset < render.LineOffset)
                                {
                                    basePosition = new Vector2(text.Position.X, baseYPosition + render.LineOffset);

                                    switch (horizontal)
                                    {
                                        case UITextAlignment.Center: basePosition.X = text.Builder.MaxExtents.X * 0.5f - render.TextSize.X * 0.5f; break;
                                        case UITextAlignment.Right: basePosition.X = text.Builder.MaxExtents.X - render.TextSize.X; break;
                                    }
                                }

                                meshBuilder.AddGlyphs(new Vector2(basePosition.X + render.LeftOffset, basePosition.Y), render.VisualInfo.Style, render.VisualInfo, render.Letters, metadataOffset);
                            }
                        }
                    }
                    else
                    {
                        uint metadataOffset = (uint)_allocator.CurrentOffset;

                        BuiltSegmentType segmentType = GetBuiltSegmentType(cmd.Type);
                        if (segmentType != currentSegmentType || !cmd.Equals(currentCmd))
                        {
                            object? value = cmd.Type switch
                            {
                                PaintType.Image => painter.StoredObjects.Get(cmd.ImageData.ImageIdx),
                                _ => null
                            };

                            _segments.Add(new BuiltDrawSegment(segmentType, meshBuilder.IndexCount, value));

                            currentSegmentType = segmentType;
                            currentCmd = cmd;
                        }

                        switch (cmd.Type)
                        {
                            case PaintType.Points:
                                {
                                    CmdPointsData points = cmd.PointsData;
                                    byte* ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<PointsMetadata>() + points.Count * Unsafe.SizeOf<Vector2>());

                                    *(PointsMetadata*)ptr = new PointsMetadata(
                                        points.Paint.StrokeEnabled,
                                        new Half4(points.Paint.Color.Solid.AsVector4()),
                                        cmd.ZIndex);

                                    points.Points.AsSpan(0, points.Count).CopyTo(new Ptr<Vector2>((Vector2*)(ptr + Unsafe.SizeOf<PointsMetadata>())).AsSpan(0, points.Count));
                                    break;
                                }
                            case PaintType.Lines:
                                {
                                    CmdLinesData lines = cmd.LinesData;
                                    byte* ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<LinesMetadata>());

                                    *(LinesMetadata*)ptr = new LinesMetadata(
                                        lines.Paint.StrokeEnabled,
                                        new Half4(lines.Paint.Color.Solid.AsVector4()),
                                        cmd.ZIndex,
                                        (Half)lines.LineWidth);

                                    Span<Vector2> points = lines.Lines.AsSpan(0, lines.Count);
                                    if (lines.Mode == UILineMode.List)
                                        meshBuilder.AddLinesList(points, lines.LineWidth, metadataOffset);
                                    else
                                        meshBuilder.AddLinesStrip(points, lines.LineWidth, metadataOffset);
                                    break;
                                }
                            case PaintType.Rect:
                                {
                                    CmdRectData rect = cmd.RectData;
                                    byte* ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<RectMetadata>());

                                    Boundaries bounds;
                                    Vector2 size;

                                    if (rect.Paint.StrokeEnabled)
                                    {
                                        bounds = rect.Rect.Grow(new Vector2(rect.Paint.StrokeWidth));
                                        size = bounds.Size;
                                    }
                                    else
                                    {
                                        bounds = rect.Rect;
                                        size = Vector2.One;
                                    }

                                    *(RectMetadata*)ptr = new RectMetadata(
                                        rect.Paint.StrokeEnabled,
                                        new Half4(rect.Paint.Color.Solid.AsVector4()),
                                        cmd.ZIndex,
                                        new UShort2(size));

                                    if (rect.Paint.StrokeEnabled)
                                    {
                                        ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<StrokeMetadata>());
                                        *(StrokeMetadata*)ptr = new StrokeMetadata(
                                            new Half4(rect.Paint.StrokeColor.Solid.AsVector4()),
                                            (Half)rect.Paint.StrokeWidth);
                                    }

                                    meshBuilder.AddRoundedRect(bounds, size, metadataOffset);
                                    break;
                                }
                            case PaintType.RoundedRect:
                                {
                                    CmdRoundedRectData roundedRect = cmd.RoundedRectData;
                                    byte* ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<RoundedRectMetadata>());

                                    Boundaries bounds;
                                    Vector2 size;

                                    if (roundedRect.Paint.StrokeEnabled)
                                    {
                                        bounds = roundedRect.Rect.Grow(new Vector2(roundedRect.Paint.StrokeWidth));
                                        size = bounds.Size;
                                    }
                                    else
                                    {
                                        bounds = roundedRect.Rect;
                                        size = Vector2.One;
                                    }

                                    *(RoundedRectMetadata*)ptr = new RoundedRectMetadata(
                                        roundedRect.Paint.StrokeEnabled,
                                        new Half4(roundedRect.Paint.Color.Solid.AsVector4()),
                                        cmd.ZIndex,
                                        (Half)(roundedRect.Radius + roundedRect.Radius),
                                        new UShort2(size));

                                    if (roundedRect.Paint.StrokeEnabled)
                                    {
                                        ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<StrokeMetadata>());
                                        *(StrokeMetadata*)ptr = new StrokeMetadata(
                                            new Half4(roundedRect.Paint.StrokeColor.Solid.AsVector4()),
                                            (Half)roundedRect.Paint.StrokeWidth);
                                    }

                                    meshBuilder.AddRoundedRect(bounds, size, metadataOffset);
                                    break;
                                }
                            case PaintType.Circle:
                                {
                                    CmdCircleData circle = cmd.CircleData;
                                    byte* ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<CircleMetadata>());

                                    float radius = circle.Paint.StrokeEnabled ? circle.Radius + circle.Paint.StrokeWidth : circle.Radius;

                                    *(CircleMetadata*)ptr = new CircleMetadata(
                                        circle.Paint.StrokeEnabled,
                                        new Half4(circle.Paint.Color.Solid.AsVector4()),
                                        cmd.ZIndex,
                                        (Half)radius);

                                    if (circle.Paint.StrokeEnabled)
                                    {
                                        ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<StrokeMetadata>());
                                        *(StrokeMetadata*)ptr = new StrokeMetadata(
                                            new Half4(circle.Paint.StrokeColor.Solid.AsVector4()),
                                            (Half)circle.Paint.StrokeWidth);
                                    }

                                    meshBuilder.AddRoundedRect(new Boundaries(circle.Center - new Vector2(radius), circle.Center + new Vector2(radius)), new Vector2(radius), metadataOffset);
                                    break;
                                }
                            case PaintType.Triangle:
                                {
                                    CmdTriangleData triangle = cmd.TriangleData;
                                    byte* ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<TriangleMetadata>());

                                    Vector2 a, b, c;
                                    if (triangle.Paint.StrokeEnabled)
                                    {
                                        a = triangle.A - new Vector2(triangle.Paint.StrokeWidth);
                                        b = triangle.B + new Vector2(0.0f, triangle.Paint.StrokeWidth);
                                        c = triangle.C + new Vector2(triangle.Paint.StrokeWidth, -triangle.Paint.StrokeWidth);
                                    }
                                    else
                                    {
                                        a = triangle.A;
                                        b = triangle.B;
                                        c = triangle.C;
                                    }

                                    Vector2 min = Vector2.Min(Vector2.Min(a, b), c);
                                    Vector2 max = Vector2.Max(Vector2.Max(a, b), c);

                                    *(TriangleMetadata*)ptr = new TriangleMetadata(
                                        triangle.Paint.StrokeEnabled,
                                        new Half4(triangle.Paint.Color.Solid.AsVector4()),
                                        cmd.ZIndex,
                                        new Half2(a - min),
                                        new Half2(b - min),
                                        new Half2(c - min));

                                    if (triangle.Paint.StrokeEnabled)
                                    {
                                        ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<StrokeMetadata>());
                                        *(StrokeMetadata*)ptr = new StrokeMetadata(
                                            new Half4(triangle.Paint.StrokeColor.Solid.AsVector4()),
                                            (Half)triangle.Paint.StrokeWidth);
                                    }

                                    meshBuilder.AddRoundedRect(new Boundaries(min, max), max - min, metadataOffset);
                                    break;
                                }
                            case PaintType.Image:
                                {
                                    CmdImageData image = cmd.ImageData;
                                    byte* ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<ImageMetadata>());

                                    Boundaries bounds;
                                    Vector2 size;

                                    if (image.Paint.StrokeEnabled)
                                    {
                                        bounds = image.Rect.Grow(new Vector2(image.Paint.StrokeWidth));
                                        size = bounds.Size;
                                    }
                                    else
                                    {
                                        bounds = image.Rect;
                                        size = Vector2.One;
                                    }

                                    *(ImageMetadata*)ptr = new ImageMetadata(
                                        image.Paint.StrokeEnabled,
                                        new Half4(image.Paint.Color.Solid.AsVector4()),
                                        cmd.ZIndex,
                                        new UShort2(size));

                                    if (image.Paint.StrokeEnabled)
                                    {
                                        ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<StrokeMetadata>());
                                        *(StrokeMetadata*)ptr = new StrokeMetadata(
                                            new Half4(image.Paint.StrokeColor.Solid.AsVector4()),
                                            (Half)image.Paint.StrokeWidth);
                                    }

                                    meshBuilder.AddRect(bounds, image.UVMin, image.UVMax, metadataOffset);
                                    break;
                                }
                        }
                    }
                }

                if (startSegmentCount < _segments.Count)
                {
                    _groups.Add(new BuiltDrawGroup(
                        _segments.Count - startSegmentCount,
                        segment.MatrixId,
                        segment.ClipId,
                        segment.BlendMode));
                }
            }

            static BuiltSegmentType GetBuiltSegmentType(PaintType type) => type switch
            {
                PaintType.Points => BuiltSegmentType.Points,
                PaintType.Lines => BuiltSegmentType.Lines,
                PaintType.Rect => BuiltSegmentType.Rect,
                PaintType.RoundedRect => BuiltSegmentType.RoundedRect,
                PaintType.Circle => BuiltSegmentType.Circle,
                PaintType.Triangle => BuiltSegmentType.Triangle,
                PaintType.Image => BuiltSegmentType.Image,
                PaintType.Text => BuiltSegmentType.Text,
                _ => throw new NotImplementedException(),
            };
        }

        public ReadOnlySpan<BuiltDrawGroup> Groups => _groups.AsSpan();
        public ReadOnlySpan<BuiltDrawSegment> Segments => _segments.AsSpan();

        public unsafe ReadOnlySpan<byte> Metadata => new ReadOnlySpan<byte>(_allocator.Pointer.ToPointer(), _allocator.CurrentOffset);

        public bool IsEmpty => _groups.Count == 0;

        private sealed class PaintCmdCompararer : IComparer<PaintCmd>
        {
            public int Compare(PaintCmd x, PaintCmd y)
            {
                return x.CommandSortIndex.CompareTo(y.CommandSortIndex);
            }

            public static readonly PaintCmdCompararer Default = new PaintCmdCompararer();
        }
    }

    public readonly record struct BuiltDrawGroup(int SegmentCount, int MatrixId, int ClipId, UIBlendMode BlendMode);
    public readonly record struct BuiltDrawSegment(BuiltSegmentType Type, int IndexOffset, object? Value);

    public enum BuiltSegmentType : byte
    {
        Points = 0,
        Lines,
        Rect,
        RoundedRect,
        Circle,
        Triangle,
        Image,
        Text
    }
}
