using CommunityToolkit.HighPerformance;
using Editor.UI.Menu;
using Primary.Assets.Types;
using Primary.Collections.ReadOnly;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Editor.UI.Assets
{
    public sealed class ContextMenuAsset : BaseAssetDefinition<ContextMenuAsset, ContextMenuAssetData>
    {
        public ContextMenuAsset(ContextMenuAssetData assetData) : base(assetData)
        {
            
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

        public ROList<ContextMenuBase> Items => IsLoaded ? AssetData.Items : ROList<ContextMenuBase>.Empty;
    }

    public sealed class ContextMenuAssetData : BaseInternalAssetData<ContextMenuAsset>
    {
        private List<ContextMenuBase>? _items;

        public ContextMenuAssetData(AssetId id) : base(id)
        {
            _items = null;
        }

        public override void Dispose()
        {
            _items = null;

            base.Dispose();
        }

        public void UpdateAssetData(ContextMenuAsset asset, List<ContextMenuBase> items)
        {
            base.UpdateAssetData(asset);

            _items = items;
        }

        public override void UpdateAssetFailed(ContextMenuAsset asset)
        {
            base.UpdateAssetFailed(asset);

            _items = null;
        }

        internal void AddItem(ContextMenuBase item)
        {
            item.SetOwner(null);
            _items?.AddUnique(item);
        }

        internal void RemoveItem(ContextMenuBase item)
        {
            item.SetOwner(null);
            _items?.Remove(item);
        }

        internal ROList<ContextMenuBase> Items => _items ?? ROList<ContextMenuBase>.Empty;
    }
}
