using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Assets.Types
{
    public abstract class BaseInternalAssetData<T> : IInternalAssetData where T : class, IAssetDefinition
    {
        private readonly WeakReference _asset;
        private readonly AssetId _id;

        private ResourceStatus _status;
        private string _name;

        protected BaseInternalAssetData(AssetId id)
        {
            _asset = new WeakReference(id);
            _id = id;

            _status = ResourceStatus.Pending;
            _name = string.Empty;
        }

        public void Dispose()
        {
            _asset.Target = null;

            _status = ResourceStatus.Disposed;
            _name = string.Empty;
        }

        public virtual void UpdateAssetData(T asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Success;
        }

        public virtual void UpdateAssetFailed(T asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;
        }

        public virtual void SetAssetInternalName(string name) => _name = name;
        public virtual void SetAssetInternalStatus(ResourceStatus status) => _status = status;

        public AssetId Id => _id;
        public IAssetDefinition? Definition => Unsafe.As<T>(_asset.Target);

        public ResourceStatus Status => _status;
        public string Name => _name;

        public Type AssetType => typeof(T);
    }
}
