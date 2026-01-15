using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using Editor.UI.Datatypes;
using Editor.UI.Memory;
using Primary.Common;
using Primary.Profiling;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

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

            _sections.Clear();
            _vertices.Clear();
            _indices.Clear();

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
            while (index < commands.Count)
            {
                ref UIDrawCommand command = ref commands.DangerousGetReferenceAt(index);
                for (int j = index; j < commands.Count;)
                {
                    ref UIDrawCommand subCommand = ref commands.DangerousGetReferenceAt(j);
                    if (command.Type != subCommand.Type)
                    {
                        index = j;

                        if (_indices.Count > indexOffset)
                            _sections.Add(new UIDrawSection(command.Type, _indices.Count - indexOffset, indexOffset, baseVertex));

                        indexOffset = _indices.Count;
                        baseVertex = _vertices.Count;

                        break;
                    }

                    switch (subCommand.Type)
                    {
                        case UIDrawType.Rectangle:
                            {
                                UIDrawRectangle rectangle = subCommand.Rectangle;
                                rectangle.RoundingPerc *= 2.0f;

                                Vector2 size = rectangle.DrawBounds.Size;
                                float pixSizeY = size.Y;

                                if (size.X > size.Y)
                                    size = new Vector2(1.0f, size.Y / size.X);
                                else
                                    size = new Vector2(size.X / size.Y, 1.0f);

                                uint offset = (uint)_dataAllocator.ByteOffset;
                                (*(RectMetadata*)_dataAllocator.Allocate(Unsafe.SizeOf<RectMetadata>())) = new RectMetadata(new Vector4(size.X, size.Y, 0.0f, 0.0f), rectangle.RoundingPerc > 0.0f ? 1u : 0, size, new Vector4(rectangle.RoundingPerc), pixSizeY, 1.0f);

                                UIDrawVertex v0 = new UIDrawVertex(new Vector2(rectangle.DrawBounds.Minimum.X, rectangle.DrawBounds.Minimum.Y), new Vector2(-1.0f, 1.0f), default, offset);
                                UIDrawVertex v1 = new UIDrawVertex(new Vector2(rectangle.DrawBounds.Maximum.X, rectangle.DrawBounds.Minimum.Y), new Vector2(1.0f, 1.0f), default, offset);
                                UIDrawVertex v2 = new UIDrawVertex(new Vector2(rectangle.DrawBounds.Minimum.X, rectangle.DrawBounds.Maximum.Y), new Vector2(-1.0f, -1.0f), default, offset);
                                UIDrawVertex v3 = new UIDrawVertex(new Vector2(rectangle.DrawBounds.Maximum.X, rectangle.DrawBounds.Maximum.Y), new Vector2(1.0f, -1.0f), default, offset);

                                if (rectangle.Color.Type == UIColorType.Solid)
                                {
                                    v0.Color = rectangle.Color.RGBA;
                                    v1.Color = rectangle.Color.RGBA;
                                    v2.Color = rectangle.Color.RGBA;
                                    v3.Color = rectangle.Color.RGBA;
                                }
                                else
                                {
                                    Boundaries uvs = renderer.GradientManager.GetGradientUVs(rectangle.Color.GradientKey);
                                    v0.Color = new Color(uvs.Minimum.X, uvs.Minimum.Y, 0.0f, -1.0f);
                                    v1.Color = new Color(uvs.Maximum.X, uvs.Minimum.Y, 0.0f, -1.0f);
                                    v2.Color = new Color(uvs.Minimum.X, uvs.Maximum.Y, 0.0f, -1.0f);
                                    v3.Color = new Color(uvs.Maximum.X, uvs.Maximum.Y, 0.0f, -1.0f);
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

                                break;
                            }
                    }

                    index = ++j;
                }
            }
        }

        public ReadOnlySpan<UIDrawSection> Sections => _sections.AsSpan();

        public ReadOnlySpan<UIDrawVertex> Vertices => _vertices.AsSpan();
        public ReadOnlySpan<ushort> Indices => _indices.AsSpan();

        public ReadOnlySpan<byte> Metadata => _dataAllocator.AsSpan();

        //value-types get boxed in sort methods
        private class DrawCommandComparer : IComparer<UIDrawCommand>
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

        private record struct RectMetadata(Vector4 UVTransform, uint IsRounded, Vector2 BoxSize, Vector4 Rounding, float PixelHeight, float Infill);
    }

    public readonly record struct UIDrawSection(UIDrawType Type, int IndexCount, int IndexOffset, int BaseVertex);
    public record struct UIDrawVertex(Vector2 Position, Vector2 UV, Color Color, uint MetadataOffset);
}
