using Primary.Assets.Types;
using Primary.Rendering.Assets;
using Primary.RHI2;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using TerraFX.Interop.Windows;

namespace Primary.Assets
{
    public sealed class ComputeShaderAsset : BaseAssetDefinition<ComputeShaderAsset, ComputeShaderAssetData>
    {
        public ComputeShaderAsset(ComputeShaderAssetData assetData) : base(assetData)
        {
        }

        public bool TryFindKernel(string kernelName, [NotNullWhen(true)] out ComputeShaderKernel? kernel)
        {
            if (Status != ResourceStatus.Success)
            {
                kernel = null;
                return false;
            }

            return AssetData.Kernels.TryGetValue(kernelName, out kernel);
        }

        public PropertyBlock? CreatePropertyBlock(string kernelName)
        {
            if (Status != ResourceStatus.Success)
                return null;
            if (AssetData.Kernels.TryGetValue(kernelName, out ComputeShaderKernel? kernel))
                return kernel.CreatePropertyBlock();

            return null;
        }

        public IReadOnlyDictionary<FastStringHash, ComputeShaderKernel> Kernels => AssetData.Kernels;
    }

    public sealed class ComputeShaderAssetData : BaseInternalAssetData<ComputeShaderAsset>
    {
        private FrozenDictionary<FastStringHash, ComputeShaderKernel> _kernels;

        public ComputeShaderAssetData(AssetId id) : base(id)
        {
            _kernels = FrozenDictionary<FastStringHash, ComputeShaderKernel>.Empty;
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var kvp in _kernels)
                kvp.Value.Pipeline.Dispose();

            _kernels = FrozenDictionary<FastStringHash, ComputeShaderKernel>.Empty;
        }

        public void UpdateAssetData(ComputeShaderAsset asset, FrozenDictionary<FastStringHash, ComputeShaderKernel> kernels)
        {
            base.UpdateAssetData(asset);

            _kernels = kernels;
        }

        internal IReadOnlyDictionary<FastStringHash, ComputeShaderKernel> Kernels => _kernels;
    }

    public record class ComputeShaderKernel : IShaderResourceSource
    {
        private readonly ComputeShaderAsset _asset;

        private readonly KernelThreadSize _threadSize;

        private readonly ShaderProperty[] _properties;
        private readonly FrozenDictionary<int, int> _remappingTable;

        private readonly int _propertyBlockSize;
        private readonly int _headerBlockSize;

        private readonly ShHeaderFlags _headerFlags;

        private readonly RHIComputePipeline _computePipeline;

        private readonly int _resourceCount;

        internal ComputeShaderKernel(ComputeShaderAsset asset, KernelThreadSize threadSize, ShaderProperty[] properties, FrozenDictionary<int, int> remappingTable, int propertyBlockSize, int headerBlockSize, ShHeaderFlags headerFlags, RHIComputePipeline computePipeline)
        {
            _asset = asset;

            _threadSize = threadSize;

            _properties = properties;
            _remappingTable = remappingTable;

            _propertyBlockSize = propertyBlockSize;
            _headerBlockSize = headerBlockSize;

            _headerFlags = headerFlags;

            _computePipeline = computePipeline;

            _resourceCount = 0;
            foreach (ref readonly ShaderProperty property in properties.AsSpan())
            {
                if (property.Type == ShPropertyType.Buffer || property.Type == ShPropertyType.Texture || property.Type == ShPropertyType.Sampler)
                    _resourceCount++;
            }
        }

        public PropertyBlock? CreatePropertyBlock()
        {
            return new PropertyBlock(this);
        }

        public int LoadIndex => _asset.LoadIndex;

        public KernelThreadSize ThreadSize => _threadSize;

        public ReadOnlySpan<ShaderProperty> Properties => _properties;
        public IReadOnlyDictionary<int, int> RemappingTable => _remappingTable;

        public int PropertyBlockSize => _propertyBlockSize;
        public int HeaderBlockSize => _headerBlockSize;

        public ShHeaderFlags HeaderFlags => _headerFlags;

        public RHIComputePipeline Pipeline => _computePipeline;

        public int ResourceCount => _resourceCount;
    }

    public readonly record struct KernelThreadSize(int X, int Y, int Z)
    {
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"{X}x{Y}x{Z}";
    }
}
