using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Editor.Interop.Ed;
using Editor.UI.Font;
using Editor.UI.Text;
using Primary.Assets.Types;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.RHI;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public UIFontTypeData? FindStyle(FontWeight weight = FontWeight.Normal)
        {
            if (Status != ResourceStatus.Success)
                return null;
            return AssetData.GetFontType(FontStyle.Normal, weight);
        }

        public UIFontTypeData? FindStyle(FontStyle style, FontWeight weight = FontWeight.Normal)
        {
            if (Status != ResourceStatus.Success)
                return null;
            return AssetData.GetFontType(style, weight);
        }

        public int GlyphSize => AssetData.GlyphSize;
    }

    public sealed class UIFontAssetData : BaseInternalAssetData<UIFontAsset>
    {
        private byte[] _sourceData;

        private int _glyphSize;

        private UIFontStyleData[] _styles;

        public UIFontAssetData(AssetId id) : base(id)
        {
            _sourceData = Array.Empty<byte>();

            _glyphSize = 0;

            _styles = [];
        }

        public unsafe override void Dispose()
        {
            foreach (UIFontStyleData styleData in _styles)
            {
                foreach (UIFontTypeData fontType in styleData.Weights)
                {
                    fontType.Dispose();
                }
            }

            base.Dispose();
        }

        public void UpdateAssetData(UIFontAsset asset, byte[] sourceData, int glyphSize, UIFontStyleData[] styles)
        {
            base.UpdateAssetData(asset);

            _sourceData = sourceData;

            _glyphSize = glyphSize;

            _styles = styles;
        }

        public ImmutableArray<UIFontTypeData> GetFontTypes(FontStyle style) => _styles[(int)style].Weights;
        public UIFontTypeData GetFontType(FontStyle style, FontWeight weight) => _styles[(int)style].GetFontType(weight);

        public ReadOnlySpan<byte> SourceData => _sourceData;

        public int GlyphSize => _glyphSize;
    }

    public sealed class UIFontStyleData
    {
        private readonly ImmutableArray<UIFontTypeData> _weights;

        internal UIFontStyleData(ImmutableArray<UIFontTypeData> weights)
        {
            _weights = weights;
        }

        public UIFontTypeData GetFontType(FontWeight weight) => _weights[(int)weight];
        public UIFontTypeData GetFontType(int weight)
        {
            weight = weight / 100 - 1;
            Guard.IsInRange(weight, (int)FontWeight._100, (int)FontWeight._900);
            return _weights[weight];
        }

        public ImmutableArray<UIFontTypeData> Weights => _weights;
    }

    public sealed class UIFontTypeData : IDisposable
    {
        private readonly UIFontAsset _assetDef;
        private readonly UIFontAssetData _assetData;

        private readonly FontStyle _style;
        private readonly FontWeight _weight;

        private readonly FrozenDictionary<char, int> _codepoints;

        private readonly FontStyleAdvances _advances;
        private readonly FontStyleMetrics _metrics;

        private readonly Lock _lock;

        private List<UIGlyphData> _data;

        private ConcurrentDictionary<char, UIGlyph> _glyphs;
        private UIGlyphSpace _space;

        private Vector2 _atlasSize;
        private RHITexture? _atlasTexture;

        private GlyphCache _glyphCache;

        private Queue<UIShapedGlyph> _unrenderedGlyphs;

        private bool _disposedValue;

        public UIFontTypeData(UIFontAsset asset, UIFontAssetData assetData, FontStyle style, FontWeight weight, FrozenDictionary<char, int> codepoints, FontStyleAdvances advances, FontStyleMetrics metrics)
        {
            _assetDef = asset;
            _assetData = assetData;

            _style = style;
            _weight = weight;

            _codepoints = codepoints;

            _advances = advances;
            _metrics = metrics;

            _lock = new Lock();

            _data = new List<UIGlyphData>();

            _glyphs = new ConcurrentDictionary<char, UIGlyph>();
            _space = default;

            _atlasSize = Vector2.Zero;
            _atlasTexture = null;

            _glyphCache = new GlyphCache(assetData.Id, style, weight);

            _unrenderedGlyphs = new Queue<UIShapedGlyph>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _glyphCache.Dispose();

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

            lock (_lock)
            {
                glyph = GenerateNewGlyph(c);
            }

            _glyphs.TryAdd(c, glyph);
            return glyph;
        }

        private unsafe UIShapedGlyph? ShapeNewGlyph(char c)
        {
            if (_codepoints.TryGetValue(c, out int offset))
            {
                fixed (byte* ptr = _assetData.SourceData)
                {
                    MSDF_ShapedGlyph* shapedGlyph = EdInterop.MSDF_DeserializeShapedGlyph(ptr + offset, _metrics.UnitsPerEM);

                    MSDF_RenderBox box;
                    EdInterop.MSDF_CalculateBox(shapedGlyph, _assetData.GlyphSize, 2.0, 1.0, 0, 0, &box);

                    return new UIShapedGlyph(c, shapedGlyph, box, (float)shapedGlyph->Advance);
                }
            }

            return null;
        }

        private UIGlyph GenerateNewGlyph(char c)
        {
            UIShapedGlyph? metricsNullable = ShapeNewGlyph(c);
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

        public FontStyle Style => _style;
        public FontWeight Weight => _weight;

        public FontStyleAdvances Advances => _advances;
        public FontStyleMetrics Metrics => _metrics;

        internal ref UIGlyphSpace Space => ref _space;

        internal Vector2 AtlasSize { get => _atlasSize; set => _atlasSize = value; }
        internal RHITexture? AtlasTexture { get => _atlasTexture; set => _atlasTexture = value; }

        internal GlyphCache GlyphCache => _glyphCache;

        internal Queue<UIShapedGlyph> UnrenderedGlyphs => _unrenderedGlyphs;
    }

    public readonly record struct FontStyleAdvances(float Space, float Tab);
    public readonly record struct FontStyleMetrics(double UnitsPerEM, float Ascender, float Descender, float LineHeight, float UnderlineY, float Height);

    public readonly record struct UIGlyph(Vector4 PlaneBounds, Vector2 Size, Vector4 AtlasUVs, float Advance);

    public enum FontStyle : byte
    {
        Normal,
        Italic
    }

    public enum FontWeight : byte
    {
        _100 = 0,
        _200,
        _300,
        _400,
        _500,
        _600,
        _700,
        _800,
        _900,

        Lighter = _300,
        Normal = _400,
        Bold = _700,
        Bolder = _800
    }

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
