using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Built;
using EditorUI.Visual.Draw;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Common.Memory;
using Primary.Mathematics;
using Primary.Windowing;

namespace EditorUI.Visual.Built
{
    public sealed class BuiltPaintData : IDisposable
    {
        private PainterData? _data;
        private Window? _window;

        private readonly MeshBuilder _meshBuilder;
        private readonly LinearResizingAllocator _dataAllocator;
        private List<PaintSegment> _segments;

        private Stack<Rect> _clipStack;

        private bool _disposedValue;

        internal BuiltPaintData()
        {
            _data = null;
            _window = null;

            _meshBuilder = new MeshBuilder();
            _dataAllocator = new LinearResizingAllocator(4096);
            _segments = new List<PaintSegment>();

            _clipStack = new Stack<Rect>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _dataAllocator.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearInternalData()
        {
            _data = null;

            _meshBuilder.Clear();
            _dataAllocator.Reset();
            _segments.Clear();

            _clipStack.Clear();
        }

        internal void BuildRenderingData(Window window, PainterData data)
        {
            _data = data;
            _window = window;

            ReadOnlySpan<byte> memory = _data.Memory;
            ROList<object> objects = _data.Objects;

            Rect? clip = null;

            int currentOffset = 0;
            uint commandIndex = 0;

            object? argument = null;

            while (currentOffset < memory.Length)
            {
                PaintCmdType cmdType = Unsafe.ReadUnaligned<PaintCmdType>(ref memory.DangerousGetReferenceAt(currentOffset));

                switch (cmdType)
                {
                    case PaintCmdType.PushClipRect:
                        {
                            clip = PushClippingRect(Unsafe.ReadUnaligned<PushClipRectCmd>(ref memory.DangerousGetReferenceAt(currentOffset)).Rect);
                            currentOffset += Unsafe.SizeOf<PushClipRectCmd>();
                            continue;
                        }
                    case PaintCmdType.PopClipRect:
                        {
                            clip = PopClippingRect();
                            currentOffset += Unsafe.SizeOf<PaintCmdType>();
                            continue;
                        }
                    case PaintCmdType.Text:
                        {
                            argument = _data.Objects[Unsafe.ReadUnaligned<TextPaintCmd>(ref memory.DangerousGetReferenceAt(currentOffset)).FontFamilyIndex];
                            break;
                        }
                    case PaintCmdType.Image:
                        {
                            argument = _data.Objects[Unsafe.ReadUnaligned<ImagePaintCmd>(ref memory.DangerousGetReferenceAt(currentOffset)).ImageIndex];
                            if (argument is TextureAsset texture)
                            {
                                if (texture.Status == ResourceStatus.Pending || texture.Status == ResourceStatus.Running)
                                    argument = AssetManager.Static.DebugTexLoading;
                                else if (texture.Status == ResourceStatus.Error)
                                    argument = AssetManager.Static.DebugTexError;
                            }

                            break;
                        }
                }

                bool breakCurrentLoop = false;
                bool isContinous = true;

                int localOffset = currentOffset;
                uint localCommandIndex = commandIndex;

                int indexCount = _meshBuilder.Indices.Count;

                while (localOffset < memory.Length && !breakCurrentLoop)
                {
                    PaintCmdType localCmdType = Unsafe.ReadUnaligned<PaintCmdType>(ref memory.DangerousGetReferenceAt(localOffset));
                    bool isMatchingCmd = localCmdType == cmdType;

                    if (isMatchingCmd)
                    {
                        switch (cmdType)
                        {
                            case PaintCmdType.Text:
                                {
                                    if (argument != _data.Objects[Unsafe.ReadUnaligned<TextPaintCmd>(ref memory.DangerousGetReferenceAt(localOffset)).FontFamilyIndex])
                                    {
                                        isMatchingCmd = false;
                                    }

                                    break;
                                }
                            case PaintCmdType.Image:
                                {
                                    if (argument != _data.Objects[Unsafe.ReadUnaligned<ImagePaintCmd>(ref memory.DangerousGetReferenceAt(localOffset)).ImageIndex])
                                    {
                                        isMatchingCmd = false;
                                    }

                                    break;
                                }
                        }
                    }

                    if (isContinous && !isMatchingCmd)
                    {
                        currentOffset = localOffset;
                        commandIndex = localCommandIndex;

                        isContinous = false;
                    }

                    switch (cmdType)
                    {
                        case PaintCmdType.Points:
                            {
                                PointsPaintCmd cmd = Unsafe.ReadUnaligned<PointsPaintCmd>(ref memory.DangerousGetReferenceAt(localOffset));
                                if (isMatchingCmd)
                                {
                                    ReadOnlySpan<Vector2> points = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, Vector2>(ref memory.DangerousGetReferenceAt(localOffset + Unsafe.SizeOf<PointsPaintCmd>())), cmd.PointCount);
                                    uint dataOffset = GenerateDataFor(ref cmd);

                                    _meshBuilder.AddPoints(points, (float)cmd.Radius, ref cmd.Paint, localCommandIndex, dataOffset);
                                }

                                localOffset += Unsafe.SizeOf<PointsPaintCmd>() + cmd.PointCount * Unsafe.SizeOf<Vector2>();
                                break;
                            }
                        case PaintCmdType.Lines:
                            {
                                LinesPaintCmd cmd = Unsafe.ReadUnaligned<LinesPaintCmd>(ref memory.DangerousGetReferenceAt(localOffset));
                                if (isMatchingCmd)
                                {
                                    ReadOnlySpan<Vector2> points = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, Vector2>(ref memory.DangerousGetReferenceAt(localOffset + Unsafe.SizeOf<LinesPaintCmd>())), cmd.LineCount);
                                    uint dataOffset = GenerateDataFor(ref cmd);

                                    _meshBuilder.AddLines(points, (float)cmd.Thickness, ref cmd.Paint, cmd.PaintMode, localCommandIndex, dataOffset);
                                }

                                localOffset += Unsafe.SizeOf<LinesPaintCmd>() + cmd.LineCount * Unsafe.SizeOf<Vector2>();
                                break;
                            }
                        case PaintCmdType.Rectangle:
                            {
                                RectanglePaintCmd cmd = Unsafe.ReadUnaligned<RectanglePaintCmd>(ref memory.DangerousGetReferenceAt(localOffset));
                                if (isMatchingCmd)
                                {
                                    uint dataOffset = GenerateDataFor(ref cmd);
                                    _meshBuilder.AddRectangle(cmd.Rect, ref cmd.Paint, localCommandIndex, dataOffset);
                                }

                                localOffset += Unsafe.SizeOf<RectanglePaintCmd>();
                                break;
                            }
                        case PaintCmdType.Circle:
                            {
                                CirclePaintCmd cmd = Unsafe.ReadUnaligned<CirclePaintCmd>(ref memory.DangerousGetReferenceAt(localOffset));
                                if (isMatchingCmd)
                                {
                                    uint dataOffset = GenerateDataFor(ref cmd);
                                    _meshBuilder.AddCircle(cmd.Center, (float)cmd.Radius, ref cmd.Paint, localCommandIndex, dataOffset);
                                }

                                localOffset += Unsafe.SizeOf<CirclePaintCmd>();
                                break;
                            }
                        case PaintCmdType.Triangle:
                            {
                                TrianglePaintCmd cmd = Unsafe.ReadUnaligned<TrianglePaintCmd>(ref memory.DangerousGetReferenceAt(localOffset));
                                if (isMatchingCmd)
                                {
                                    uint dataOffset = GenerateDataFor(ref cmd);
                                    _meshBuilder.AddTriangle(cmd.A, cmd.B, cmd.C, ref cmd.Paint, localCommandIndex, dataOffset);
                                }

                                localOffset += Unsafe.SizeOf<TrianglePaintCmd>();
                                break;
                            }
                        case PaintCmdType.Text:
                            {
                                TextPaintCmd cmd = Unsafe.ReadUnaligned<TextPaintCmd>(ref memory.DangerousGetReferenceAt(localOffset));
                                if (isMatchingCmd)
                                {
                                    
                                }

                                localOffset += Unsafe.SizeOf<TextPaintCmd>() + cmd.TextLength * 2;
                                break;
                            }
                        case PaintCmdType.PushClipRect:
                        case PaintCmdType.PopClipRect: breakCurrentLoop = true; continue;
                    }

                    ++localCommandIndex;
                }

                if (isContinous)
                {
                    currentOffset = localOffset;
                    commandIndex = localCommandIndex;
                }

                _segments.Add(new PaintSegment(cmdType, new IndexRange(indexCount, _meshBuilder.Indices.Count), argument, clip));
            }
        }

        private Rect PushClippingRect(Rect rect)
        {
            _clipStack.Push(rect);
            return rect;
        }

        private Rect? PopClippingRect()
        {
            _clipStack.Pop();
            return _clipStack.TryPeek(out Rect previousClip) ? previousClip : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe uint GenerateDataFor(ref PointsPaintCmd cmd)
        {
            uint dataOffset = (uint)_dataAllocator.CurrentOffset;

            Color color = cmd.Paint.Stroke.ColorType == BuiltColorType.Solid ? cmd.Paint.Stroke.Solid : s_gradientColor;
            SharedShaderData shared = new SharedShaderData(cmd.Paint.StrokeWidth, new Vortice.Mathematics.PackedVector.Half4(color.AsVector4()));
            *(PointsShaderData*)_dataAllocator.Allocate(Unsafe.SizeOf<PointsShaderData>()) = new PointsShaderData(shared, cmd.Radius);

            return dataOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe uint GenerateDataFor(ref LinesPaintCmd cmd)
        {
            uint dataOffset = (uint)_dataAllocator.CurrentOffset;

            Color color = cmd.Paint.Stroke.ColorType == BuiltColorType.Solid ? cmd.Paint.Stroke.Solid : s_gradientColor;
            SharedShaderData shared = new SharedShaderData(cmd.Paint.StrokeWidth, new Vortice.Mathematics.PackedVector.Half4(color.AsVector4()));
            *(LinesShaderData*)_dataAllocator.Allocate(Unsafe.SizeOf<LinesShaderData>()) = new LinesShaderData(shared, cmd.Thickness);

            return dataOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe uint GenerateDataFor(ref RectanglePaintCmd cmd)
        {
            uint dataOffset = (uint)_dataAllocator.CurrentOffset;

            Color color = cmd.Paint.Stroke.ColorType == BuiltColorType.Solid ? cmd.Paint.Stroke.Solid : s_gradientColor;
            Vector2 boxSize = cmd.Rect.Size;
            SharedShaderData shared = new SharedShaderData(cmd.Paint.StrokeWidth, new Vortice.Mathematics.PackedVector.Half4(color.AsVector4()));
            *(RectangleShaderData*)_dataAllocator.Allocate(Unsafe.SizeOf<RectangleShaderData>()) = new RectangleShaderData(shared, new Vortice.Mathematics.PackedVector.UShort2(boxSize), cmd.CornerRadiusTL, cmd.CornerRadiusTR, cmd.CornerRadiusBL, cmd.CornerRadiusBR);

            return dataOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe uint GenerateDataFor(ref CirclePaintCmd cmd)
        {
            uint dataOffset = (uint)_dataAllocator.CurrentOffset;

            Color color = cmd.Paint.Stroke.ColorType == BuiltColorType.Solid ? cmd.Paint.Stroke.Solid : s_gradientColor;
            SharedShaderData shared = new SharedShaderData(cmd.Paint.StrokeWidth, new Vortice.Mathematics.PackedVector.Half4(color.AsVector4()));
            *(CircleShaderData*)_dataAllocator.Allocate(Unsafe.SizeOf<CircleShaderData>()) = new CircleShaderData(shared, cmd.Center, cmd.Radius);

            return dataOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe uint GenerateDataFor(ref TrianglePaintCmd cmd)
        {
            uint dataOffset = (uint)_dataAllocator.CurrentOffset;

            Color color = cmd.Paint.Stroke.ColorType == BuiltColorType.Solid ? cmd.Paint.Stroke.Solid : s_gradientColor;
            SharedShaderData shared = new SharedShaderData(cmd.Paint.StrokeWidth, new Vortice.Mathematics.PackedVector.Half4(color.AsVector4()));
            *(TriangleShaderData*)_dataAllocator.Allocate(Unsafe.SizeOf<TriangleShaderData>()) = new TriangleShaderData(shared, cmd.A, cmd.B, cmd.C, cmd.CornerRadius);

            return dataOffset;
        }

        public PainterData? Data => _data;
        public Window? Window => _window;

        public MeshBuilder MeshBuilder => _meshBuilder;

        public ROList<PaintSegment> Segments => _segments;
        public unsafe ReadOnlySpan<byte> DataBuffer => new ReadOnlySpan<byte>(_dataAllocator.Pointer.ToPointer(), _dataAllocator.CurrentOffset);

        private static readonly Color s_gradientColor = new Color(0.0f, -1.0f);
    }
}
