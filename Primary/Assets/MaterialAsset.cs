using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.RHI2;
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
        /// <inheritdoc cref="PropertyBlock.SetResource(int, RHIBuffer)">
        public void SetResource(int id, RHIBuffer buffer) => AssetData.PropertyBlock?.SetResource(id, buffer);
        /// <inheritdoc cref="PropertyBlock.SetResource(int, RHITexture)">
        public void SetResource(int id, RHITexture texture) => AssetData.PropertyBlock?.SetResource(id, texture);
        /// <inheritdoc cref="PropertyBlock.SetResource(int, TextureAsset)">
        public void SetResource(int id, TextureAsset texture) => AssetData.PropertyBlock?.SetResource(id, texture);

        ////////////////////////////////////////////////////////////////

        /// <inheritdoc cref="PropertyBlock.SetResource(int, RHIBuffer)">
        public void SetResource(ReadOnlySpan<char> id, RHIBuffer buffer) => AssetData.PropertyBlock?.SetResource(id.GetDjb2HashCode(), buffer);
        /// <inheritdoc cref="PropertyBlock.SetResource(int, RHITexture)">
        public void SetResource(ReadOnlySpan<char> id, RHITexture texture) => AssetData.PropertyBlock?.SetResource(id.GetDjb2HashCode(), texture);
        /// <inheritdoc cref="PropertyBlock.SetResource(int, TextureAsset)">
        public void SetResource(ReadOnlySpan<char> id, TextureAsset texture) => AssetData.PropertyBlock?.SetResource(id.GetDjb2HashCode(), texture);
        #endregion

        #region Get
        /// <inheritdoc cref="PropertyBlock.GetRHIBuffer(int)"/>
        public RHIBuffer? GetRHIBuffer(int id) => AssetData.PropertyBlock?.GetRHIBuffer(id);
        /// <inheritdoc cref="PropertyBlock.GetRHITexture(int)"/>
        public RHITexture? GetRHITexture(int id) => AssetData.PropertyBlock?.GetRHITexture(id);
        /// <inheritdoc cref="PropertyBlock.GetTextureAsset(int)"/>
        public TextureAsset? GetTextureAsset(int id) => AssetData.PropertyBlock?.GetTextureAsset(id);

        ////////////////////////////////////////////////////////////////

        /// <inheritdoc cref="PropertyBlock.GetRHIBuffer(int)"/>
        public RHIBuffer? GetRHIBuffer(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetRHIBuffer(id.GetDjb2HashCode());
        /// <inheritdoc cref="PropertyBlock.GetRHITexture(int)"/>
        public RHITexture? GetRHITexture(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetRHITexture(id.GetDjb2HashCode());
        /// <inheritdoc cref="PropertyBlock.GetTextureAsset(int)"/>
        public TextureAsset? GetTextureAsset(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetTextureAsset(id.GetDjb2HashCode());
        #endregion

        #endregion

        #region Properties

        #region Set
        /// <inheritdoc cref="PropertyBlock.SetSingle(int, float)">
        public void SetSingle(int id, float value) => AssetData.PropertyBlock?.SetSingle(id, value);
        /// <inheritdoc cref="PropertyBlock.SetDouble(int, double)">
        public void SetDouble(int id, double value) => AssetData.PropertyBlock?.SetDouble(id, value);
        /// <inheritdoc cref="PropertyBlock.SetUInt(int, uint)">
        public void SetUInt(int id, uint value) => AssetData.PropertyBlock?.SetUInt(id, value);
        /// <inheritdoc cref="PropertyBlock.SetInt(int, int)">
        public void SetInt(int id, int value) => AssetData.PropertyBlock?.SetInt(id, value);
        /// <inheritdoc cref="PropertyBlock.SetVector2(int, System.Numerics.Vector2)">
        public void SetVector2(int id, Vector2 value) => AssetData.PropertyBlock?.SetVector2(id, value);
        /// <inheritdoc cref="PropertyBlock.SetVector3(int, System.Numerics.Vector3)">
        public void SetVector3(int id, Vector3 value) => AssetData.PropertyBlock?.SetVector3(id, value);
        /// <inheritdoc cref="PropertyBlock.SetVector4(int, System.Numerics.Vector4)">
        public void SetVector4(int id, Vector4 value) => AssetData.PropertyBlock?.SetVector4(id, value);
        /// <inheritdoc cref="PropertyBlock.SetStruct{T}(int, T)">
        public void SetMatrix4x4(int id, Matrix4x4 value) => AssetData.PropertyBlock?.SetMatrix4x4(id, value);
        /// <inheritdoc cref="PropertyBlock.SetStruct{T}(int, T)">
        public void SetStruct<T>(int id, T value) where T : unmanaged => AssetData.PropertyBlock?.SetStruct(id, value);

        ////////////////////////////////////////////////////////////////

        /// <inheritdoc cref="PropertyBlock.SetSingle(int, float)">
        public void SetSingle(ReadOnlySpan<char> id, float value) => AssetData.PropertyBlock?.SetSingle(id.GetDjb2HashCode(), value);
        /// <inheritdoc cref="PropertyBlock.SetDouble(int, double)">
        public void SetDouble(ReadOnlySpan<char> id, double value) => AssetData.PropertyBlock?.SetDouble(id.GetDjb2HashCode(), value);
        /// <inheritdoc cref="PropertyBlock.SetUInt(int, uint)">
        public void SetUInt(ReadOnlySpan<char> id, uint value) => AssetData.PropertyBlock?.SetUInt(id.GetDjb2HashCode(), value);
        /// <inheritdoc cref="PropertyBlock.SetInt(int, int)">
        public void SetInt(ReadOnlySpan<char> id, int value) => AssetData.PropertyBlock?.SetInt(id.GetDjb2HashCode(), value);
        /// <inheritdoc cref="PropertyBlock.SetVector2(int, System.Numerics.Vector2)">
        public void SetVector2(ReadOnlySpan<char> id, Vector2 value) => AssetData.PropertyBlock?.SetVector2(id.GetDjb2HashCode(), value);
        /// <inheritdoc cref="PropertyBlock.SetVector3(int, System.Numerics.Vector3)">
        public void SetVector3(ReadOnlySpan<char> id, Vector3 value) => AssetData.PropertyBlock?.SetVector3(id.GetDjb2HashCode(), value);
        /// <inheritdoc cref="PropertyBlock.SetVector4(int, System.Numerics.Vector4)">
        public void SetVector4(ReadOnlySpan<char> id, Vector4 value) => AssetData.PropertyBlock?.SetVector4(id.GetDjb2HashCode(), value);
        /// <inheritdoc cref="PropertyBlock.SetMatrix4x4(int, Matrix4x4)">
        public void SetMatrix4x4(ReadOnlySpan<char> id, Matrix4x4 value) => AssetData.PropertyBlock?.SetMatrix4x4(id.GetDjb2HashCode(), value);
        /// <inheritdoc cref="PropertyBlock.SetStruct{T}(int, T)">
        public void SetStruct<T>(ReadOnlySpan<char> id, T value) where T : unmanaged => AssetData.PropertyBlock?.SetStruct(id.GetDjb2HashCode(), value);
        #endregion

        #region Get

        #endregion
        /// <inheritdoc cref="PropertyBlock.GetSingle(int)">
        public float GetSingle(int id) => AssetData.PropertyBlock?.GetSingle(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetDouble(int)">
        public double GetDouble(int id) => AssetData.PropertyBlock?.GetDouble(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetUInt(int)">
        public uint GetUInt(int id) => AssetData.PropertyBlock?.GetUInt(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetInt(int)">
        public int GetInt(int id) => AssetData.PropertyBlock?.GetInt(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector2(int)">
        public Vector2 GetVector2(int id) => AssetData.PropertyBlock?.GetVector2(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector3(int)">
        public Vector3 GetVector3(int id) => AssetData.PropertyBlock?.GetVector3(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector4(int)">
        public Vector4 GetVector4(int id) => AssetData.PropertyBlock?.GetVector4(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetMatrix4x4(int)">
        public Matrix4x4 GetMatrix4x4(int id) => AssetData.PropertyBlock?.GetMatrix4x4(id) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetStruct{T}(int)">
        public T GetStruct<T>(int id) where T : unmanaged => AssetData.PropertyBlock?.GetStruct<T>(id) ?? default;

        ////////////////////////////////////////////////////////////////

        /// <inheritdoc cref="PropertyBlock.GetSingle(int)">
        public float GetSingle(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetSingle(id.GetDjb2HashCode()) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetDouble(int)">
        public double GetDouble(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetDouble(id.GetDjb2HashCode()) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetUInt(int)">
        public uint GetUInt(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetUInt(id.GetDjb2HashCode()) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetInt(int)">
        public int GetInt(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetInt(id.GetDjb2HashCode()) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector2(int)">
        public Vector2 GetVector2(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetVector2(id.GetDjb2HashCode()) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector3(int)">
        public Vector3 GetVector3(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetVector3(id.GetDjb2HashCode()) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetVector4(int)">
        public Vector4 GetVector4(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetVector4(id.GetDjb2HashCode()) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetMatrix4x4(int)">
        public Matrix4x4 GetMatrix4x4(ReadOnlySpan<char> id) => AssetData.PropertyBlock?.GetMatrix4x4(id.GetDjb2HashCode()) ?? default;
        /// <inheritdoc cref="PropertyBlock.GetStruct{T}(int)">
        public T GetStruct<T>(ReadOnlySpan<char> id) where T : unmanaged => AssetData.PropertyBlock?.GetStruct<T>(id.GetDjb2HashCode()) ?? default;
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
