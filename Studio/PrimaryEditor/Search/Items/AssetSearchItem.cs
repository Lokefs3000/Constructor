using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using PrimaryEditor.Search.Providers;

namespace PrimaryEditor.Search.Items
{
    public sealed class AssetSearchItem : SearchItem
    {
        private Type? _assetType;
        private string? _filePath;
        private AssetId _assetId;

        public AssetSearchItem()
        {
            _assetType = null;
            _filePath = null;
            _assetId = AssetId.Invalid;
        }

        internal void SetupItem(Type assetType, string filePath, AssetId assetId)
        {
            _sourceType = typeof(AssetSearchProvider);
            _assetType = assetType;
            _filePath = filePath;
            _assetId = assetId;
        }

        protected internal override void ClearForPoolReturn()
        {
            base.ClearForPoolReturn();
            _assetType = null;
            _filePath = null;
            _assetId = AssetId.Invalid;
        }

        public Type AssetType => _assetType!;
        public string FilePath => _filePath!;
        public AssetId AssetId => _assetId;
    }
}
