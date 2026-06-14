using Editor.Interop.Ed;
using Editor.UI.Assets;
using Editor.UI.Font;
using Primary.Collections;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.Pooling;
using Primary.Profiling;
using Primary.RHI;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using TerraFX.Interop.WinRT;

namespace Editor.UI
{
    public sealed class UIFontManager : IDisposable
    {
        private HashSet<UIFontTypeData> _pendingFontUpdates;
        private Queue<UIFontUpdate> _pendingUpdates;

        private bool _disposedValue;

        internal UIFontManager()
        {
            _pendingFontUpdates = new HashSet<UIFontTypeData>();
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

        internal void AddFontToUpdateSet(UIFontTypeData font)
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

                    foreach (UIFontTypeData style in _pendingFontUpdates)
                    {
                        using (new ProfilingScope($"{style.Font.Name}-{style.Style}-{style.Weight}"))
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

        private unsafe void RenderNewFontGlyphs(UIFontTypeData typeData)
        {
            typeData.RequestGlyph(unchecked((char)-1));

            Queue<UIShapedGlyph> unrendered = typeData.UnrenderedGlyphs;
            ConcurrentBag<RenderedGlyph> renderedGlyphs = new ConcurrentBag<RenderedGlyph>();

            GlyphCache glyphCache = typeData.GlyphCache;

            {
                HashSet<char> alreadyRendered = new HashSet<char>();
                foreach (UIShapedGlyph x in unrendered)
                {
                    Int2 bitmapSize = new Int2(x.Box.RectW, x.Box.RectH);
                    int bitmapPixelCount = bitmapSize.X * bitmapSize.Y * 4;

                    if (glyphCache.TryGetBitmap(x.Codepoint, out CachedGlyphBitmap cachedBitmap))
                    {
                        if (cachedBitmap.Pixels.Length == bitmapPixelCount)
                        {
                            renderedGlyphs.Add(new RenderedGlyph(x.Codepoint, cachedBitmap.Pixels, bitmapSize, x, true));
                            alreadyRendered.Add(x.Codepoint);
                        }
                        else
                            glyphCache.RemoveBitmap(x.Codepoint);
                    }
                }

                Parallel.ForEach(unrendered.AsEnumerable().Where((x) => !alreadyRendered.Contains(x.Codepoint)), (x) =>
                {
                    if (x.Shaped.IsNull)
                        return;

                    Int2 bitmapSize = new Int2(x.Box.RectW, x.Box.RectH);
                    int bitmapPixelCount = bitmapSize.X * bitmapSize.Y;

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

                    Span<uint> pixelsInt = MemoryMarshal.Cast<byte, uint>(bitmapPixels.AsSpan());
                    for (int i = 0, j = 0; i < bitmapPixels.Length; i += 4, ++j)
                    {
                        Vector4 v = Unsafe.As<float, Vector4>(ref bitmapFloats[i]);

                        Vector128<int> vector = Vector128.ConvertToInt32(s_vector255_5 - s_vector255 * Vector128.Clamp(v.AsVector128(), Vector128<float>.Zero, Vector128<float>.One));

                        uint pixel = 0;
                        byte* rgba = (byte*)&pixel;

                        rgba[0] = (byte)vector[0];
                        rgba[1] = (byte)vector[1];
                        rgba[2] = (byte)vector[2];
                        rgba[3] = (byte)vector[3];

                        pixelsInt[j] = ~pixel;
                    }

                    renderedGlyphs.Add(new RenderedGlyph(x.Codepoint, bitmapPixels, bitmapSize, x, false));
                });

                foreach (UIShapedGlyph shapedGlyph in unrendered)
                {
                    if (!shapedGlyph.Shaped.IsNull)
                        EdInterop.MSDF_DestroyShapedGlyph(shapedGlyph.Shaped.Pointer);
                }
                unrendered.Clear();
            }

            if (!renderedGlyphs.IsEmpty)
            {
                ref UIGlyphSpace currentSpace = ref typeData.Space;
                Vector2 atlasSize = typeData.AtlasSize;

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
                    Int2 paddedSize = glyph.BitmapSize + Int2.One;

                    if (currentSpace.CurrentOffset.X + paddedSize.X > currentSpace.SpaceSize.X)
                    {
                        currentSpace.CurrentOffset = new Int2(0, currentSpace.CurrentOffset.Y + currentSpace.MaxLineHeight);
                        currentSpace.MaxLineHeight = 0;
                    }

                    if (currentSpace.CurrentOffset.Y + paddedSize.Y > currentSpace.SpaceSize.Y)
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

                    if (glyph.BitmapSize > Int2.Zero)
                        bitmaps.Add(new UIGlyphBitmap(currentSpace.CurrentOffset, glyph.BitmapSize, glyph.Bitmap));
                    typeData.AddGlyph(new UIGlyphData(glyph.Glyph, currentSpace.CurrentOffset, glyph.BitmapSize));

                    if (!glyph.IsFromCache)
                        glyphCache.StoreBitmap(glyph.Glyph, new CachedGlyphBitmap(glyph.Bitmap));

                    currentSpace.CurrentOffset.X += paddedSize.X;
                    currentSpace.MaxLineHeight = Math.Max(currentSpace.MaxLineHeight, paddedSize.Y);
                }

                if (bitmaps.Count > 0)
                {
                    if (atlasSize != typeData.AtlasSize)
                    {
                        RHITexture? oldAtlas = typeData.AtlasTexture;

                        typeData.AtlasSize = atlasSize;
                        typeData.AtlasTexture = RHIDevice.Instance!.CreateTexture(new RHITextureDescription
                        {
                            Width = (int)typeData.AtlasSize.X,
                            Height = (int)typeData.AtlasSize.Y,
                            DepthOrArraySize = 1,

                            MipLevels = 1,

                            Usage = RHIResourceUsage.ShaderResource,
                            Dimension = RHIDimension.Texture2D,
                            Format = RHIFormat.RGBA8_UNorm
                        }, Span<ArrayPtr<byte>>.Empty, $"{typeData.Font.Name}:{typeData.Style}:{typeData.Weight}");

                        _pendingUpdates.Enqueue(new UIFontUpdate(typeData, typeData.Font.LoadIndex, oldAtlas, [.. bitmaps]));
                    }
                    else
                        _pendingUpdates.Enqueue(new UIFontUpdate(typeData, typeData.Font.LoadIndex, null, [.. bitmaps]));
                }

                typeData.GenerateUVsForGlyphs();
            }
        }

        public bool DoAnyFontsNeedUpdates => _pendingUpdates.Count > 0;

        internal Queue<UIFontUpdate> PendingFontUpdates => _pendingUpdates;

        private static readonly Vector128<float> s_vector255_5 = Vector128.Create(255.5f);
        private static readonly Vector128<float> s_vector255 = Vector128.Create(255.0f);

        private const int AtlasIncrementSize = 256;

        private readonly record struct RenderedGlyph(char Glyph, byte[] Bitmap, Int2 BitmapSize, UIShapedGlyph ShapedGlyph, bool IsFromCache);

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

    public readonly record struct UIFontUpdate(UIFontTypeData TypeData, int LoadIndex, RHITexture? OldAtlas, UIGlyphBitmap[] Bitmaps);
    public readonly record struct UIGlyphBitmap(Int2 TextureOffset, Int2 TextureSize, byte[] Pixels);
}
