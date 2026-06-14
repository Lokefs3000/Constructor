using Primary.Assets.Types;
using Primary.Collections.ReadOnly;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace Primary.Assets
{
    public sealed class TextureAtlasAsset : BaseAssetDefinition<TextureAtlasAsset, TextureAtlasAssetData>
    {
        public TextureAtlasAsset(TextureAtlasAssetData assetData) : base(assetData)
        {
        }

        public Sprite? TryFindSpriteOrNull(string name)
        {
            if (IsLoaded)
            {
                foreach (Sprite spr in AssetData.Sprites)
                {
                    if (spr.Name == name)
                        return spr;
                }
            }

            return null;
        }

        public bool TryGetSprite(string? name, [NotNullWhen(true)] out Sprite? sprite)
        {
            if (IsLoaded)
            {
                foreach (Sprite spr in AssetData.Sprites)
                {
                    if (spr.Name == name)
                    {
                        sprite = spr;
                        return true;
                    }
                }
            }

            sprite = null;
            return false;
        }

        public ImmutableArray<Sprite> Sprites => IsLoaded ? AssetData.Sprites : [];
    }

    public sealed class TextureAtlasAssetData : BaseInternalAssetData<TextureAtlasAsset>
    {
        private ImmutableArray<Sprite> _sprites;

        public TextureAtlasAssetData(AssetId id) : base(id)
        {
            _sprites = [];
        }

        public override void Dispose()
        {
            base.Dispose();

            _sprites = [];
        }

        public void UpdateAssetData(TextureAtlasAsset asset, ImmutableArray<Sprite> sprites)
        {
            base.UpdateAssetData(asset);

            _sprites = sprites;
        }

        internal ImmutableArray<Sprite> Sprites => _sprites;
    }

    public sealed class Sprite
    {
        private readonly TextureAtlasAsset? _owningAsset;

        private TextureAsset _texture;
        private string _name;
        private Rect _sourceRect;

        private Vector2 _uvMin;
        private Vector2 _uvMax;

        internal Sprite(TextureAtlasAsset? owningAsset, TextureAsset texture, string name, Rect sourceRect)
        {
            _owningAsset = owningAsset;

            _texture = texture;
            _name = name;
            _sourceRect = sourceRect;

            if (texture.IsLoaded)
                RecalculateUVs();

            AssetManager.ListenForAssetLoad(texture, this, AssetLoadedCallback);
        }

        public Sprite(TextureAsset texture, string name, Rect sourceRect) : this(null, texture, name, sourceRect)
        {
        }

        private void AssetLoadedCallback(TextureAsset asset, bool wasReloaded)
        {
            if (_texture != asset)
            {
                EngLog.Assets.Debug("Unexpected sprite texture in load callback {got} expected {expected}", asset, _texture);
                return;
            }

            RecalculateUVs();
        }

        private void RecalculateUVs()
        {
            Vector2 textureSize = new Vector2(_texture.Width, _texture.Height);

            _uvMin = _sourceRect.Position.AsVector2() / textureSize;
            _uvMax = _sourceRect.Size.AsVector2() / textureSize + _uvMin;
        }

        public TextureAtlasAsset? OwningAtlas => _owningAsset;

        public TextureAsset Texture
        {
            get => _texture;
            set
            {
                if (_owningAsset == null && _texture != value)
                {
                    _texture = value;

                    if (value.IsLoaded)
                        RecalculateUVs();
                    AssetManager.ListenForAssetLoad(value, this, AssetLoadedCallback);
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_owningAsset == null)
                    _name = value;
            }
        }

        public Rect SourceRect
        {
            get => _sourceRect;
            set
            {
                if (_owningAsset == null && _sourceRect != value)
                {
                    _sourceRect = value;
                    RecalculateUVs();
                }
            }
        }

        public Vector2 UVMin => _uvMin;
        public Vector2 UVMax => _uvMax;
    }
}
