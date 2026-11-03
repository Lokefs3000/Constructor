using System.Numerics;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.RHI;
using Primary.Threading;
using static Primary.Rendering.Resources.FGResourceDesc;

namespace Primary.Assets
{
    public sealed class MaterialAsset : BaseAssetDefinition<MaterialAsset, MaterialAssetData>
    {
        internal MaterialAsset(MaterialAssetData assetData) : base(assetData)
        {
        }

        #region Resources

        #region Set
        /// <inheritdoc cref="PropertyBlock.SetResource(string, RHIBuffer)">
        public void SetResource(string id, RHIBuffer buffer)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetResource(id, buffer);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetResource(string, RHIBuffer)">
        public void SetResource(MaterialShaderType shaderType, string id, RHIBuffer buffer) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetResource(id, buffer);

        /// <inheritdoc cref="PropertyBlock.SetResource(string, RHITexture)">
        public void SetResource(string id, RHITexture texture)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetResource(id, texture);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetResource(string, RHITexture)">
        public void SetResource(MaterialShaderType shaderType, string id, RHITexture texture) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetResource(id, texture);

        /// <inheritdoc cref="PropertyBlock.SetResource(string, TextureAsset)">
        public void SetResource(string id, TextureAsset texture)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetResource(id, texture);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetResource(string, TextureAsset)">
        public void SetResource(MaterialShaderType shaderType, string id, TextureAsset texture) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetResource(id, texture);
        #endregion

        #region Get
        /// <inheritdoc cref="PropertyBlock.SetResource(string, RHIBuffer)">
        public RHIBuffer? GetRHIBuffer(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetRHIBuffer(id);
        /// <inheritdoc cref="PropertyBlock.SetResource(string, RHITexture)">
        public RHITexture? GetRHITexture(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetRHITexture(id);
        /// <inheritdoc cref="PropertyBlock.SetResource(string, TextureAsset)">
        public TextureAsset? GetTextureAsset(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetTextureAsset(id);
        #endregion

        /// <inheritdoc cref="PropertyBlock.ClearResource(string)"/>
        public void ClearResource(string id)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.ClearResource(id);
            }
        }

        /// <inheritdoc cref="PropertyBlock.ClearResource(string)"/>
        public void ClearResource(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.ClearResource(id);

        #endregion

        #region Properties

        #region Set
        /// <inheritdoc cref="PropertyBlock.SetSingle(string, float)">
        public void SetSingle(string id, float value)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetSingle(id, value);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetSingle(string, float)">
        public void SetSingle(MaterialShaderType shaderType, string id, float value) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetSingle(id, value);

        /// <inheritdoc cref="PropertyBlock.SetDouble(string, double)">
        public void SetDouble(string id, double value)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetDouble(id, value);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetDouble(string, double)">
        public void SetDouble(MaterialShaderType shaderType, string id, double value) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetDouble(id, value);

        /// <inheritdoc cref="PropertyBlock.SetUInt(string, uint)">
        public void SetUInt(string id, uint value)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetUInt(id, value);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetUInt(string, uint)">
        public void SetUInt(MaterialShaderType shaderType, string id, uint value) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetUInt(id, value);

        /// <inheritdoc cref="PropertyBlock.SetInt(string, int)">
        public void SetInt(string id, int value)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetInt(id, value);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetInt(string, int)">
        public void SetInt(MaterialShaderType shaderType, string id, int value) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetInt(id, value);

        /// <inheritdoc cref="PropertyBlock.SetVector2(string, System.Numerics.Vector2)">
        public void SetVector2(string id, Vector2 value)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetVector2(id, value);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetVector2(string, System.Numerics.Vector2)">
        public void SetVector2(MaterialShaderType shaderType, string id, Vector2 value) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetVector2(id, value);

        /// <inheritdoc cref="PropertyBlock.SetVector3(string, System.Numerics.Vector3)">
        public void SetVector3(string id, Vector3 value)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetVector3(id, value);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetVector3(string, System.Numerics.Vector3)">
        public void SetVector3(MaterialShaderType shaderType, string id, Vector3 value) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetVector3(id, value);

        /// <inheritdoc cref="PropertyBlock.SetVector4(string, System.Numerics.Vector4)">
        public void SetVector4(string id, Vector4 value)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetVector4(id, value);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetVector4(string, System.Numerics.Vector4)">
        public void SetVector4(MaterialShaderType shaderType, string id, Vector4 value) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetVector4(id, value);

        /// <inheritdoc cref="PropertyBlock.SetMatrix4x4(string, Matrix4x4)">
        public void SetMatrix4x4(string id, Matrix4x4 value)
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetMatrix4x4(id, value);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetMatrix4x4(string, Matrix4x4)">
        public void SetMatrix4x4(MaterialShaderType shaderType, string id, Matrix4x4 value) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetMatrix4x4(id, value);

        /// <inheritdoc cref="PropertyBlock.SetStruct{T}(string, T)">
        public void SetStruct<T>(string id, T value) where T : unmanaged
        {
            foreach (MaterialShader shader in AssetData.Shaders)
            {
                shader.Block?.SetStruct(id, value);
            }
        }
        /// <inheritdoc cref="PropertyBlock.SetStruct{T}(string, T)">
        public void SetStruct<T>(MaterialShaderType shaderType, string id, T value) where T : unmanaged => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.SetStruct(id, value);
        #endregion

        #region Get
        /// <inheritdoc cref="PropertyBlock.GetSingle(int)">
        public float GetSingle(MaterialShaderType shaderType,  string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetSingle(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetDouble(int)">
        public double GetDouble(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetDouble(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetUInt(int)">
        public uint GetUInt(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetUInt(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetInt(int)">
        public int GetInt(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetInt(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector2(int)">
        public Vector2 GetVector2(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetVector2(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector3(int)">
        public Vector3 GetVector3(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetVector3(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector4(int)">
        public Vector4 GetVector4(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetVector4(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetMatrix4x4(int)">
        public Matrix4x4 GetMatrix4x4(MaterialShaderType shaderType, string id) => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetMatrix4x4(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetStruct{T}(int)">
        public T GetStruct<T>(MaterialShaderType shaderType, string id) where T : unmanaged => AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block?.GetStruct<T>(id) ?? default;
        #endregion

        #endregion

        public ShaderAsset? GetShaderOrNull(MaterialShaderType shaderType)
        {
            if (!IsReady)
                return null;
            return AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Shader;
        }

        public ROPropertyBlock GetPropertyBlockOrNull(MaterialShaderType shaderType)
        {
            if (!IsReady)
                return null;
            return AssetData.Shaders.DangerousGetReferenceAt((int)shaderType).Block;
        }

        public ShaderSetAsset? ShaderSet { get => AssetData.ShaderSet; set => AssetData.ChangeActiveShaderSet(NullableUtility.ThrowIfNull(value)); }
    }

    public sealed class MaterialAssetData : BaseInternalAssetData<MaterialAsset>
    {
        private ShaderSetAsset? _shaderSet;

        private MaterialShader[] _shaderTypes;

        internal MaterialAssetData(AssetId id) : base(id)
        {
            _shaderSet = null;

            _shaderTypes = new MaterialShader[s_shaderTypeCount];
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_shaderSet != null)
                AssetManager.ListenForAssetLoad(_shaderSet, this, null);

            foreach (MaterialShader shader in _shaderTypes)
            {
                shader.Dispose();
            }

            Array.Clear(_shaderTypes);
        }

        internal void ChangeActiveShaderSet(ShaderSetAsset newShaderSet)
        {
            if (_shaderSet == newShaderSet)
                return;

            if (_shaderSet != null)
                AssetManager.ListenForAssetLoad(_shaderSet, this, null);

            foreach (MaterialShader shader in _shaderTypes)
            {
                shader.Dispose();
            }

            Array.Clear(_shaderTypes);

            AssetManager.ListenForAssetLoad(newShaderSet, this, OnShaderSetLoaded);
            if (newShaderSet.IsReady)
            {
                ThreadHelper.ExecuteOnMainThread(() => OnShaderSetLoaded(newShaderSet, false));
            }
        }

        public void UpdateAssetData(MaterialAsset asset, ShaderSetAsset shaderSet, MaterialShader[] shaderTypes)
        {
            base.UpdateAssetData(asset);

            _shaderSet = shaderSet;
            _shaderTypes = shaderTypes;

            AssetManager.ListenForAssetLoad(shaderSet, this, OnShaderSetLoaded);
            if (shaderSet.IsReady)
            {
                ThreadHelper.ExecuteOnMainThread(() => OnShaderSetLoaded(shaderSet, false));
            }
        }

        private void OnShaderSetLoaded(ShaderSetAsset shaderSet, bool wasReloaded)
        {
            MaterialShaderType shaderType = 0;
            foreach (string name in Enum.GetNames<MaterialShaderType>())
            {
                ref MaterialShader currentVal = ref _shaderTypes[(int)shaderType++];

                if (shaderSet.TryGetShader(name, out ShaderAsset? shader))
                {
                    if (currentVal.Shader != shader)
                    {
                        if (currentVal.Block != null)
                        {
                            currentVal.Block.Reload(currentVal.Shader);
                        }
                        else
                        {
                            currentVal = new MaterialShader(shader, shader.CreatePropertyBlock());
                        }
                    }
                }
                else
                {
                    currentVal.Block?.Dispose();
                    _shaderTypes[(int)shaderType++] = default;
                }
            }
        }

        internal ShaderSetAsset? ShaderSet => _shaderSet;
        internal MaterialShader[] Shaders => _shaderTypes;

        private static readonly int s_shaderTypeCount = Enum.GetValues<MaterialShaderType>().Length;
    }

    public readonly record struct MaterialShader(ShaderAsset Shader, PropertyBlock Block) : IDisposable
    {
        public void Dispose()
        {
            Block?.Dispose();
        }
    }

    public enum MaterialShaderType : byte
    {
        Default = 0,
        Shadow
    }
}
