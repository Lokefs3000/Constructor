using CommunityToolkit.HighPerformance;
using Editor.Interop.Ed;
using Editor.UI.Assets;
using Editor.UI.Memory;
using Primary.Common;
using Primary.Pooling;
using Primary.RHI2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI
{
    public sealed class UIFontManager : IDisposable
    {
        private HashSet<UIFontStyle> _pendingFontUpdates;
        private Queue<UIFontUpdate> _pendingUpdates;

        private DisposableObjectPool<DisposableShapedGlyph> _shapedGlyphPool;
        private TemporyAllocator _temporaryAllocator;

        private bool _disposedValue;

        internal UIFontManager()
        {
            _pendingFontUpdates = new HashSet<UIFontStyle>();
            _pendingUpdates = new Queue<UIFontUpdate>();

            _shapedGlyphPool = new DisposableObjectPool<DisposableShapedGlyph>(new DisposableShapedGlyph.Policy());
            _temporaryAllocator = new TemporyAllocator(8192);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    while (_pendingUpdates.TryDequeue(out UIFontUpdate update))
                        update.OldAtlas?.Dispose();

                    _temporaryAllocator.Dispose();
                }

                _shapedGlyphPool.Dispose();

                _disposedValue = true;
            }
        }

        ~UIFontManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void AddFontToUpdateSet(UIFontStyle font)
        {
            _pendingFontUpdates.Add(font);
        }

        internal void RenderPendingFonts()
        {
            if (_pendingFontUpdates.Count > 0)
            {
                foreach (UIFontStyle style in _pendingFontUpdates)
                {
                    RenderNewFontGlyphs(style);
                }

                _pendingFontUpdates.Clear();
            }
        }

        private unsafe void RenderNewFontGlyphs(UIFontStyle style)
        {
            _temporaryAllocator.Reset();

            Queue<char> unrendered = style.UnrenderedGlyphs;
            ConcurrentQueue<RenderedGlyph> rendered = new ConcurrentQueue<RenderedGlyph>();

            style.FontData.SetShapingFontStyle(style);

            {
                using RentedArray<Task> rentedTasks = RentedArray<Task>.Rent(unrendered.Count, true);
                (DisposableShapedGlyph, char)[] shapedGlyphs = new (DisposableShapedGlyph, char)[unrendered.Count];

                int index = 0;
                while (unrendered.TryDequeue(out char glyph))
                {
                    DisposableShapedGlyph sg = _shapedGlyphPool.Get();
                    if (EdInterop.MSDF_ShapeGlyph((MSDF_FontFace*)style.FontData.Face, glyph, sg.Ptr))
                    {
                        shapedGlyphs[index++] = (sg, glyph);
                    }
                    else
                    {
                        EdLog.Gui.Error("Failed to shape glyph: {g} in font: {name}:{style}", glyph, style.Font.Name, style.StyleName);
                        _shapedGlyphPool.Return(sg);
                    }
                }

                for (int i = 0; i < index; ++i)
                {
                    int k = i;

                    rentedTasks[i] = Task.Factory.StartNew(() =>
                    {
                        DisposableShapedGlyph sg = shapedGlyphs[k].Item1;

                        MSDF_RenderBox renderBox = new MSDF_RenderBox();
                        EdInterop.MSDF_CalculateBox(sg.Ptr, style.FontData.GlyphSize, 2.0, 1.0, 2, 2, &renderBox);

                        Vector2 bitmapSize = new Vector2(renderBox.RectW, renderBox.RectH);
                        int bitmapPixelCount = (int)bitmapSize.X * (int)bitmapSize.Y;

                        byte[] bitmapPixels = new byte[bitmapPixelCount * 4];
                        float[] bitmapFloats = new float[bitmapPixelCount * 4];

                        fixed (float* ptr = bitmapFloats)
                        {
                            MSDF_RenderBitmap renderBitmap = new MSDF_RenderBitmap
                            {
                                Pixels = ptr,

                                Width = (int)bitmapSize.X,
                                Height = (int)bitmapSize.Y,

                                RowStride = (int)bitmapSize.X * 4
                            };

                            EdInterop.MSDF_GenerateGlyph(sg.Ptr, &renderBox, &renderBitmap);
                        }

                        //TODO: maybe see if SIMD can speed up this conversion?
                        int total = bitmapPixelCount * 4;
                        for (int i = 0; i < total; i++)
                        {
                            float normal = MathF.Min(MathF.Max(bitmapFloats[i], 0.0f), 1.0f);
                            bitmapPixels[i] = (byte)(~(int)(255.5f - 255.0f * normal));
                        }

                        rendered.Enqueue(new RenderedGlyph(shapedGlyphs[k].Item2, bitmapPixels, bitmapSize));
                    });
                }

                Task.WaitAll(rentedTasks.Span);

                for (int i = 0; i < shapedGlyphs.Length; i++)
                {
                    _shapedGlyphPool.Return(shapedGlyphs[i].Item1);
                }
            }

            if (rendered.Count > 0)
            {
                List<UIGlyphSpace> spaces = style.Spaces;
                if (style.AtlasTexture == null)
                {
                    style.AtlasSize = new Vector2(256.0f);
                    style.AtlasTexture = RHIDevice.Instance!.CreateTexture(new RHITextureDescription
                    {
                        Width = 256,
                        Height = 256,
                        DepthOrArraySize = 1,

                        MipLevels = 1,

                        Usage = RHIResourceUsage.ShaderResource,
                        Dimension = RHIDimension.Texture2D,
                        Format = RHIFormat.RGBA8_UNorm
                    }, Span<nint>.Empty, $"{style.Font.Name}:{style.StyleName}");

                    spaces.Add(new UIGlyphSpace
                    {
                        OffsetFromOrigin = Vector2.Zero,
                        SpaceExtents = style.AtlasSize,
                        CurrentOffset = Vector2.Zero,
                        MaxGlyphHeightOnLine = 0.0f
                    });
                }

                Vector2? newAtlasSize = null;

                List<RenderedGlyph> currentGlyphLine = new List<RenderedGlyph>();
                List<UIGlyphLine> glyphLines = new List<UIGlyphLine>();

                float startLineLeftOffset = -1.0f;
                Vector2 glyphLineExtents = Vector2.Zero;

                while (rendered.TryDequeue(out RenderedGlyph glyph))
                {
                    ref UIGlyphSpace space = ref spaces.AsSpan()[0];
                    if (startLineLeftOffset < 0.0f)
                        startLineLeftOffset = space.OffsetFromOrigin.X + space.CurrentOffset.X;

                    float filledLeftOffset = space.CurrentOffset.X + glyph.BitmapSize.X;
                    if (filledLeftOffset >= space.SpaceExtents.X)
                    {
                        filledLeftOffset = glyph.BitmapSize.X;

                        if (currentGlyphLine.Count > 0)
                            glyphLines.Add(BakeNewGlyphLine(space.OffsetFromOrigin.Y + space.CurrentOffset.Y));

                        currentGlyphLine.Clear();
                        startLineLeftOffset = 0.0f;
                        glyphLineExtents = Vector2.Zero;

                        space.CurrentOffset = new Vector2(0.0f, space.CurrentOffset.Y + space.MaxGlyphHeightOnLine);
                        space.MaxGlyphHeightOnLine = space.OffsetFromOrigin.X;
                    }

                    if (space.CurrentOffset.Y + glyph.BitmapSize.Y >= space.SpaceExtents.Y)
                    {
                        float yOffset = space.OffsetFromOrigin.Y + space.CurrentOffset.Y;
                        spaces.RemoveAt(0);

                        Vector2 atlasSize = newAtlasSize.GetValueOrDefault(style.AtlasSize);
                        newAtlasSize = Vector2.Min(style.AtlasSize * 2.0f, new Vector2(256.0f));

                        spaces.Add(new UIGlyphSpace
                        {
                            OffsetFromOrigin = new Vector2(style.AtlasSize.X, 0.0f),
                            SpaceExtents = style.AtlasSize,
                            CurrentOffset = Vector2.Zero,
                            MaxGlyphHeightOnLine = 0.0f
                        });

                        spaces.Add(new UIGlyphSpace
                        {
                            OffsetFromOrigin = new Vector2(0.0f, style.AtlasSize.X),
                            SpaceExtents = style.AtlasSize,
                            CurrentOffset = Vector2.Zero,
                            MaxGlyphHeightOnLine = 0.0f
                        });

                        spaces.Add(new UIGlyphSpace
                        {
                            OffsetFromOrigin = style.AtlasSize,
                            SpaceExtents = style.AtlasSize,
                            CurrentOffset = Vector2.Zero,
                            MaxGlyphHeightOnLine = 0.0f
                        });

                        space = spaces[0];

                        if (currentGlyphLine.Count > 0)
                            glyphLines.Add(BakeNewGlyphLine(yOffset));

                        currentGlyphLine.Clear();
                        startLineLeftOffset = space.OffsetFromOrigin.X;
                        glyphLineExtents = Vector2.Zero;
                    }

                    currentGlyphLine.Add(glyph);

                    space.CurrentOffset.X = filledLeftOffset;
                    space.MaxGlyphHeightOnLine = MathF.Max(space.MaxGlyphHeightOnLine, glyph.BitmapSize.Y);

                    glyphLineExtents.X += glyph.BitmapSize.X;
                    glyphLineExtents.Y = MathF.Max(glyphLineExtents.Y, glyph.BitmapSize.Y);
                }

                if (currentGlyphLine.Count > 0)
                    glyphLines.Add(BakeNewGlyphLine(spaces[0].OffsetFromOrigin.Y + spaces[0].CurrentOffset.Y));

                UIGlyphLine BakeNewGlyphLine(float yOffset)
                {
                    byte[] pixels = new byte[(int)glyphLineExtents.X * (int)glyphLineExtents.Y * 4];
                    int rowStride = (int)glyphLineExtents.X * 4;

                    Array.Fill<byte>(pixels, 0);

                    fixed (byte* pixelsPtr = pixels)
                    {
                        int currentByteOffset = 0;
                        foreach (RenderedGlyph glyph in currentGlyphLine)
                        {
                            int glyphRowStride = (int)glyph.BitmapSize.X * 4;
                            for (int y = 0; y < glyph.BitmapSize.Y; y++)
                            {
                                Array.Copy(glyph.Bitmap, glyphRowStride * y, pixels, currentByteOffset + rowStride * y, glyphRowStride);
                            }

                            currentByteOffset += glyphRowStride;
                        }
                    }

                    return new UIGlyphLine(new Vector2(startLineLeftOffset, yOffset), pixels, glyphLineExtents);
                }

                if (newAtlasSize.HasValue)
                {
                    RHITexture? oldAtlas = style.AtlasTexture;

                    style.AtlasSize = newAtlasSize.Value;
                    style.AtlasTexture = RHIDevice.Instance!.CreateTexture(new RHITextureDescription
                    {
                        Width = (int)style.AtlasSize.X,
                        Height = (int)style.AtlasSize.Y,
                        DepthOrArraySize = 1,

                        MipLevels = 1,

                        Usage = RHIResourceUsage.ShaderResource,
                        Dimension = RHIDimension.Texture2D,
                        Format = RHIFormat.RGBA8_UNorm
                    }, Span<nint>.Empty, $"{style.Font.Name}:{style.StyleName}");

                    _pendingUpdates.Enqueue(new UIFontUpdate(style, oldAtlas, glyphLines.ToArray()));
                }
                else
                    _pendingUpdates.Enqueue(new UIFontUpdate(style, null, glyphLines.ToArray()));
            }
        }

        public bool DoAnyFontsNeedUpdates => _pendingUpdates.Count > 0;

        internal Queue<UIFontUpdate> PendingFontUpdates => _pendingUpdates;

        private readonly record struct RenderedGlyph(char Glyph, byte[] Bitmap, Vector2 BitmapSize);

        private unsafe struct DisposableShapedGlyph : IDisposable
        {
            public MSDF_ShapedGlyph* Ptr;

            public DisposableShapedGlyph()
            {
                Ptr = EdInterop.MSDF_CreateShapedGlyph();
            }

            public void Dispose()
            {
                EdInterop.MSDF_DestroyShapedGlyph(Ptr);
                Ptr = null;
            }

            public struct Policy : IObjectPoolPolicy<DisposableShapedGlyph>
            {
                DisposableShapedGlyph IObjectPoolPolicy<DisposableShapedGlyph>.Create() => new DisposableShapedGlyph();
                bool IObjectPoolPolicy<DisposableShapedGlyph>.Return(ref DisposableShapedGlyph obj) => true;
            }
        }
    }

    public readonly record struct UIFontUpdate(UIFontStyle Style, RHITexture? OldAtlas, UIGlyphLine[] NewLines);
    public readonly record struct UIGlyphLine(Vector2 TextureOrigin, byte[] BitmapData, Vector2 BitmapSize);
}
