using Primary.Assets.Types;
using Primary.Rendering;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Primary.Assets
{
    public sealed class MaterialAsset : IAssetDefinition
    {
        private readonly MaterialAssetData _assetData;

        internal MaterialAsset(MaterialAssetData assetData)
        {
            _assetData = assetData;
        }

        public object? GetResource(string path) => _assetData.GetResource(path);
        public void SetResource(string path, object? resource) => _assetData.SetResource(path, resource);

        internal MaterialAssetData AssetData => _assetData;

        public ShaderAsset? Shader { get => _assetData.TargetShader; set => _assetData.ChangeCurrentShader(value, null); }
        public IReadOnlyDictionary<string, MaterialProperty> Properties => _assetData.Properties;

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

        internal void ChangeCurrentShader(ShaderAsset? newShader, ShaderBindGroup? bindGroup)
        {
            if (newShader == _targetShader)
                return;

            _targetShader = newShader;

            if (newShader != null)
            {
                Dictionary<string, MaterialProperty> properties = new Dictionary<string, MaterialProperty>();
                ShaderBindGroup newBindGroup = bindGroup ?? newShader.CreateDefaultBindGroup();

                foreach (ShaderVariable variable in newShader.Variables.Values)
                {
                    if (variable.BindGroup == newBindGroup.GroupName)
                    {
                        int idx = Array.FindIndex(variable.Attributes, (x) => x.Type == ShaderAttributeType.Property);
                        if (idx >= 0)
                        {
                            ref ShaderVariableAttribute attribute = ref variable.Attributes[idx];
                            ShaderVariableAttribProperty property = (ShaderVariableAttribProperty)attribute.Value!;

                            properties.Add(property.Name, new MaterialProperty
                            {
                                Type = variable.Type switch
                                {
                                    ShaderVariableType.Texture1D => MaterialVariableType.Texture,
                                    ShaderVariableType.Texture2D => MaterialVariableType.Texture,
                                    ShaderVariableType.Texture3D => MaterialVariableType.Texture,
                                    ShaderVariableType.TextureCube => MaterialVariableType.Texture,
                                    _ => throw new NotSupportedException(variable.Type.ToString())
                                },
                                VariableName = variable.Name,
                            });

                            if (bindGroup == null)
                            {
                                newBindGroup.SetResource(variable.Name, _bindGroup?.GetResource(variable.Name) ?? property.Default switch
                                {
                                    ShaderPropertyDefault.White => AssetManager.Static.DefaultWhite,
                                    ShaderPropertyDefault.Normal => AssetManager.Static.DefaultNormal,
                                    _ => AssetManager.Static.DefaultWhite
                                });
                            }
                        }
                    }
                }

                _properties = properties.ToFrozenDictionary();
                _bindGroup = newBindGroup;
            }
            else
            {
                _properties = FrozenDictionary<string, MaterialProperty>.Empty;
                _bindGroup = null;
            }

            /*Span<ShaderResourceSignature.Resource> resources = resourceSignature.Resources;
            for (int i = 0; i < resources.Length; i++)
            {
                ref ShaderResourceSignature.Resource res = ref resources[i];

                _properties.Add(res.PropertyName, new MaterialProperty
                {
                    
                });
            }*/
        }

        internal object? GetResource(string path)
        {
            ref readonly MaterialProperty property = ref _properties.GetValueRefOrNullRef(path);
            if (Unsafe.IsNullRef(in property))
                return null;

            return _bindGroup?.GetResource(property.VariableName);
        }

        internal void SetResource(string path, object? resource)
        {
            ref readonly MaterialProperty property = ref _properties.GetValueRefOrNullRef(path);
            if (Unsafe.IsNullRef(in property))
                return;

            if (resource == null)
            {
                _bindGroup?.SetResource(property.VariableName, GetDefault(property.Default));
            }
            else
            {
                switch (property.Type)
                {
                    case MaterialVariableType.Texture: _bindGroup?.SetResource(property.VariableName, resource as TextureAsset ?? GetDefault(property.Default)); break;
                }
            }

            static TextureAsset GetDefault(ShaderPropertyDefault @default)
            {
                switch (@default)
                {
                    case ShaderPropertyDefault.White: return AssetManager.Static.DefaultWhite;
                    case ShaderPropertyDefault.Normal: return AssetManager.Static.DefaultNormal;
                }

                throw new NotSupportedException(@default.ToString());
            }
        }

        internal ResourceStatus Status => _status;

        internal AssetId Id => _id;
        internal string Name => _name;

        internal ShaderAsset? TargetShader => _targetShader;
        internal IReadOnlyDictionary<string, MaterialProperty> Properties => _properties;

        internal nint Handle => _handle;

        internal ShaderBindGroup? BindGroup => _bindGroup;

        public Type AssetType => typeof(MaterialAsset);
        public IAssetDefinition? Definition => Unsafe.As<IAssetDefinition>(_asset.Target);

        AssetId IInternalAssetData.Id => Id;
        ResourceStatus IInternalAssetData.Status => Status;
        string IInternalAssetData.Name => Name;
    }

    public record struct MaterialProperty
    {
        public MaterialVariableType Type;
        public string VariableName;
        public ShaderPropertyDefault Default;
    }

    public enum MaterialVariableType : byte
    {
        Texture
    }
}
