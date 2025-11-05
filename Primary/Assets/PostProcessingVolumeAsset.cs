using Primary.Rendering.PostProcessing;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Vortice.DXGI;

namespace Primary.Assets
{
    public sealed class PostProcessingVolumeAsset : IAssetDefinition
    {
        private readonly PPVolumeAssetData _assetData;

        internal PostProcessingVolumeAsset(PPVolumeAssetData assetData)
        {
            _assetData = assetData;
        }

        /// <summary>Not thread-safe</summary>
        public void AddEffect<T>() where T : IPostProcessingData, new()
        {
            if (!_assetData.Effects.Exists((x) => x is T))
            {
                _assetData.Effects.Add(new T());
            }
        }

        /// <summary>Not thread-safe</summary>
        public void RemoveEffect<T>() where T : IPostProcessingData, new()
        {
            _assetData.Effects.RemoveWhere((x) => x is T);
        }

        /// <summary>Not thread-safe</summary>
        public void MoveEffect<T>(int newIndex) where T : IPostProcessingData, new()
        {
            if ((uint)newIndex >= _assetData.Effects.Count)
                return;

            int index = _assetData.Effects.FindIndex((x) => x is T);
            if (index == newIndex)
                return;

            IPostProcessingData old = _assetData.Effects[index];
            IPostProcessingData @new = _assetData.Effects[newIndex];

            _assetData.Effects[index] = old;
            _assetData.Effects[newIndex] = @new;
        }

        internal PPVolumeAssetData AssetData => _assetData;

        public ResourceStatus Status => _assetData.Status;

        public string Name => _assetData.Name;
        public AssetId Id => _assetData.Id;

        public IReadOnlyList<IPostProcessingData> Effects => _assetData.Effects;
    }

    internal sealed class PPVolumeAssetData : IInternalAssetData
    {
        private readonly WeakReference _asset;

        private ResourceStatus _status;

        private readonly AssetId _id;
        private string _name;

        private List<IPostProcessingData> _effectData;

        internal PPVolumeAssetData(AssetId id)
        {
            _asset = new WeakReference(null);

            _status = ResourceStatus.Pending;

            _id = id;
            _name = string.Empty;

            _effectData = new List<IPostProcessingData>();
        }

        public void Dispose()
        {
            _status = ResourceStatus.Disposed;

            _asset.Target = null;

            _effectData.Clear();
        }
        public void SetAssetInternalStatus(ResourceStatus status)
        {
            _status = status;
        }

        public void SetAssetInternalName(string name)
        {
            _name = name;
        }

        internal void UpdateAssetData(PostProcessingVolumeAsset asset, List<IPostProcessingData> effectData)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Success;

            _effectData = effectData;
        }

        internal void UpdateAssetFailed(PostProcessingVolumeAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;

            _effectData.Clear();
        }

        internal ResourceStatus Status => _status;

        internal AssetId Id => _id;
        internal string Name => _name;

        internal List<IPostProcessingData> Effects => _effectData;

        public Type AssetType => typeof(PostProcessingVolumeAsset);
        public IAssetDefinition? Definition => Unsafe.As<IAssetDefinition>(_asset.Target);
    }
}
