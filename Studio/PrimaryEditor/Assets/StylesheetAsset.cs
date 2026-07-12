using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Styling;
using Primary.Assets.Types;

namespace PrimaryEditor.Assets
{
    public sealed class StylesheetAsset : BaseAssetDefinition<StylesheetAsset, StylesheetData>
    {
        public StylesheetAsset(StylesheetData assetData) : base(assetData)
        {
        }

        public Stylesheet? Stylesheet => Status == ResourceStatus.Success ? AssetData.Stylesheet : null;
    }

    public sealed class StylesheetData : BaseInternalAssetData<StylesheetAsset>
    {
        private Stylesheet? _stylesheet;

        public StylesheetData(AssetId id) : base(id)
        {
            _stylesheet = null;
        }

        public override void Dispose()
        {
            _stylesheet = null;

            base.Dispose();
        }

        public void UpdateAssetData(StylesheetAsset asset, Stylesheet stylesheet)
        {
            _stylesheet = stylesheet;

            base.UpdateAssetData(asset);
        }

        public override void UpdateAssetFailed(StylesheetAsset asset)
        {
            _stylesheet = null;

            base.UpdateAssetFailed(asset);
        }

        public Stylesheet? Stylesheet => _stylesheet;
    }
}
