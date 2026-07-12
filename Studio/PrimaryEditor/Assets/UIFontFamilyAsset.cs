using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using EditorUI.Assets;
using Primary.Assets.Types;
using TerraFX.Interop.Gdiplus;

namespace PrimaryEditor.Assets
{
    public sealed class UIFontFamilyAsset : BaseAssetDefinition<UIFontFamilyAsset, UIFontFamilyData>, IAssetProvider<FontFamily>
    {
        public UIFontFamilyAsset(UIFontFamilyData assetData) : base(assetData)
        {
        }

        public bool IsReadyToUse => IsLoaded;
        public FontFamily? Value => IsLoaded ? AssetData.FontFamily : null;
    }

    public sealed class UIFontFamilyData : BaseInternalAssetData<UIFontFamilyAsset>
    {
        private FontFamily? _fontFamily;

        public UIFontFamilyData(AssetId id) : base(id)
        {
            _fontFamily = null;
        }

        public override void Dispose()
        {
            base.Dispose();

            _fontFamily?.Dispose();
            _fontFamily = null;
        }

        public void UpdateAssetData(UIFontFamilyAsset asset, FontFamily fontFamily)
        {
            _fontFamily = fontFamily;

            base.UpdateAssetData(asset);
        }

        public override void UpdateAssetFailed(UIFontFamilyAsset asset)
        {
            Debug.Assert(_fontFamily == null);
            _fontFamily = null;

            base.UpdateAssetFailed(asset);
        }

        internal FontFamily? FontFamily => _fontFamily;
    }
}
