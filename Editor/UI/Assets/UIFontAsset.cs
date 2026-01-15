using Editor.Interop.Ed;
using Primary.Assets.Types;
using Primary.Common;
using Primary.RHI2;
using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
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

        public IReadOnlyDictionary<string, UIFontStyle> Styles => AssetData.Styles;
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

        public UIFontAssetData(AssetId id) : base(id)
        {
            _ft = nint.Zero;
            _fontFace = nint.Zero;
            _fontVars = nint.Zero;

            _glyphSize = 0;

            _shapedGlyph = Ptr<MSDF_ShapedGlyph>.Null;

            _activeFontStyle = 0;

            _styles = FrozenDictionary<string, UIFontStyle>.Empty;
        }

        public unsafe override void Dispose()
        {
            foreach (var kvp in _styles)
                kvp.Value.Dispose();

            _styles = FrozenDictionary<string, UIFontStyle>.Empty;

            _activeFontStyle = -1;

            EdInterop.MSDF_DestroyShapedGlyph(_shapedGlyph.Pointer);

            EdInterop.MSDF_DestroyVarData(_ft, _fontVars);
            EdInterop.MSDF_DestroyFont((MSDF_FontFace*)_fontFace);
            EdInterop.MSDF_ShutdownFt(_ft);

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
                return new UIShapedGlyph(new Vector2((float)shapedGlyph->BearingX, (float)shapedGlyph->BearingY), new Vector2((float)shapedGlyph->Width, (float)shapedGlyph->Height), (float)shapedGlyph->Advance);
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
    }

    public sealed class UIFontStyle : IDisposable
    {
        private readonly UIFontAsset _assetDef;
        private readonly UIFontAssetData _assetData;

        private string _styleName;
        private int _index;

        private float _spaceAdvance;
        private float _tabAdvance;
        private float _lineHeight;

        private Dictionary<char, UIGlyph> _glyphs;
        private List<UIGlyphSpace> _spaces;

        private Vector2 _atlasSize;
        private RHITexture? _atlasTexture;

        private Queue<char> _unrenderedGlyphs;

        private bool _disposedValue;

        internal UIFontStyle(UIFontAsset asset, UIFontAssetData assetData, string styleName, int index, float spaceAdvance, float tabAdvance, float lineHeight)
        {
            _assetDef = asset;
            _assetData = assetData;

            _styleName = styleName;
            _index = index;

            _spaceAdvance = spaceAdvance;
            _tabAdvance = tabAdvance;
            _lineHeight = lineHeight;

            _glyphs = new Dictionary<char, UIGlyph>();
            _spaces = new List<UIGlyphSpace>();

            _atlasSize = Vector2.Zero;
            _atlasTexture = null;

            _unrenderedGlyphs = new Queue<char>();
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
            if (_glyphs.TryGetValue(c, out UIGlyph glyph))
                return glyph;

            glyph = GenerateNewGlyph(c);
            _glyphs.Add(c, glyph);

            return glyph;
        }

        private UIGlyph GenerateNewGlyph(char c)
        {
            UIShapedGlyph? metricsNullable = _assetData.ShapeNewGlyph(this, c);
            if (!metricsNullable.HasValue)
                return default;

            UIShapedGlyph metrics = metricsNullable.Value;

            _unrenderedGlyphs.Enqueue(c);
            UIManager.Instance.FontManager.AddFontToUpdateSet(this);

            return new UIGlyph(metrics.Offset, metrics.Size, Boundaries.Zero, metrics.Advance);
        }

        public UIFontAsset Font => _assetDef;
        internal UIFontAssetData FontData => _assetData;

        public string StyleName => _styleName;
        public int Index => _index;

        public float SpaceAdvance => _spaceAdvance;
        public float TabAdvance => _tabAdvance;
        public float LineHeight => _lineHeight;

        internal List<UIGlyphSpace> Spaces => _spaces;

        internal Vector2 AtlasSize { get => _atlasSize; set => _atlasSize = value; }
        internal RHITexture? AtlasTexture { get => _atlasTexture; set => _atlasTexture = value; }

        internal Queue<char> UnrenderedGlyphs => _unrenderedGlyphs;
    }

    public readonly record struct UIGlyph(Vector2 Offset, Vector2 Size, Boundaries AtlasUVs, float Advance);

    internal struct UIGlyphSpace
    {
        public Vector2 OffsetFromOrigin;
        public Vector2 SpaceExtents;
        public Vector2 CurrentOffset;
        public float MaxGlyphHeightOnLine;
    }

    internal readonly record struct UIShapedGlyph(Vector2 Offset, Vector2 Size, float Advance);
}
