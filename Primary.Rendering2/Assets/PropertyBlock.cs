using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering2.Resources;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Primary.Rendering2.Assets
{
    public unsafe sealed class PropertyBlock : IDisposable
    {
        private ShaderAsset2? _shader;

        private FrozenDictionary<int, PropertyRemapData> _remapDict;
        private int _propertyBlockSize;

        private nint _propertyData;
        private PropertyData[] _properties;

        private bool _disposedValue;

        internal PropertyBlock(ShaderAsset2? shader = null)
        {
            _remapDict = FrozenDictionary<int, PropertyRemapData>.Empty;
            _propertyBlockSize = 0;

            _propertyData = nint.Zero;
            _properties = Array.Empty<PropertyData>();

            if (shader != null)
                Reload(shader);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_propertyData != nint.Zero)
                    NativeMemory.Free(_propertyData.ToPointer());
                _propertyData = nint.Zero;

                _disposedValue = true;
            }
        }

        ~PropertyBlock()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Not thread-safe</summary>
        internal ref readonly PropertyData GetPropertyValue(int index)
        {
            Debug.Assert(index >= 0 && index < _properties.Length);
            return ref _properties.DangerousGetReferenceAt(index);
        }

        /// <summary>Not thread-safe</summary>
        public void Clear()
        {
            NativeMemory.Clear(_propertyData.ToPointer(), (nuint)_propertyBlockSize);
            Array.Clear(_properties);
        }

        /// <summary>Not thread-safe</summary>
        public void Reload(ShaderAsset2? replacementShader = null)
        {
            _shader = replacementShader ?? _shader;

            if (_shader == null)
            {
                if (_propertyData != nint.Zero)
                    NativeMemory.Free(_propertyData.ToPointer());

                _remapDict = FrozenDictionary<int, PropertyRemapData>.Empty;
                _propertyBlockSize = 0;

                _propertyData = nint.Zero;
                _properties = Array.Empty<PropertyData>();
            }
            else
            {
                if (_propertyBlockSize != _shader.PropertyBlockSize || _propertyData == nint.Zero)
                {
                    if (_propertyData != nint.Zero)
                        NativeMemory.Free(_propertyData.ToPointer());
                    if (_shader.PropertyBlockSize > 0)
                        _propertyData = (nint)NativeMemory.Alloc((nuint)_shader.PropertyBlockSize);

                    _propertyBlockSize = _shader.PropertyBlockSize;
                }

                if (_properties.Length != _shader.Properties.Length)
                    _properties = _shader.Properties.IsEmpty ? Array.Empty<PropertyData>() : new PropertyData[_shader.Properties.Length];
                Array.Clear(_properties);

                if (_shader.Properties.IsEmpty)
                    _remapDict = FrozenDictionary<int, PropertyRemapData>.Empty;
                else
                {
                    Dictionary<int, PropertyRemapData> remapDict = new Dictionary<int, PropertyRemapData>();

                    int index = 0;
                    foreach (ref readonly ShaderProperty property in _shader.Properties)
                    {
                        if (FlagUtility.HasFlag(property.Flags, ShPropertyFlags.Property))
                        {
                            remapDict[property.Name.GetDjb2HashCode()] = new PropertyRemapData
                            {
                                Type = property.Type,
                                IndexOrByteOffset = (ushort)(property.Type <= ShPropertyType.Texture ? index++ : property.IndexOrByteOffset)
                            };
                        }
                    }

                    _remapDict = remapDict.ToFrozenDictionary();
                }
            }
        }

        private T GetRawPropertyValue<T>(int id, ShPropertyType type, T @default = default) where T : unmanaged
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != type)
                    return @default;

                return *(T*)((byte*)_propertyData.ToPointer() + remap.IndexOrByteOffset);
            }

            return @default;
        }

        private void SetRawPropertyValue<T>(int id, ShPropertyType type, T value) where T : unmanaged
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != type)
                    return;

                *(T*)((byte*)_propertyData.ToPointer() + remap.IndexOrByteOffset) = value;
            }
        }

        #region Resources
        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, FrameGraphBuffer buffer)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Buffer)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(buffer);
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, FrameGraphTexture texture)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(texture);
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, RHI.Buffer buffer)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Buffer)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(new FrameGraphResource(buffer));
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, RHI.Texture texture)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(new FrameGraphResource(texture));
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, TextureAsset texture)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(FrameGraphResource.Invalid, texture);
            }
        }

        /// <summary>Not thread-safe</summary>
        public FrameGraphBuffer GetFrameGraphBuffer(int id)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Buffer)
                    return FrameGraphBuffer.Invalid;

                PropertyData data = _properties[remap.IndexOrByteOffset];
                return data.Resource.IsExternal ? FrameGraphBuffer.Invalid : new FrameGraphBuffer(data.Resource);
            }

            return FrameGraphBuffer.Invalid;
        }

        /// <summary>Not thread-safe</summary>
        public RHI.Buffer? GetRHIBuffer(int id)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Buffer)
                    return null;

                PropertyData data = _properties[remap.IndexOrByteOffset];
                return data.Resource.Resource as RHI.Buffer;
            }

            return null;
        }

        /// <summary>Not thread-safe</summary>
        public FrameGraphTexture GetFrameGraphTexture(int id)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return FrameGraphTexture.Invalid;

                PropertyData data = _properties[remap.IndexOrByteOffset];
                return data.Resource.IsExternal ? FrameGraphTexture.Invalid : new FrameGraphTexture(data.Resource);
            }

            return FrameGraphTexture.Invalid;
        }

        /// <summary>Not thread-safe</summary>
        public RHI.Texture? GetRHITexture(int id)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return null;

                PropertyData data = _properties[remap.IndexOrByteOffset];
                return data.Resource.Resource as RHI.Texture;
            }

            return null;
        }

        /// <summary>Not thread-safe</summary>
        public TextureAsset? GetTextureAsset(int id)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return null;

                PropertyData data = _properties[remap.IndexOrByteOffset];
                return data.Aux as TextureAsset;
            }

            return null;
        }
        #endregion

        #region Values
        /// <summary>Not thread-safe</summary>
        public void SetSingle(int id, float value) => SetRawPropertyValue(id, ShPropertyType.Single, value);
        /// <summary>Not thread-safe</summary>
        public void SetDouble(int id, double value) => SetRawPropertyValue(id, ShPropertyType.Double, value);
        /// <summary>Not thread-safe</summary>
        public void SetUInt(int id, uint value) => SetRawPropertyValue(id, ShPropertyType.UInt32, value);
        /// <summary>Not thread-safe</summary>
        public void SetInt(int id, int value) => SetRawPropertyValue(id, ShPropertyType.Int32, value);
        /// <summary>Not thread-safe</summary>
        public void SetVector2(int id, Vector2 value) => SetRawPropertyValue(id, ShPropertyType.Vector2, value);
        /// <summary>Not thread-safe</summary>
        public void SetVector3(int id, Vector3 value) => SetRawPropertyValue(id, ShPropertyType.Vector3, value);
        /// <summary>Not thread-safe</summary>
        public void SetVector4(int id, Vector4 value) => SetRawPropertyValue(id, ShPropertyType.Vector4, value);
        /// <summary>Not thread-safe</summary>
        public void SetMatrix4x4(int id, Matrix4x4 value) => SetRawPropertyValue(id, ShPropertyType.Matrix4x4, value);
        /// <summary>Not thread-safe</summary>
        public void SetStruct<T>(int id, T value) where T : unmanaged
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Struct)
                    return;
                if (Unsafe.SizeOf<T>() != remap.ByteWidth)
                    return;

                *(T*)((byte*)_propertyData.ToPointer() + remap.IndexOrByteOffset) = value;
            }
        }

        /// <summary>Not thread-safe</summary>
        public float GetSingle(int id) => GetRawPropertyValue(id, ShPropertyType.Single, 0.0f);
        /// <summary>Not thread-safe</summary>
        public double GetDouble(int id) => GetRawPropertyValue(id, ShPropertyType.Double, 0.0);
        /// <summary>Not thread-safe</summary>
        public uint GetUInt(int id) => GetRawPropertyValue(id, ShPropertyType.UInt32, 0u);
        /// <summary>Not thread-safe</summary>
        public int GetInt(int id) => GetRawPropertyValue(id, ShPropertyType.Int32, 0);
        /// <summary>Not thread-safe</summary>
        public Vector2 GetVector2(int id) => GetRawPropertyValue(id, ShPropertyType.Vector2, Vector2.Zero);
        /// <summary>Not thread-safe</summary>
        public Vector3 GetVector3(int id) => GetRawPropertyValue(id, ShPropertyType.Vector3, Vector3.Zero);
        /// <summary>Not thread-safe</summary>
        public Vector4 GetVector4(int id) => GetRawPropertyValue(id, ShPropertyType.Vector4, Vector4.Zero);
        /// <summary>Not thread-safe</summary>
        public Matrix4x4 GetMatrix4x4(int id) => GetRawPropertyValue(id, ShPropertyType.Matrix4x4, new Matrix4x4());
        /// <summary>Not thread-safe</summary>
        public T GetStruct<T>(int id) where T : unmanaged
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Struct)
                    return default;
                if (Unsafe.SizeOf<T>() != remap.ByteWidth)
                    return default;

                return *(T*)((byte*)_propertyData.ToPointer() + remap.IndexOrByteOffset);
            }

            return default;
        }
        #endregion

        public ShaderAsset2? Shader => _shader;
    }

    internal readonly record struct PropertyData(FrameGraphResource Resource, object? Aux = null);

    internal struct PropertyRemapData
    {
        public ShPropertyType Type;
        public ushort IndexOrByteOffset;
        public ushort ByteWidth;
    }
}
