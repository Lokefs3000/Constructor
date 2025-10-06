using Primary.Rendering;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Assets
{
    public sealed class MaterialAsset : IAssetDefinition
    {
        private readonly MaterialAssetData _assetData;

        internal MaterialAsset(MaterialAssetData assetData)
        {
            _assetData = assetData;
        }

        internal MaterialAssetData AssetData => _assetData;

        public ShaderAsset? Shader => _assetData.TargetShader;

        internal nint Handle => _assetData.Handle;

        public ResourceStatus Status => _assetData.Status;

        public string Name => _assetData.Name;
        public AssetId Id => _assetData.Id;

        internal ShaderBindGroup? BindGroup => _assetData.BindGroup;
    }

    internal sealed class MaterialAssetData : IInternalAssetData
    {
        private readonly WeakReference _asset;

        private ResourceStatus _status;

        private readonly AssetId _id;
        private string _name;

        private ShaderAsset? _targetShader;

        //TODO: add native block of memory to use as cbuffer

        private FrozenDictionary<string, MaterialProperty> _properties;
        private ShaderBindGroup? _bindGroup;

        private GCHandle _gc;
        private nint _handle;

        internal MaterialAssetData(AssetId id)
        {
            _asset = new WeakReference(null);

            _status = ResourceStatus.Pending;

            _id = id;
            _name = string.Empty;

            _targetShader = null;

            _properties = FrozenDictionary<string, MaterialProperty>.Empty;
            _bindGroup = null;

            _gc = GCHandle.Alloc(null);
            _handle = nint.Zero;
        }

        public void Dispose()
        {
            _status = ResourceStatus.Disposed;

            _asset.Target = null;

            _targetShader = null;

            _properties = FrozenDictionary<string, MaterialProperty>.Empty;
            _bindGroup = null;

            _gc.Free();
            _handle = nint.Zero;
        }

        public void SetAssetInternalStatus(ResourceStatus status)
        {
            _status = status;
        }

        public void SetAssetInternalName(string name)
        {
            _name = name;
        }

        internal void UpdateAssetData(MaterialAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Success;

            _gc = GCHandle.Alloc(asset, GCHandleType.Normal);
            _handle = GCHandle.ToIntPtr(_gc);
        }

        internal void UpdateAssetFailed(MaterialAsset asset)
        {
            _asset.Target = asset;

            _status = ResourceStatus.Error;

            _targetShader = null;
        }

        internal void ChangeCurrentShader(ShaderAsset? newShader, FrozenDictionary<string, MaterialProperty> properties, ShaderBindGroup? bindGroup)
        {
            if (newShader == _targetShader)
                return;

            _targetShader = newShader;

            _properties = properties;
            _bindGroup = bindGroup;

            /*Span<ShaderResourceSignature.Resource> resources = resourceSignature.Resources;
            for (int i = 0; i < resources.Length; i++)
            {
                ref ShaderResourceSignature.Resource res = ref resources[i];

                _properties.Add(res.PropertyName, new MaterialProperty
                {
                    
                });
            }*/
        }

        internal ResourceStatus Status => _status;

        internal AssetId Id => _id;
        internal string Name => _name;

        internal ShaderAsset? TargetShader => _targetShader;

        internal nint Handle => _handle;

        internal ShaderBindGroup? BindGroup => _bindGroup;

        public Type AssetType => typeof(MaterialAsset);
        public IAssetDefinition? Definition => Unsafe.As<IAssetDefinition>(_asset.Target);
    }

    internal record struct MaterialProperty
    {
        public MaterialVariableType Type;
        public string VariableName;
    }

    internal enum MaterialVariableType : byte
    {
        Texture
    }
}
