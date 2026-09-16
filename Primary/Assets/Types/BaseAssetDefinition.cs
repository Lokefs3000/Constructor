using Primary.Serialization.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Primary.Assets.Types
{
    public abstract class BaseAssetDefinition : IAssetDefinition
    {
        public abstract AssetId Id { get; }
        public abstract ResourceStatus Status { get; }
        public abstract string Name { get; }
    }

    [JsonConverter(typeof(AssetJsonConverter))]
    public abstract class BaseAssetDefinition<TSelf, T> : BaseAssetDefinition where TSelf : class, IAssetDefinition where T : class, IInternalAssetData
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

        public override AssetId Id => _assetData.Id;
        public override ResourceStatus Status => _assetData.Status;
        public override string Name => _assetData.Name;

        public bool IsLoaded => _assetData.Status > ResourceStatus.Running;

        public int LoadIndex => _assetData.LoadIndex;
    }
}
