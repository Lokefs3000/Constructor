using CommunityToolkit.HighPerformance;
using Editor.Interop.Ed;
using Editor.UI.Text;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Mathematics;
using Primary.RHI2;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace Editor.UI.Assets
{
    public sealed class UIFontAsset : BaseAssetDefinition<UIFontAsset, UIFontAssetData>
    {
        public UIFontAsset(UIFontAssetData assetData) : base(assetData)
        {
        }

        public UIFontStyle? FindStyle(string? styleName)
        {
            styleName ??= string.Empty;

            if (AssetData.Styles.TryGetValue(styleName, out UIFontStyle? style))
                return style;

            return null;
        }

        public UIFontStyle? FindStyle(int styleId)
        {
            if (AssetData.IdStyles.TryGetValue(styleId, out UIFontStyle? style))
                return style;

            return null;
        }

        public IReadOnlyDictionary<string, UIFontStyle> Styles => AssetData.Styles;

        public int GlyphSize => AssetData.GlyphSize;
    }

    public sealed class UIFontAssetData : BaseInternalAssetData<UIFontAsset>
    {
        private nint _ft;
        private nint _fontFace;
        private nint _fontVars;

        private int _glyphSize;

        private Ptr<MSDF_ShapedGlyph> _shapedGlyph;

        private int _activeFontStyle;

        private FrozenDictionary<string, UIFontStyle> _styles;
        private FrozenDictionary<int, UIFontStyle> _idStyles;

        private Lock _lock;

        public UIFontAssetData(AssetId id) : base(id)
        {
            _ft = nint.Zero;
            _fontFace = nint.Zero;
            _fontVars = nint.Zero;

            _glyphSize = 0;

            _shapedGlyph = Ptr<MSDF_ShapedGlyph>.Null;

            _activeFontStyle = 0;

            _styles = FrozenDictionary<string, UIFontStyle>.Empty;
            _idStyles = FrozenDictionary<int, UIFontStyle>.Empty;

            _lock = new Lock();
        }

        public unsafe override void Dispose()
        {
            foreach (var kvp in _styles)
                kvp.Value.Dispose();

            if (!_shapedGlyph.IsNull)
                EdInterop.MSDF_DestroyShapedGlyph(_shapedGlyph.Pointer);

            if (_ft != nint.Zero && _fontVars != nint.Zero)
                EdInterop.MSDF_DestroyVarData(_ft, _fontVars);
            if (_fontFace != nint.Zero)
                EdInterop.MSDF_DestroyFont((MSDF_FontFace*)_fontFace);
            if (_ft != nint.Zero)
                EdInterop.MSDF_ShutdownFt(_ft);

            _styles = FrozenDictionary<string, UIFontStyle>.Empty;
            _idStyles = FrozenDictionary<int, UIFontStyle>.Empty;

            _activeFontStyle = -1;

            _shapedGlyph = null;

            _fontVars = nint.Zero;
            _fontFace = nint.Zero;
            _ft = nint.Zero;

            base.Dispose();
        }

        public void UpdateAssetData(UIFontAsset asset, nint ft, nint fontFace, nint fontVars, int glyphSize, Ptr<MSDF_ShapedGlyph> shapedGlyph, FrozenDictionary<string, UIFontStyle> styles)
        {
            base.UpdateAssetData(asset);

            _ft = ft;
            _fontFace = fontFace;
            _fontVars = fontVars;

            _glyphSize = glyphSize;

            _shapedGlyph = shapedGlyph;

            _activeFontStyle = 0;

            _styles = styles;
            _idStyles = styles.Select((x) => new KeyValuePair<int, UIFontStyle>(x.Key.GetDjb2HashCode(), x.Value)).ToFrozenDictionary();
        }

        internal unsafe UIShapedGlyph? ShapeNewGlyph(UIFontStyle style, char c)
        {
            if (_activeFontStyle != style.Index)
            {
                MSDF_VarFontStyle varFontStyle = default;
                nint fontStyleRaw = EdInterop.MSDF_GetVarFontStyle((MSDF_FontFace*)_fontFace, _fontVars, (uint)style.Index, &varFontStyle);

                EdInterop.MSDF_SetFontStyle((MSDF_FontFace*)_fontFace, (uint)style.Index, fontStyleRaw);
                _activeFontStyle = style.Index;
            }

            MSDF_ShapedGlyph* shapedGlyph = _shapedGlyph.Pointer;
            if (EdInterop.MSDF_ShapeGlyph((MSDF_FontFace*)_fontFace, (uint)c, shapedGlyph))
            {
                MSDF_RenderBox box;
                EdInterop.MSDF_CalculateBox(shapedGlyph, _glyphSize, 2.0, 1.0, 0, 0, &box);

                _shapedGlyph = EdInterop.MSDF_CreateShapedGlyph();

                return new UIShapedGlyph(c, shapedGlyph, box, (float)shapedGlyph->Advance);
            }

            return null;
        }

        internal unsafe void SetShapingFontStyle(UIFontStyle style)
        {
            if (_activeFontStyle != style.Index)
            {
                MSDF_VarFontStyle varFontStyle = default;
                nint fontStyleRaw = EdInterop.MSDF_GetVarFontStyle((MSDF_FontFace*)_fontFace, _fontVars, (uint)style.Index, &varFontStyle);

                EdInterop.MSDF_SetFontStyle((MSDF_FontFace*)_fontFace, (uint)style.Index, fontStyleRaw);
                _activeFontStyle = style.Index;
            }
        }

        internal nint Ft => _ft;
        internal nint Face => _fontFace;
        internal nint Vars => _fontVars;

        public int GlyphSize => _glyphSize;

        public IReadOnlyDictionary<string, UIFontStyle> Styles => _styles;
        public IReadOnlyDictionary<int, UIFontStyle> IdStyles => _idStyles;

        internal Lock Lock => _lock;
    }

    public sealed class UIFontStyle : IDisposable
    {
        private readonly UIFontAsset _assetDef;
        private readonly UIFontAssetData _assetData;

        private string _styleName;
        private int _index;

        private FontStyleAdvances _advances;
        private FontStyleMetrics _metrics;

        private List<UIGlyphData> _data;

        private ConcurrentDictionary<char, UIGlyph> _glyphs;
        private UIGlyphSpace _space;

        private Vector2 _atlasSize;
        private RHITexture? _atlasTexture;

        private Queue<UIShapedGlyph> _unrenderedGlyphs;

        private bool _disposedValue;

        public UIFontStyle(UIFontAsset asset, UIFontAssetData assetData, string styleName, int index, FontStyleAdvances advances, FontStyleMetrics metrics)
        {
            _assetDef = asset;
            _assetData = assetData;

            _styleName = styleName;
            _index = index;

            _advances = advances;
            _metrics = metrics;

            _data = new List<UIGlyphData>();

            _glyphs = new ConcurrentDictionary<char, UIGlyph>();
            _space = default;

            _atlasSize = Vector2.Zero;
            _atlasTexture = null;

            _unrenderedGlyphs = new Queue<UIShapedGlyph>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _atlasTexture?.Dispose();
                    _atlasTexture = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public UIGlyph RequestGlyph(char c)
        {
            switch (c)
            {
                case ' ': return new UIGlyph(Vector4.Zero, Vector2.Zero, Vector4.Zero, _advances.Space);
                case '\t': return new UIGlyph(Vector4.Zero, Vector2.Zero, Vector4.Zero, _advances.Tab);
            }

            if (_glyphs.TryGetValue(c, out UIGlyph glyph))
                return glyph;

            lock (_assetData.Lock)
            {
                glyph = GenerateNewGlyph(c);
            }

            _glyphs.TryAdd(c, glyph);
            return glyph;
        }

        private UIGlyph GenerateNewGlyph(char c)
        {
            UIShapedGlyph? metricsNullable = _assetData.ShapeNewGlyph(this, c);
            if (!metricsNullable.HasValue)
            {
                if (c == unchecked((char)-1))
                    return default;

                _unrenderedGlyphs.Enqueue(new UIShapedGlyph(c, Ptr<MSDF_ShapedGlyph>.Null, default, 0.0f));
                UIManager.Instance.FontManager.AddFontToUpdateSet(this);

                return RequestGlyph(unchecked((char)-1) /*65536, U+???? - .notdef*/);
            }

            UIShapedGlyph metrics = metricsNullable.Value;

            _unrenderedGlyphs.Enqueue(metrics);
            UIManager.Instance.FontManager.AddFontToUpdateSet(this);

            double invBoxScale = 1.0 / FontData.GlyphSize;
            double halfScale = invBoxScale * 0.5;

            Vector256<double> vec;
            {
                MSDF_Vector2 tmp = metrics.Box.Projection.Translate;

                Vector128<double> lower = -Vector128.LoadUnsafe(ref Unsafe.As<MSDF_Vector2, double>(ref tmp));
                Vector128<double> upper = lower + Vector128.Create((double)metrics.Box.RectW, metrics.Box.RectH) * Vector128.Create(invBoxScale);

                vec = Vector256.Create(lower, upper) + Vector256.Create(halfScale);
            }

            return new UIGlyph(
                new Vector4((float)vec[0], (float)-vec[3], (float)vec[2], (float)-vec[1]),
                new Vector2((float)(vec[2] - vec[0]), (float)(vec[1] - vec[3])),
                Vector4.Zero,
                metrics.Advance);
        }

        internal void AddGlyph(UIGlyphData data) => _data.Add(data);

        internal void GenerateUVsForGlyphs()
        {
            ReadOnlySpan<UIGlyphData> span = _data.AsSpan();
            for (int i = 0; i < _data.Count; i++)
            {
                ref readonly UIGlyphData glyph = ref span.DangerousGetReferenceAt(i);
                UIGlyph oldGlyph = _glyphs[glyph.Codepoint];

                Vector2 uvMin = glyph.BitmapOffset.AsVector2() / _atlasSize;
                Vector2 uvMax = (glyph.BitmapOffset + glyph.BitmapSize).AsVector2() / _atlasSize;

                _glyphs[glyph.Codepoint] = new UIGlyph(oldGlyph.PlaneBounds, oldGlyph.Size, new Vector4(uvMin, uvMax.X, uvMax.Y), oldGlyph.Advance);
            }
        }

        public UIFontAsset Font => _assetDef;
        internal UIFontAssetData FontData => _assetData;

        public string StyleName => _styleName;
        public int Index => _index;

        public FontStyleAdvances Advances => _advances;
        public FontStyleMetrics Metrics => _metrics;

        internal ref UIGlyphSpace Space => ref _space;

        internal Vector2 AtlasSize { get => _atlasSize; set => _atlasSize = value; }
        internal RHITexture? AtlasTexture { get => _atlasTexture; set => _atlasTexture = value; }

        internal Queue<UIShapedGlyph> UnrenderedGlyphs => _unrenderedGlyphs;
    }

    public readonly record struct FontStyleAdvances(float Space, float Tab);
    public readonly record struct FontStyleMetrics(float Ascender, float Descender, float LineHeight, float UnderlineY, float Height);

    public readonly record struct UIGlyph(Vector4 PlaneBounds, Vector2 Size, Vector4 AtlasUVs, float Advance);

    internal struct UIGlyphSpace
    {
        public Int2 SpacePosition;
        public Int2 SpaceSize;

        public Int2 CurrentOffset;
        public int MaxLineHeight;
    }

    internal readonly record struct UIShapedGlyph(char Codepoint, Ptr<MSDF_ShapedGlyph> Shaped, MSDF_RenderBox Box, float Advance);
    internal readonly record struct UIGlyphData(char Codepoint, Int2 BitmapOffset, Int2 BitmapSize);
}
