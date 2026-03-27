using CommunityToolkit.HighPerformance;
using Editor.UI.Menu;
using Primary.Assets.Types;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Assets
{
    public sealed class ContextMenuAsset : BaseAssetDefinition<ContextMenuAsset, ContextMenuAssetData>
    {
        private UIFontStyle? _cachedStyle;

        public ContextMenuAsset(ContextMenuAssetData assetData) : base(assetData)
        {
            _cachedStyle = null;
        }

        public void AddItem(ContextMenuBase item)
        {
            if (AssetData.Status != ResourceStatus.Success)
                return;
            AssetData.AddItem(item);
        }

        public void RemoveItem(ContextMenuBase item)
        {
            if (AssetData.Status != ResourceStatus.Success)
                return;
            AssetData.RemoveItem(item);
        }

        public ReadOnlySpan<ContextMenuBase> Items => Status == ResourceStatus.Success ? AssetData.Items : ReadOnlySpan<ContextMenuBase>.Empty;

        public UIFontStyle? Style
        {
            get
            {
                if (Status != ResourceStatus.Success)
                {
                    _cachedStyle = null;
                    return null;
                }

                if (_cachedStyle == null)
                    _cachedStyle = AssetData.FontAsset?.FindStyle(AssetData.StyleName);

                return _cachedStyle;
            }
        }
    }

    public sealed class ContextMenuAssetData : BaseInternalAssetData<ContextMenuAsset>
    {
        private UIFontAsset? _font;
        private string? _styleName;

        private List<ContextMenuBase>? _items;

        public ContextMenuAssetData(AssetId id) : base(id)
        {
            _items = null;

            _font = null;
            _styleName = null;
        }

        public override void Dispose()
        {
            if (_items != null)
            {
                foreach (ContextMenuBase item in _items)
                {
                    item.ChangeOwner(null);
                }
            }

            _items = null;

            _font = null;
            _styleName = null;

            base.Dispose();
        }

        public void UpdateAssetData(ContextMenuAsset asset, UIFontAsset fontAsset, string styleName, List<ContextMenuBase> items)
        {
            base.UpdateAssetData(asset);

            _font = fontAsset;
            _styleName = styleName;

            _items = items;
        }

        public override void UpdateAssetFailed(ContextMenuAsset asset)
        {
            base.UpdateAssetFailed(asset);

            _font = null;
            _styleName = null;

            _items = null;
        }

        internal void AddItem(ContextMenuBase item)
        {
            item.ChangeOwner(Definition);
            _items?.AddUnique(item);
        }

        internal void RemoveItem(ContextMenuBase item)
        {
            if (item.Owner == Definition)
                item.ChangeOwner(null);
            _items?.Remove(item);
        }

        internal UIFontAsset? FontAsset => _font;
        internal string? StyleName => _styleName;

        internal ReadOnlySpan<ContextMenuBase> Items => _items == null ? ReadOnlySpan<ContextMenuBase>.Empty : _items.AsSpan();
    }
}
