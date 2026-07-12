using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using Editor.Interop.MSDF;
using EditorUI.Text.Visual;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Utility;

namespace EditorUI.Text
{
    public sealed class FontRenderer : IDisposable
    {
        private ConcurrentDictionary<FontStyleData, bool> _unrenderedFontStyles;
        private List<FontAtlasUpdate> _atlasUpdates;

        private List<GlyphRenderGroup> _glyphRenderTaskGroups;

        private List<GlyphBitmap> _glyphBitmaps;
        private Lock _glyphBitmapsLock;

        private bool _disposedValue;

        internal FontRenderer()
        {
            _unrenderedFontStyles = new ConcurrentDictionary<FontStyleData, bool>();
            _atlasUpdates = new List<FontAtlasUpdate>();

            _glyphRenderTaskGroups = new List<GlyphRenderGroup>();

            _glyphBitmaps = new List<GlyphBitmap>();
            _glyphBitmapsLock = new Lock();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    for (int i = 0; i < _glyphRenderTaskGroups.Count; i++)
                    {
                        GlyphRenderGroup renderGroup = _glyphRenderTaskGroups[i];

                        renderGroup.CTS.Cancel();
                        Task.WaitAll(renderGroup.Tasks);

                        renderGroup.CTS.Dispose();
                    }

                    for (int i = 0; i < _atlasUpdates.Count; i++)
                    {
                        _atlasUpdates[i].OldTexture?.Dispose();
                    }

                    _glyphRenderTaskGroups.Clear();
                    _atlasUpdates.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void RequestFontStyleRender(FontStyleData fontStyleData)
        {
            _unrenderedFontStyles.TryAdd(fontStyleData, false);
        }

        internal void CancelFontStyleRender(FontStyleData fontStyleData)
        {
            _unrenderedFontStyles.TryRemove(fontStyleData, out _);

            for (int i = 0; i < _glyphRenderTaskGroups.Count; i++)
            {
                GlyphRenderGroup renderGroup = _glyphRenderTaskGroups[i];
                if (renderGroup.StyleData == fontStyleData)
                {
                    renderGroup.CTS.Cancel();
                }
            }

            using (_glyphBitmapsLock.EnterScope())
            {
                for (int i = 0; i < _glyphBitmaps.Count; ++i)
                {
                    if (_glyphBitmaps[i].GlyphAtlas == fontStyleData.GlyphAtlas)
                    {
                        _glyphBitmaps.RemoveAt(i--);
                    }
                }
            }
        }

        internal void RenderGlyphs()
        {
            if (_atlasUpdates.Count > 0)
            {
                foreach (FontAtlasUpdate atlasUpdate in _atlasUpdates)
                {
                    atlasUpdate.OldTexture?.Dispose();
                }

                _atlasUpdates.Clear();
            }

            if (_glyphRenderTaskGroups.Count > 0)
            {
                for (int i = 0; i < _glyphRenderTaskGroups.Count; i++)
                {
                    GlyphRenderGroup renderGroup = _glyphRenderTaskGroups[i];

                    bool areAllFinished = true;
                    for (int j = 0; j < renderGroup.Tasks.Length; j++)
                    {
                        Task task = renderGroup.Tasks[j];
                        if (!task.IsCompleted)
                        {
                            areAllFinished = false;
                            break;
                        }
                    }

                    if (areAllFinished)
                    {
                        renderGroup.CTS.Dispose();
                        _glyphRenderTaskGroups.RemoveAt(i--);
                    }
                }
            }

            if (!_unrenderedFontStyles.IsEmpty)
            {
                foreach (var (fontStyleData, _) in _unrenderedFontStyles)
                {
                    RenderFontStyleGlyphs(fontStyleData);
                }

                _unrenderedFontStyles.Clear();
            }
        }

        private unsafe void RenderFontStyleGlyphs(FontStyleData fontStyleData)
        {
            long startTimestamp = Stopwatch.GetTimestamp();

            using RentedList<Task> tasks = new RentedList<Task>();

            FontGlyphAtlasData[] glyphsInAtlas = new FontGlyphAtlasData[fontStyleData.UnrenderedGlyphs.Count];
            int arrayIndex = 0;

            CancellationTokenSource cts = new CancellationTokenSource();
            foreach (UnrenderedGlyph glyph in fontStyleData.UnrenderedGlyphs)
            {
                Int2 position = fontStyleData.GlyphAtlas.PackGlyphInto(new Rect(0, 0, glyph.RenderBox.RectW, glyph.RenderBox.RectH));
                glyphsInAtlas[arrayIndex++] = new FontGlyphAtlasData(glyph.Codepoint, new Rect(position.X, position.Y, glyph.RenderBox.RectW, glyph.RenderBox.RectH));

                Task task = Task.Factory.StartNew(() =>
                {
                    using RentedArray<float> bitmapFloats = RentedArray<float>.Rent(glyph.RenderBox.RectW * glyph.RenderBox.RectH * 4);
                    fixed (float* ptr = bitmapFloats.Span)
                    {
                        MSDF_RenderBitmap bitmap = new MSDF_RenderBitmap
                        {
                            Pixels = ptr,

                            Width = glyph.RenderBox.RectW,
                            Height = glyph.RenderBox.RectH,

                            RowStride = 4
                        };

                        MSDF_RenderBox renderBox = glyph.RenderBox;

                        MSDFInterop.GenerateGlyph(glyph.ShapedGlyph.Pointer, &renderBox, &bitmap);
                        MSDFInterop.DestroyShapedGlyph(glyph.ShapedGlyph.Pointer);
                    }

                    cts.Token.ThrowIfCancellationRequested();

                    byte[] pixelsRgba = new byte[bitmapFloats.Count];
                    Span<uint> pixels = MemoryMarshal.Cast<byte, uint>(pixelsRgba.AsSpan());
                    for (int i = 0, j = 0; i < pixels.Length; ++i, j += 4)
                    {
                        Vector128<float> floating = Vector128.LoadUnsafe(ref bitmapFloats.DangerousGetReference(), (nuint)j);
                        Vector128<int> vector = Vector128.ConvertToInt32(Vector128.Create(255.5f) - Vector128.Create(255.0f) * Vector128.Clamp(floating, Vector128<float>.Zero, Vector128<float>.One));

                        pixels[i] = ~new GlyphPixel
                        {
                            Red = (byte)vector[0],
                            Green = (byte)vector[1],
                            Blue = (byte)vector[2],
                            Alpha = (byte)vector[3],
                        }.RGBA;
                    }

                    using (_glyphBitmapsLock.EnterScope())
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        _glyphBitmaps.Add(new GlyphBitmap(fontStyleData.GlyphAtlas, pixelsRgba, new Rect(position.X, position.Y, glyph.RenderBox.RectW, glyph.RenderBox.RectH)));
                    }
                });

                tasks.Add(task);
            }

            if (tasks.IsEmpty)
            {
                cts.Dispose();
            }
            else
            {
                _glyphRenderTaskGroups.Add(new GlyphRenderGroup(fontStyleData, [.. tasks], cts));
            }

            IFontTexture? oldTexture = fontStyleData.GlyphAtlas.FinishPacking();

            fontStyleData.UpdateGlyphUVs(glyphsInAtlas, oldTexture != null);
            fontStyleData.ClearUnrenderedGlyphsSet();

            if (oldTexture != null)
            {
                bool foundUpdate = false;
                for (int i = 0; i < _atlasUpdates.Count; i++)
                {
                    if (_atlasUpdates[i].CurrentTexture == oldTexture)
                    {
                        _atlasUpdates[i] = new FontAtlasUpdate(fontStyleData.GlyphAtlas.FontTexture!, oldTexture);
                        foundUpdate = true;
                    }
                }

                if (!foundUpdate)
                    _atlasUpdates.Add(new FontAtlasUpdate(fontStyleData.GlyphAtlas.FontTexture!, oldTexture));
            }

            UILog.Logger?.Debug("Queued #{c} new glyphs to be rendered for the font '{s}:{w}' in {secs}s", glyphsInAtlas.Length, fontStyleData.Style, fontStyleData.Weight, Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds);
        }

        public TryEnterLockScope TryEnterBitmapLockScope(out bool success, out ROList<GlyphBitmap> bitmaps)
        {
            TryEnterLockScope lockScope = _glyphBitmapsLock.TryEnterScope(out success);
            bitmaps = success ? _glyphBitmaps : ROList<GlyphBitmap>.Empty;
            return lockScope;
        }

        public void ClearBitmapsWhenFinished()
        {
            _glyphBitmaps.Clear();
        }

        public ROList<FontAtlasUpdate> AtlasUpdates => _atlasUpdates;

        public bool HasGlyphBitmapUpdates => _glyphBitmaps.Count > 0;

        [StructLayout(LayoutKind.Explicit)]
        private record struct GlyphPixel
        {
            [FieldOffset(0)] public uint RGBA;

            [FieldOffset(0)] public byte Red;
            [FieldOffset(1)] public byte Green;
            [FieldOffset(2)] public byte Blue;
            [FieldOffset(3)] public byte Alpha;
        }

        private readonly record struct GlyphRenderGroup(FontStyleData StyleData, Task[] Tasks, CancellationTokenSource CTS);
    }

    public readonly record struct GlyphBitmap(FontGlyphAtlas GlyphAtlas, byte[] PixelsRGBA, Rect AtlasRect);
    public readonly record struct FontAtlasUpdate(IFontTexture CurrentTexture, IFontTexture? OldTexture);
}
