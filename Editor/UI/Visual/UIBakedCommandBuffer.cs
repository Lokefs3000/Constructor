using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Helpers;
using Editor.UI.Text;
using Primary.Assets;
using Primary.Common;
using Primary.Common.Native;
using Primary.Profiling;
using Primary.Timing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Visual
{
    public sealed class UIBakedCommandBuffer : IDisposable
    {
        private List<UIDrawSection> _sections;

        private UnsafeList<UIDrawVertex> _vertices;
        private UnsafeList<ushort> _indices;

        private TemporyAllocator _dataAllocator;

        private bool _disposedValue;

        internal UIBakedCommandBuffer()
        {
            _sections = new List<UIDrawSection>();

            _vertices = new UnsafeList<UIDrawVertex>(8 * 4);
            _indices = new UnsafeList<ushort>(8 * 6);

            _dataAllocator = new TemporyAllocator(1024);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _dataAllocator.Dispose();
                }

                _vertices.Dispose();
                _indices.Dispose();

                _disposedValue = true;
            }
        }

        ~UIBakedCommandBuffer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearInternalData()
        {
            _sections.Clear();

            _vertices.Clear();
            _indices.Clear();

            _dataAllocator.Reset();
        }

        internal unsafe void Bake(UIRenderer renderer, UICommandBuffer commandBuffer)
        {
            if (commandBuffer.Commands.Count == 0)
                return;

            ClearInternalData();

            using RentedArray<UIDrawCommand> commands = RentedArray<UIDrawCommand>.Rent(commandBuffer.Commands.Count + 1);
            commandBuffer.CopyCommandsTo(commands.Span);

            commands[commandBuffer.Commands.Count] = new UIDrawCommand
            {
                Type = unchecked((UIDrawType)(-1)),
                CommandId = ushort.MaxValue,
                ZIndex = ushort.MaxValue
            };

            using (new ProfilingScope("Sort"))
            {
                //assemble all commands into z order
                //TODO: verify id current method already keeps commands in proper z order
                commands.Span.Sort(DrawCommandComparer.Default);
            }

            Vector2 regionSize = commandBuffer.DrawBoundaries.Size;

            int indexOffset = 0;
            int baseVertex = 0;

            int index = 0;
            using (new ProfilingScope("Polygon"))
            {
                while (index < commands.Count)
                {
                    ref UIDrawCommand command = ref commands.DangerousGetReferenceAt(index);
                    for (int j = index; j < commands.Count;)
                    {
                        ref UIDrawCommand subCommand = ref commands.DangerousGetReferenceAt(j);
                        if (!command.Equals(subCommand))
                        {
                            index = j;

                            if (_indices.Count > indexOffset)
                            {
                                object? aux = null;
                                switch (command.Type)
                                {
                                    case UIDrawType.SimpleText:
                                        {
                                            aux = (UIFontStyle)commandBuffer.Indexer.Get(command.SimpleText.FontStyleIndex)!;
                                            break;
                                        }
                                }

                                _sections.Add(new UIDrawSection(command.Type, _indices.Count - indexOffset, indexOffset, baseVertex, aux));
                            }

                            indexOffset = _indices.Count;
                            //baseVertex = _vertices.Count;

                            break;
                        }

                        switch (subCommand.Type)
                        {
                            case UIDrawType.Rectangle: CmdRectangle(ref subCommand); break;
                            case UIDrawType.Triangle: CmdTriangle(ref subCommand); break;
                            case UIDrawType.Circle: CmdCircle(ref subCommand); break;

                            case UIDrawType.SimpleText: CmdSimpleText(ref subCommand); break;
                        }

                        index = ++j;
                    }
                }
            }

            void CmdRectangle(ref UIDrawCommand cmd)
            {
                ref UIDrawRectangle rect = ref cmd.Rectangle;

                int dataOffset = _dataAllocator.ByteOffset;

                {
                    Vector2 drawSize = rect.DrawBounds.Size;
                    Vector2 aspectSize = drawSize.X < drawSize.Y ?
                        new Vector2(1.0f, drawSize.Y / drawSize.X) :
                        new Vector2(drawSize.X / drawSize.Y, 1.0f);

                    Vector4 rounding = Vector4.Zero;

                    if (Flags.HasFlag(rect.CornersToRound, UIRoundedCorner.TopLeft))
                        rounding.X = rect.Rounding;
                    if (Flags.HasFlag(rect.CornersToRound, UIRoundedCorner.TopRight))
                        rounding.Y = rect.Rounding;
                    if (Flags.HasFlag(rect.CornersToRound, UIRoundedCorner.BottomLeft))
                        rounding.Z = rect.Rounding;
                    if (Flags.HasFlag(rect.CornersToRound, UIRoundedCorner.BottomRight))
                        rounding.W = rect.Rounding;

                    RectMetadata metadata = new RectMetadata(
                        new Vector4(drawSize, 0.0f, 0.0f),
                        drawSize,
                        rounding);

                    *(RectMetadata*)_dataAllocator.Allocate(Unsafe.SizeOf<RectMetadata>()) = metadata;
                }

                Vector2 min = rect.DrawBounds.Minimum;
                Vector2 max = rect.DrawBounds.Maximum;

                UIDrawVertex v0 = new UIDrawVertex(new Vector2(min.X, min.Y), new Vector2(-1.0f, -1.0f), default, (uint)dataOffset);
                UIDrawVertex v1 = new UIDrawVertex(new Vector2(max.X, min.Y), new Vector2(1.0f, -1.0f), default, (uint)dataOffset);
                UIDrawVertex v2 = new UIDrawVertex(new Vector2(min.X, max.Y), new Vector2(-1.0f, 1.0f), default, (uint)dataOffset);
                UIDrawVertex v3 = new UIDrawVertex(new Vector2(max.X, max.Y), new Vector2(1.0f, 1.0f), default, (uint)dataOffset);

                if (rect.Color.Type == UIColorType.Gradient)
                {
                    Boundaries gradient = renderer.GradientManager.GetGradientUVs(rect.Color.GradientKey);

                    v0.Color = new Color(gradient.Minimum.X, gradient.Minimum.Y, 0.0f, -1.0f);
                    v1.Color = new Color(gradient.Maximum.X, gradient.Minimum.Y, 0.0f, -1.0f);
                    v2.Color = new Color(gradient.Minimum.X, gradient.Maximum.Y, 0.0f, -1.0f);
                    v3.Color = new Color(gradient.Maximum.X, gradient.Maximum.Y, 0.0f, -1.0f);
                }
                else
                {
                    v0.Color = rect.Color.RGBA;
                    v1.Color = rect.Color.RGBA;
                    v2.Color = rect.Color.RGBA;
                    v3.Color = rect.Color.RGBA;
                }

                int start = baseVertex + _vertices.Count;

                _vertices.Add(v0);
                _vertices.Add(v1);
                _vertices.Add(v2);
                _vertices.Add(v3);

                _indices.Add((ushort)start);
                _indices.Add((ushort)(start + 1));
                _indices.Add((ushort)(start + 2));
                _indices.Add((ushort)(start + 1));
                _indices.Add((ushort)(start + 3));
                _indices.Add((ushort)(start + 2));
            }
            void CmdTriangle(ref UIDrawCommand cmd)
            {
                ref UIDrawTriangle tri = ref cmd.Triangle;

                int dataOffset = _dataAllocator.ByteOffset;

                Vector2 topLeft = Vector2.Min(Vector2.Min(tri.A, tri.B), tri.C);
                Vector2 bottomRight = Vector2.Max(Vector2.Max(tri.A, tri.B), tri.C);

                {
                    Vector2 sub = topLeft;

                    TriangleMetadata metadata = new TriangleMetadata(
                        tri.A - sub,
                        tri.B - sub,
                        tri.C - sub,
                        tri.Rounding);

                    *(TriangleMetadata*)_dataAllocator.Allocate(Unsafe.SizeOf<TriangleMetadata>()) = metadata;
                }

                if (tri.Rounding > 0.0f)
                {
                    Vector2 size = bottomRight - topLeft + new Vector2(tri.Rounding);
                    size += new Vector2(tri.Rounding);

                    UIDrawVertex v0 = new UIDrawVertex(new Vector2(topLeft.X, topLeft.Y), new Vector2(-tri.Rounding, -tri.Rounding), default, (uint)dataOffset);
                    UIDrawVertex v1 = new UIDrawVertex(new Vector2(bottomRight.X, topLeft.Y), new Vector2(size.X, -tri.Rounding), default, (uint)dataOffset);
                    UIDrawVertex v2 = new UIDrawVertex(new Vector2(topLeft.X, bottomRight.Y), new Vector2(-tri.Rounding, size.Y), default, (uint)dataOffset);
                    UIDrawVertex v3 = new UIDrawVertex(new Vector2(bottomRight.X, bottomRight.Y), new Vector2(size.X, size.Y), default, (uint)dataOffset);

                    if (tri.Color.Type == UIColorType.Gradient)
                    {
                        Boundaries gradient = renderer.GradientManager.GetGradientUVs(tri.Color.GradientKey);

                        v0.Color = new Color(gradient.Minimum.X, gradient.Minimum.Y, 0.0f, -1.0f);
                        v1.Color = new Color(gradient.Maximum.X, gradient.Minimum.Y, 0.0f, -1.0f);
                        v2.Color = new Color(gradient.Minimum.X, gradient.Maximum.Y, 0.0f, -1.0f);
                        v3.Color = new Color(gradient.Maximum.X, gradient.Maximum.Y, 0.0f, -1.0f);
                    }
                    else
                    {
                        v0.Color = tri.Color.RGBA;
                        v1.Color = tri.Color.RGBA;
                        v2.Color = tri.Color.RGBA;
                        v3.Color = tri.Color.RGBA;
                    }

                    int start = baseVertex + _vertices.Count;

                    _vertices.Add(v0);
                    _vertices.Add(v1);
                    _vertices.Add(v2);
                    _vertices.Add(v3);

                    _indices.Add((ushort)start);
                    _indices.Add((ushort)(start + 1));
                    _indices.Add((ushort)(start + 2));
                    _indices.Add((ushort)(start + 1));
                    _indices.Add((ushort)(start + 3));
                    _indices.Add((ushort)(start + 2));
                }
                else
                {
                    UIDrawVertex v0 = new UIDrawVertex(tri.A, new Vector2(-1.0f, -1.0f), default, (uint)dataOffset);
                    UIDrawVertex v1 = new UIDrawVertex(tri.B, new Vector2(0.0f, 1.0f), default, (uint)dataOffset);
                    UIDrawVertex v2 = new UIDrawVertex(tri.C, new Vector2(1.0f, -1.0f), default, (uint)dataOffset);

                    if (tri.Color.Type == UIColorType.Gradient)
                    {
                        Boundaries gradient = renderer.GradientManager.GetGradientUVs(tri.Color.GradientKey);

                        v0.Color = new Color(gradient.Minimum.X, gradient.Minimum.Y, 0.0f, -1.0f);
                        v1.Color = new Color(float.Lerp(gradient.Minimum.X, gradient.Maximum.X, 0.5f), gradient.Maximum.Y, 0.0f, -1.0f);
                        v2.Color = new Color(gradient.Maximum.X, gradient.Minimum.Y, 0.0f, -1.0f);
                    }
                    else
                    {
                        v0.Color = tri.Color.RGBA;
                        v1.Color = tri.Color.RGBA;
                        v2.Color = tri.Color.RGBA;
                    }

                    int start = baseVertex + _vertices.Count;

                    _vertices.Add(v0);
                    _vertices.Add(v1);
                    _vertices.Add(v2);

                    _indices.Add((ushort)start);
                    _indices.Add((ushort)(start + 1));
                    _indices.Add((ushort)(start + 2));
                }
            }
            void CmdCircle(ref UIDrawCommand cmd)
            {
                ref UIDrawCircle circ = ref cmd.Circle;

                int dataOffset = _dataAllocator.ByteOffset;

                {
                    CircleMetadata metadata = new CircleMetadata(
                        circ.InfillRadius);

                    *(CircleMetadata*)_dataAllocator.Allocate(Unsafe.SizeOf<CircleMetadata>()) = metadata;
                }

                Vector2 min = circ.Center - new Vector2(circ.Radius);
                Vector2 max = circ.Center + new Vector2(circ.Radius);

                UIDrawVertex v0 = new UIDrawVertex(new Vector2(min.X, min.Y), new Vector2(-1.0f, -1.0f), default, (uint)dataOffset);
                UIDrawVertex v1 = new UIDrawVertex(new Vector2(max.X, min.Y), new Vector2(1.0f, -1.0f), default, (uint)dataOffset);
                UIDrawVertex v2 = new UIDrawVertex(new Vector2(min.X, max.Y), new Vector2(-1.0f, 1.0f), default, (uint)dataOffset);
                UIDrawVertex v3 = new UIDrawVertex(new Vector2(max.X, max.Y), new Vector2(1.0f, 1.0f), default, (uint)dataOffset);

                if (circ.Color.Type == UIColorType.Gradient)
                {
                    Boundaries gradient = renderer.GradientManager.GetGradientUVs(circ.Color.GradientKey);

                    v0.Color = new Color(gradient.Minimum.X, gradient.Minimum.Y, 0.0f, -1.0f);
                    v1.Color = new Color(gradient.Maximum.X, gradient.Minimum.Y, 0.0f, -1.0f);
                    v2.Color = new Color(gradient.Minimum.X, gradient.Maximum.Y, 0.0f, -1.0f);
                    v3.Color = new Color(gradient.Maximum.X, gradient.Maximum.Y, 0.0f, -1.0f);
                }
                else
                {
                    v0.Color = circ.Color.RGBA;
                    v1.Color = circ.Color.RGBA;
                    v2.Color = circ.Color.RGBA;
                    v3.Color = circ.Color.RGBA;
                }

                int start = baseVertex + _vertices.Count;

                _vertices.Add(v0);
                _vertices.Add(v1);
                _vertices.Add(v2);
                _vertices.Add(v3);

                _indices.Add((ushort)start);
                _indices.Add((ushort)(start + 1));
                _indices.Add((ushort)(start + 2));
                _indices.Add((ushort)(start + 1));
                _indices.Add((ushort)(start + 3));
                _indices.Add((ushort)(start + 2));
            }

            void CmdSimpleText(ref UIDrawCommand cmd)
            {
                ref UIDrawSimpleText text = ref cmd.SimpleText;

                UIFontStyle fontStyle = (UIFontStyle)commandBuffer.Indexer.Get(text.FontStyleIndex)!;
                ReadOnlySpan<char> letters = text.Text.String;

                Vector2 spaceTabAdvance = new Vector2(fontStyle.SpaceAdvance, fontStyle.TabAdvance) * text.TextScale;
                Color color = text.Color.Type == UIColorType.Solid ? text.Color.RGBA : Color.TransparentBlack;

                int lastLetterIndex = letters.Length - 1;
                int startVertexCount = _vertices.Count;

                Vector2 basePosition = text.Position;

                for (int i = 0; i < letters.Length; ++i)
                {
                    char c = letters.DangerousGetReferenceAt(i);
                    switch (c)
                    {
                        case ' ': basePosition.X += spaceTabAdvance.X; break;
                        case '\t': basePosition.X += spaceTabAdvance.Y; break;
                        default:
                            {
                                UIGlyph glyph = fontStyle.RequestGlyph(c);

                                Vector2 size = glyph.Size * text.TextScale;
                                Vector2 offset = glyph.Offset * text.TextScale;

                                Vector2 min = basePosition + offset;
                                Vector2 max = min + size;

                                UIDrawVertex v0 = new UIDrawVertex(new Vector2(min.X, min.Y), new Vector2(glyph.AtlasUVs.Minimum.X, glyph.AtlasUVs.Minimum.Y), color);
                                UIDrawVertex v1 = new UIDrawVertex(new Vector2(max.X, min.Y), new Vector2(glyph.AtlasUVs.Maximum.X, glyph.AtlasUVs.Minimum.Y), color);
                                UIDrawVertex v2 = new UIDrawVertex(new Vector2(min.X, max.Y), new Vector2(glyph.AtlasUVs.Minimum.X, glyph.AtlasUVs.Maximum.Y), color);
                                UIDrawVertex v3 = new UIDrawVertex(new Vector2(max.X, max.Y), new Vector2(glyph.AtlasUVs.Maximum.X, glyph.AtlasUVs.Maximum.Y), color);

                                int start = baseVertex + _vertices.Count;

                                _vertices.Add(v0);
                                _vertices.Add(v1);
                                _vertices.Add(v2);
                                _vertices.Add(v3);

                                _indices.Add((ushort)start);
                                _indices.Add((ushort)(start + 1));
                                _indices.Add((ushort)(start + 2));
                                _indices.Add((ushort)(start + 1));
                                _indices.Add((ushort)(start + 3));
                                _indices.Add((ushort)(start + 2));

                                basePosition.X += i == lastLetterIndex ? size.X : glyph.Advance * text.TextScale;

                                break;
                            }
                    }
                }

                if (text.Color.Type == UIColorType.Gradient)
                {
                    Boundaries gradient = renderer.GradientManager.GetGradientUVs(text.Color.GradientKey);

                    Vector2 mul = Vector2.One / new Vector2(basePosition.X - text.Position.X);
                    for (int i = startVertexCount; i < _vertices.Count; i += 4)
                    {
                        ref UIDrawVertex v0 = ref _vertices[i];
                        ref UIDrawVertex v1 = ref _vertices[i + 1];
                        ref UIDrawVertex v2 = ref _vertices[i + 2];
                        ref UIDrawVertex v3 = ref _vertices[i + 3];

                        Vector2 lerpX = Vector2.Lerp(
                            new Vector2(gradient.Minimum.X),
                            new Vector2(gradient.Minimum.X),
                            (new Vector2(v0.Position.X, v1.Position.X) - text.Position) * mul);

                        v0.Color = new Color(lerpX.X, gradient.Minimum.Y, 0.0f, -1.0f);
                        v1.Color = new Color(lerpX.Y, gradient.Minimum.Y, 0.0f, -1.0f);
                        v2.Color = new Color(lerpX.X, gradient.Maximum.Y, 0.0f, -1.0f);
                        v3.Color = new Color(lerpX.Y, gradient.Maximum.Y, 0.0f, -1.0f);
                    }
                }
            }
        }

        internal void CopyInternalsTo(UIBakedCommandBuffer commandBuffer)
        {
            commandBuffer.ClearInternalData();

            commandBuffer._sections.AddRange(_sections);

            //commandBuffer._vertices.AddRange(_vertices.AsSpan());
            //commandBuffer._indices.AddRange(_indices.AsSpan());

            _dataAllocator.CopyTo(commandBuffer._dataAllocator);
        }

        public ReadOnlySpan<UIDrawSection> Sections => _sections.AsSpan();

        public ReadOnlySpan<UIDrawVertex> Vertices => _vertices.AsSpan();
        public ReadOnlySpan<ushort> Indices => _indices.AsSpan();

        public ReadOnlySpan<byte> Metadata => _dataAllocator.AsSpan();

        //value-types get boxed in sort methods
        internal class DrawCommandComparer : IComparer<UIDrawCommand>
        {
            public int Compare(UIDrawCommand x, UIDrawCommand y)
            {
                int r = x.ZIndex.CompareTo(y.ZIndex);
                if (r == 0)
                    return x.CommandId.CompareTo(y.CommandId);
                return r;
            }

            public static readonly DrawCommandComparer Default = new DrawCommandComparer();
        }

        private record struct RectMetadata(Vector4 UVTransform, Vector2 BoxSize, Vector4 Rounding);
        private record struct TriangleMetadata(Vector2 A, Vector2 B, Vector2 C, float Rounding);
        private record struct CircleMetadata(float InfillRadius);
    }

    public readonly record struct UIDrawSection(UIDrawType Type, int IndexCount, int IndexOffset, int BaseVertex, object? Aux) : IEquatable<UIDrawSection>
    {
        public bool Equals(UIDrawSection other) => Type == other.Type && Aux == other.Aux;

        public override int GetHashCode() => base.GetHashCode();
    }
    public record struct UIDrawVertex(Vector2 Position, Vector2 UV, Color Color, uint MetadataOffset = 0);
}
