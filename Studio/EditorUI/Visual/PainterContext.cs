using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Assets;
using EditorUI.Built;
using EditorUI.Text;
using EditorUI.Visual.Draw;
using Primary.Assets;
using Primary.Mathematics;
using Primary.RHI;

namespace EditorUI.Visual
{
    public record struct PainterContext
    {
        private readonly PainterData _data;
        private readonly GradientManager _gradientManager;

        private int _clipStackSize;

        internal PainterContext(PainterData data, GradientManager gradientManager)
        {
            _data = data;
            _gradientManager = gradientManager;

            _clipStackSize = 0;
        }

        public readonly void AddPoint(Vector2 point, Paint paint, float radius = 1.0f)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<PointsPaintCmd>() + Unsafe.SizeOf<Vector2>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new PointsPaintCmd
            {
                CmdType = PaintCmdType.Points,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                Radius = (Half)radius,
                PointCount = 1
            });

            Unsafe.WriteUnaligned(ref memory.DangerousGetReferenceAt(Unsafe.SizeOf<PointsPaintCmd>()), point);
        }

        public readonly void AddPoints(ReadOnlySpan<Vector2> points, Paint paint, float radius = 1.0f)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<PointsPaintCmd>() + Unsafe.SizeOf<Vector2>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new PointsPaintCmd
            {
                CmdType = PaintCmdType.Points,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                Radius = (Half)radius,
                PointCount = 1
            });

            points.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, Vector2>(ref memory.DangerousGetReferenceAt(Unsafe.SizeOf<PointsPaintCmd>())), points.Length));
        }

        public readonly void AddLine(Vector2 from, Vector2 to, Paint paint, float thickness = 1.0f)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<LinesPaintCmd>() + Unsafe.SizeOf<Vector2>() * 2);

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new LinesPaintCmd
            {
                CmdType = PaintCmdType.Lines,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                Thickness = (Half)thickness,
                PaintMode = LinePaintMode.List,
                LineCount = 1
            });

            Unsafe.WriteUnaligned(ref memory.DangerousGetReferenceAt(Unsafe.SizeOf<LinesPaintCmd>()), from);
            Unsafe.WriteUnaligned(ref memory.DangerousGetReferenceAt(Unsafe.SizeOf<LinesPaintCmd>() + Unsafe.SizeOf<Vector2>()), to);
        }

        public readonly void AddLines(ReadOnlySpan<Vector2> points, Paint paint, LinePaintMode paintMode = LinePaintMode.List, float thickness = 1.0f)
        {
            if (points.Length < 2)
                return;

            if (paintMode == LinePaintMode.List)
            {
                if (points.Length % 2 != 0)
                    points = points[..(points.Length - 1)];
            }

            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<LinesPaintCmd>() + points.Length);

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new LinesPaintCmd
            {
                CmdType = PaintCmdType.Lines,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                Thickness = (Half)thickness,
                PaintMode = LinePaintMode.List,
                LineCount = 1
            });

            points.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, Vector2>(ref memory.DangerousGetReferenceAt(Unsafe.SizeOf<LinesPaintCmd>())), points.Length));
        }

        public readonly void AddRectangle(Boundaries boundaries, Paint paint)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<RectanglePaintCmd>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new RectanglePaintCmd
            {
                CmdType = PaintCmdType.Rectangle,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                Rect = boundaries,
                CornerRadiusTL = Half.NegativeOne
            });
        }

        public readonly void AddRectangle(Boundaries boundaries, Paint paint, Vector4 cornerRadius)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<RectanglePaintCmd>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new RectanglePaintCmd
            {
                CmdType = PaintCmdType.Rectangle,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                Rect = boundaries,
                CornerRadiusTL = (Half)cornerRadius.X,
                CornerRadiusTR = (Half)cornerRadius.Y,
                CornerRadiusBL = (Half)cornerRadius.Z,
                CornerRadiusBR = (Half)cornerRadius.W,
            });
        }

        public readonly void AddImage(Boundaries boundaries, TextureAsset image, Boundaries uvs, Paint paint)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<ImagePaintCmd>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new ImagePaintCmd
            {
                CmdType = PaintCmdType.Image,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                Rect = boundaries,

                ImageIndex = _data.GetObjectIndex(image),
                UVs = uvs
            });
        }

        public readonly void AddImage(Boundaries boundaries, RHITexture image, Boundaries uvs, Paint paint)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<ImagePaintCmd>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new ImagePaintCmd
            {
                CmdType = PaintCmdType.Image,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                Rect = boundaries,

                ImageIndex = _data.GetObjectIndex(image),
                UVs = uvs
            });
        }

        public readonly void AddImage(Boundaries boundaries, Sprite image, Paint paint)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<ImagePaintCmd>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new ImagePaintCmd
            {
                CmdType = PaintCmdType.Image,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                Rect = boundaries,

                ImageIndex = _data.GetObjectIndex(image.Texture),
                UVs = new Boundaries(image.UVMin, image.UVMax)
            });
        }

        public readonly void AddCircle(Vector2 center, float radius, Paint paint)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<CirclePaintCmd>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new CirclePaintCmd
            {
                CmdType = PaintCmdType.Circle,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                Radius = (Half)radius,
                Center = center
            });
        }

        public readonly void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Paint paint, float cornerRadius = -1.0f)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<CirclePaintCmd>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new TrianglePaintCmd
            {
                CmdType = PaintCmdType.Triangle,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                CornerRadius = float.IsNegative(cornerRadius) ? Half.NegativeOne : (Half)cornerRadius,
                A = a,
                B = b,
                C = c
            });
        }

        public readonly void AddText(Vector2 position, ReadOnlySpan<char> text, FontFamily fontFamily, Paint paint, TextBuilder builder)
        {
            if (text.IsEmpty)
                return;

            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<TextPaintCmd>() + text.Length);

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new TextPaintCmd
            {
                CmdType = PaintCmdType.Text,

                Paint = BuiltPaint.Build(in paint, _gradientManager),
                TextBuilder = BuiltTextBuilder.Build(in builder),
                FontFamilyIndex = (ushort)_data.GetObjectIndex(fontFamily),
                Position = position,
                TextLength = text.Length
            });

            text.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, char>(ref memory.DangerousGetReferenceAt(Unsafe.SizeOf<TextPaintCmd>())), text.Length));
        }

        public void PushClippingRect(Rect rect)
        {
            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<PushClipRectCmd>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), new PushClipRectCmd
            {
                CmdType = PaintCmdType.PushClipRect,
                Rect = rect
            });

            ++_clipStackSize;
        }

        public void PopClippingRect(Rect rect)
        {
            if (_clipStackSize == 0)
                return;

            Span<byte> memory = _data.AllocateSpace(Unsafe.SizeOf<PaintCmdType>());

            Unsafe.WriteUnaligned(ref memory.DangerousGetReference(), PaintCmdType.PopClipRect);

            --_clipStackSize;
        }
    }

    public enum LinePaintMode : byte
    {
        /// <summary>v0 -> v1; v2 -> v3; v4 -> v5; ...</summary>
        List = 0,
        /// <summary>v0 -> v1; v1 -> v2; v2 -> v3; ...</summary>
        Strip
    }
}
