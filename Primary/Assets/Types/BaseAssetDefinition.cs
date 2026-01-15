using System.Runtime.CompilerServices;

namespace Primary.Assets.Types
{
    public abstract class BaseAssetDefinition<TSelf, T> : IAssetDefinition where TSelf : class, IAssetDefinition where T : class, IInternalAssetData
    {
        private readonly T _assetData;

        protected BaseAssetDefinition(T assetData)
        {
            _assetData = assetData;
        }

        public TSelf WaitIfNotLoaded()
        {
            if (!IsLoaded)
                AssetManager.WaitForAssetLoad(_assetData.Id);
            return Unsafe.As<TSelf>(this);
        }

        internal T InternalAssetData => _assetData;
        protected T AssetData => _assetData;

        public AssetId Id => _assetData.Id;
        public ResourceStatus Status => _assetData.Status;
        public string Name => _assetData.Name;

        public bool IsLoaded => _assetData.Status > ResourceStatus.Running;

        public int LoadIndex => _assetData.LoadIndex;
    }
}
