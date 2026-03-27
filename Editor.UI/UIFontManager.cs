using CommunityToolkit.HighPerformance;
using Editor.Interop.Ed;
using Editor.UI.Assets;
using Editor.UI.Helpers;
using Primary.Collections;
using Primary.Common;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.Pooling;
using Primary.Profiling;
using Primary.RHI2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

namespace Editor.UI
{
    public sealed class UIFontManager : IDisposable
    {
        private HashSet<UIFontStyle> _pendingFontUpdates;
        private Queue<UIFontUpdate> _pendingUpdates;

        private bool _disposedValue;

        internal UIFontManager()
        {
            _pendingFontUpdates = new HashSet<UIFontStyle>();
            _pendingUpdates = new Queue<UIFontUpdate>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    while (_pendingUpdates.TryDequeue(out UIFontUpdate update))
                        update.OldAtlas?.Dispose();
                }

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
                using (new ProfilingScope("UpdateFonts"))
                {
                    long startTime = Stopwatch.GetTimestamp();
                    int glyphCount = 0;

                    foreach (UIFontStyle style in _pendingFontUpdates)
                    {
                        using (new ProfilingScope($"{style.Font.Name}-{style.StyleName}"))
                        {
                            glyphCount += style.UnrenderedGlyphs.Count;
                            RenderNewFontGlyphs(style);
                        }
                    }

                    _pendingFontUpdates.Clear();

                    UIManager.Logger?.Debug("Rendering new glyphs took: {t}ms (count: {c})", Stopwatch.GetElapsedTime(startTime).TotalMilliseconds, glyphCount);
                }
            }
        }

        private unsafe void RenderNewFontGlyphs(UIFontStyle style)
        {
            style.RequestGlyph(unchecked((char)-1));

            Queue<UIShapedGlyph> unrendered = style.UnrenderedGlyphs;
            ConcurrentBag<RenderedGlyph> renderedGlyphs = new ConcurrentBag<RenderedGlyph>();

            style.FontData.SetShapingFontStyle(style);

            {
                Parallel.ForEach(unrendered.AsEnumerable(), (x) =>
                {
                    Int2 bitmapSize = new Int2(x.Box.RectW, x.Box.RectH);
                    int bitmapPixelCount = (int)bitmapSize.X * (int)bitmapSize.Y;

                    float[] bitmapFloats = new float[bitmapPixelCount * 4];
                    fixed (float* tempPtr = bitmapFloats)
                    {
                        MSDF_RenderBox box = x.Box;
                        MSDF_RenderBitmap renderBitmap = new MSDF_RenderBitmap
                        {
                            Pixels = tempPtr,

                            Width = bitmapSize.X,
                            Height = bitmapSize.Y,

                            RowStride = bitmapSize.X * 4
                        };

                        EdInterop.MSDF_GenerateGlyph(x.Shaped.Pointer, &box, &renderBitmap);
                    }

                    byte[] bitmapPixels = new byte[bitmapPixelCount * 4];
                    for (int i = 0; i < bitmapPixels.Length; i += 4)
                    {
                        Vector4 v = Unsafe.As<float, Vector4>(ref bitmapFloats[i]);
                        Vector128<int> vector = ~Vector128.ConvertToInt32(s_vector255_5 - s_vector255 * Vector128.Clamp(v.AsVector128(), Vector128<float>.Zero, Vector128<float>.One));

                        Unsafe.As<byte, int>(ref bitmapPixels[i]) = ((byte)vector[0] << 24) | ((byte)vector[1] << 16) | ((byte)vector[2] << 8) | (byte)vector[3];
                    }

                    renderedGlyphs.Add(new RenderedGlyph(x.Codepoint, bitmapPixels, bitmapSize, x));
                });
            }

            if (!renderedGlyphs.IsEmpty)
            {
                ref UIGlyphSpace currentSpace = ref style.Space;
                Vector2 atlasSize = style.AtlasSize;

                if (atlasSize == Vector2.Zero)
                {
                    atlasSize = new Vector2(AtlasIncrementSize);
                    currentSpace = new UIGlyphSpace
                    {
                        SpacePosition = Int2.Zero,
                        SpaceSize = new Int2(AtlasIncrementSize),

                        CurrentOffset = Int2.Zero,
                        MaxLineHeight = 0
                    };
                }

                using RentedList<UIGlyphBitmap> bitmaps = new RentedList<UIGlyphBitmap>();
                while (renderedGlyphs.TryTake(out RenderedGlyph glyph))
                {
                    if (currentSpace.CurrentOffset.X + glyph.BitmapSize.X > currentSpace.SpaceSize.X)
                    {
                        currentSpace.CurrentOffset = new Int2(0, currentSpace.CurrentOffset.Y + currentSpace.MaxLineHeight);
                        currentSpace.MaxLineHeight = 0;
                    }

                    if (currentSpace.CurrentOffset.Y + glyph.BitmapSize.Y > currentSpace.SpaceSize.Y)
                    {
                        if (atlasSize.X > atlasSize.Y)
                        {
                            currentSpace = new UIGlyphSpace
                            {
                                SpacePosition = new Int2(0, (int)atlasSize.Y),
                                SpaceSize = new Int2((int)atlasSize.X, AtlasIncrementSize),

                                CurrentOffset = Int2.Zero,
                                MaxLineHeight = 0
                            };

                            atlasSize.Y += AtlasIncrementSize;
                        }
                        else
                        {
                            currentSpace = new UIGlyphSpace
                            {
                                SpacePosition = new Int2((int)atlasSize.X, 0),
                                SpaceSize = new Int2(AtlasIncrementSize, (int)atlasSize.Y),

                                CurrentOffset = Int2.Zero,
                                MaxLineHeight = 0
                            };

                            atlasSize.X += AtlasIncrementSize;
                        }
                    }

                    bitmaps.Add(new UIGlyphBitmap(currentSpace.CurrentOffset, glyph.BitmapSize, glyph.Bitmap));
                    style.AddGlyph(new UIGlyphData(glyph.Glyph, currentSpace.CurrentOffset, glyph.BitmapSize));

                    currentSpace.CurrentOffset.X += glyph.BitmapSize.X;
                    currentSpace.MaxLineHeight = Math.Max(currentSpace.MaxLineHeight, glyph.BitmapSize.Y);
                }

                if (bitmaps.Count > 0)
                {
                    if (atlasSize != style.AtlasSize)
                    {
                        RHITexture? oldAtlas = style.AtlasTexture;

                        style.AtlasSize = atlasSize;
                        style.AtlasTexture = RHIDevice.Instance!.CreateTexture(new RHITextureDescription
                        {
                            Width = (int)style.AtlasSize.X,
                            Height = (int)style.AtlasSize.Y,
                            DepthOrArraySize = 1,

                            MipLevels = 1,

                            Usage = RHIResourceUsage.ShaderResource,
                            Dimension = RHIDimension.Texture2D,
                            Format = RHIFormat.RGBA8_UNorm
                        }, Span<ArrayPtr<byte>>.Empty, $"{style.Font.Name}:{style.StyleName}");

                        _pendingUpdates.Enqueue(new UIFontUpdate(style, style.Font.LoadIndex, oldAtlas, bitmaps.ToArray()));
                    }
                    else
                        _pendingUpdates.Enqueue(new UIFontUpdate(style, style.Font.LoadIndex, null, bitmaps.ToArray()));
                }

                style.GenerateUVsForGlyphs();
            }
        }

        public bool DoAnyFontsNeedUpdates => _pendingUpdates.Count > 0;

        internal Queue<UIFontUpdate> PendingFontUpdates => _pendingUpdates;

        private static readonly Vector128<float> s_vector255_5 = Vector128.Create(255.5f);
        private static readonly Vector128<float> s_vector255 = Vector128.Create(255.0f);

        private const int AtlasIncrementSize = 256;

        private readonly record struct RenderedGlyph(char Glyph, byte[] Bitmap, Int2 BitmapSize, UIShapedGlyph ShapedGlyph);

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

    public readonly record struct UIFontUpdate(UIFontStyle Style, int LoadIndex, RHITexture? OldAtlas, UIGlyphBitmap[] Bitmaps);
    public readonly record struct UIGlyphBitmap(Int2 TextureOffset, Int2 TextureSize, byte[] Pixels);
}
