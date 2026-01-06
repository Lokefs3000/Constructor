using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Assets.Types
{
    public abstract class BaseAssetDefinition<T> : IAssetDefinition where T : class, IInternalAssetData
    {
        private readonly T _assetData;

        protected BaseAssetDefinition(T assetData)
        {
            _assetData = assetData;
        }

        internal T InternalAssetData => _assetData;
        protected T AssetData => _assetData;

        public AssetId Id => _assetData.Id;
        public ResourceStatus Status => _assetData.Status;
        public string Name => _assetData.Name;

        public int LoadIndex => _assetData.LoadIndex;
    }
}
