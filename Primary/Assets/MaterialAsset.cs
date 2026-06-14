using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.RHI;
using System.Numerics;

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
        public void SetResource(string id, RHIBuffer buffer) => AssetData.PropertyBlock?.SetResource(id, buffer);
        /// <inheritdoc cref="PropertyBlock.SetResource(string, RHITexture)">
        public void SetResource(string id, RHITexture texture) => AssetData.PropertyBlock?.SetResource(id, texture);
        /// <inheritdoc cref="PropertyBlock.SetResource(string, TextureAsset)">
        public void SetResource(string id, TextureAsset texture) => AssetData.PropertyBlock?.SetResource(id, texture);
        #endregion

        #region Get
        /// <inheritdoc cref="PropertyBlock.GetRHIBuffer(int)"/>
        public RHIBuffer? GetRHIBuffer(string id) => AssetData.PropertyBlock?.GetRHIBuffer(id);
        /// <inheritdoc cref="PropertyBlock.GetRHITexture(int)"/>
        public RHITexture? GetRHITexture(string id) => AssetData.PropertyBlock?.GetRHITexture(id);
        /// <inheritdoc cref="PropertyBlock.GetTextureAsset(int)"/>
        public TextureAsset? GetTextureAsset(string id) => AssetData.PropertyBlock?.GetTextureAsset(id);
        #endregion

        /// <inheritdoc cref="PropertyBlock.ClearResource(string)"/>
        public void ClearResource(string id) => AssetData.PropertyBlock?.ClearResource(id);

        #endregion

        #region Properties

        #region Set
        /// <inheritdoc cref="PropertyBlock.SetSingle(string, float)">
        public void SetSingle(string id, float value) => AssetData.PropertyBlock?.SetSingle(id, value);
        /// <inheritdoc cref="PropertyBlock.SetDouble(string, double)">
        public void SetDouble(string id, double value) => AssetData.PropertyBlock?.SetDouble(id, value);
        /// <inheritdoc cref="PropertyBlock.SetUInt(string, uint)">
        public void SetUInt(string id, uint value) => AssetData.PropertyBlock?.SetUInt(id, value);
        /// <inheritdoc cref="PropertyBlock.SetInt(string, int)">
        public void SetInt(string id, int value) => AssetData.PropertyBlock?.SetInt(id, value);
        /// <inheritdoc cref="PropertyBlock.SetVector2(string, System.Numerics.Vector2)">
        public void SetVector2(string id, Vector2 value) => AssetData.PropertyBlock?.SetVector2(id, value);
        /// <inheritdoc cref="PropertyBlock.SetVector3(string, System.Numerics.Vector3)">
        public void SetVector3(string id, Vector3 value) => AssetData.PropertyBlock?.SetVector3(id, value);
        /// <inheritdoc cref="PropertyBlock.SetVector4(string, System.Numerics.Vector4)">
        public void SetVector4(string id, Vector4 value) => AssetData.PropertyBlock?.SetVector4(id, value);
        /// <inheritdoc cref="PropertyBlock.SetMatrix4x4(string, Matrix4x4)">
        public void SetMatrix4x4(string id, Matrix4x4 value) => AssetData.PropertyBlock?.SetMatrix4x4(id, value);
        /// <inheritdoc cref="PropertyBlock.SetStruct{T}(string, T)">
        public void SetStruct<T>(string id, T value) where T : unmanaged => AssetData.PropertyBlock?.SetStruct(id, value);
        #endregion

        #region Get
        /// <inheritdoc cref="PropertyBlock.GetSingle(int)">
        public float GetSingle(string id) => AssetData.PropertyBlock?.GetSingle(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetDouble(int)">
        public double GetDouble(string id) => AssetData.PropertyBlock?.GetDouble(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetUInt(int)">
        public uint GetUInt(string id) => AssetData.PropertyBlock?.GetUInt(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetInt(int)">
        public int GetInt(string id) => AssetData.PropertyBlock?.GetInt(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector2(int)">
        public Vector2 GetVector2(string id) => AssetData.PropertyBlock?.GetVector2(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector3(int)">
        public Vector3 GetVector3(string id) => AssetData.PropertyBlock?.GetVector3(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector4(int)">
        public Vector4 GetVector4(string id) => AssetData.PropertyBlock?.GetVector4(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetMatrix4x4(int)">
        public Matrix4x4 GetMatrix4x4(string id) => AssetData.PropertyBlock?.GetMatrix4x4(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetStruct{T}(int)">
        public T GetStruct<T>(string id) where T : unmanaged => AssetData.PropertyBlock?.GetStruct<T>(id) ?? default;
        #endregion

        #endregion

        public ShaderAsset? Shader { get => AssetData.Shader; set => AssetData.ChangeActiveShader(NullableUtility.ThrowIfNull(value)); }
        public ROPropertyBlock PropertyBlock => AssetData.PropertyBlock;
    }

    public sealed class MaterialAssetData : BaseInternalAssetData<MaterialAsset>
    {
        private ShaderAsset? _shader;
        private PropertyBlock? _propertyBlock;

        internal MaterialAssetData(AssetId id) : base(id)
        {
            _shader = null;
            _propertyBlock = null;
        }

        internal void ChangeActiveShader(ShaderAsset newShader)
        {
            if (_shader == newShader)
                return;

            _shader = newShader;

            _propertyBlock?.Dispose();
            _propertyBlock = newShader.CreatePropertyBlock();
        }

        public void UpdateAssetData(MaterialAsset asset, ShaderAsset shader, PropertyBlock block)
        {
            base.UpdateAssetData(asset);

            _shader = shader;
            _propertyBlock = block;
        }

        internal ShaderAsset? Shader => _shader;
        internal PropertyBlock? PropertyBlock => _propertyBlock;
    }
}
