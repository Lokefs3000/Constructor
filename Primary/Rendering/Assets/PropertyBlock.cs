using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Resources;
using Primary.RHI2;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Assets
{
    public unsafe sealed class PropertyBlock : IDisposable
    {
        private IShaderResourceSource? _shader;
        private int _loadIndex;

        private FrozenDictionary<int, PropertyRemapData> _remapDict;
        private int _propertyBlockSize;

        private nint _propertyData;
        private PropertyData[] _properties;

        private int _resourceCount;

        private int _updateIndex;

        private bool _disposedValue;

        internal PropertyBlock(IShaderResourceSource? shader = null)
        {
            _shader = null;
            _loadIndex = -1;

            _remapDict = FrozenDictionary<int, PropertyRemapData>.Empty;
            _propertyBlockSize = 0;

            _propertyData = nint.Zero;
            _properties = Array.Empty<PropertyData>();

            _resourceCount = 0;

            _updateIndex = 0;

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

            ++_updateIndex;
        }

        /// <summary>Not thread-safe</summary>
        public void Reload(IShaderResourceSource? replacementShader = null)
        {
            _shader = replacementShader ?? _shader;
            _loadIndex = _shader?.LoadIndex ?? -1;

            if (_shader == null)
            {
                if (_propertyData != nint.Zero)
                    NativeMemory.Free(_propertyData.ToPointer());

                _remapDict = FrozenDictionary<int, PropertyRemapData>.Empty;
                _propertyBlockSize = 0;

                _propertyData = nint.Zero;
                _properties = Array.Empty<PropertyData>();

                _resourceCount = 0;
            }
            else
            {
                if (FlagUtility.HasFlag(_shader.HeaderFlags, ShHeaderFlags.ExternalProperties))
                {
                    _propertyBlockSize = 0;
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
                }

                if (_properties.Length != _shader.Properties.Length)
                    _properties = _shader.Properties.IsEmpty ? Array.Empty<PropertyData>() : new PropertyData[_shader.Properties.Length];
                Array.Fill(_properties, PropertyData.Null);

                _resourceCount = 0;

                if (_shader.Properties.IsEmpty)
                    _remapDict = FrozenDictionary<int, PropertyRemapData>.Empty;
                else
                {
                    Dictionary<int, PropertyRemapData> remapDict = new Dictionary<int, PropertyRemapData>();

                    int index = 0;
                    foreach (ref readonly ShaderProperty property in _shader.Properties)
                    {
                        if (!FlagUtility.HasFlag(property.Flags, ShPropertyFlags.Global))
                        {
                            remapDict[property.Name.GetDjb2HashCode()] = new PropertyRemapData
                            {
                                Type = property.Type,
                                IndexOrByteOffset = (ushort)(property.Type <= ShPropertyType.Texture ? index++ : property.IndexOrByteOffset),
                                ByteWidthOrChildIndex = property.Type <= ShPropertyType.Texture ? property.ChildIndex : property.ByteWidth
                            };

                            if (property.Type <= ShPropertyType.Sampler)
                                _resourceCount++;
                        }
                    }

                    _remapDict = remapDict.ToFrozenDictionary();
                }
            }

            ++_updateIndex;
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
                ++_updateIndex;
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
                ++_updateIndex;
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
                if (remap.ByteWidthOrChildIndex != ushort.MaxValue)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(texture);
                ++_updateIndex;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, RHIBuffer buffer)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Buffer)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(new FrameGraphResource(buffer, null));
                ++_updateIndex;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, RHITexture texture)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return;
                if (remap.ByteWidthOrChildIndex != ushort.MaxValue)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(new FrameGraphResource(texture, null));
                ++_updateIndex;
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

                if (remap.ByteWidthOrChildIndex != ushort.MaxValue)
                {
                    _properties[remap.ByteWidthOrChildIndex] = new PropertyData(FrameGraphResource.Invalid, texture.RawRHISampler);
                }

                ++_updateIndex;
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
        public RHIBuffer? GetRHIBuffer(int id)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Buffer)
                    return null;

                PropertyData data = _properties[remap.IndexOrByteOffset];
                return data.Resource.Resource as RHIBuffer;
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
        public RHITexture? GetRHITexture(int id)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return null;

                PropertyData data = _properties[remap.IndexOrByteOffset];
                return data.Resource.Resource as RHITexture;
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
                if (Unsafe.SizeOf<T>() != remap.ByteWidthOrChildIndex)
                    return;

                *(T*)((byte*)_propertyData.ToPointer() + remap.IndexOrByteOffset) = value;
                ++_updateIndex;
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
                if (Unsafe.SizeOf<T>() != remap.ByteWidthOrChildIndex)
                    return default;

                return *(T*)((byte*)_propertyData.ToPointer() + remap.IndexOrByteOffset);
            }

            return default;
        }
        #endregion

        #region Raw
        public void CopyBlockDataTo(nint ptr)
        {
            if (_propertyData != nint.Zero)
                NativeMemory.Copy(_propertyData.ToPointer(), ptr.ToPointer(), (nuint)_propertyBlockSize);
        }
        #endregion

        public IShaderResourceSource? Shader => _shader;

        public nint BlockPointer => _propertyData;
        public int BlockSize => _propertyBlockSize;

        public int ResourceCount => _resourceCount;

        public bool IsOutOfDate => (_shader?.LoadIndex ?? -1) != _loadIndex;
        public int UpdateIndex => _updateIndex;

        public static int GetID(ReadOnlySpan<char> id) => id.GetDjb2HashCode();
    }

    public interface IShaderResourceSource
    {
        public int LoadIndex { get; }

        public ReadOnlySpan<ShaderProperty> Properties { get; }
        public IReadOnlyDictionary<int, int> RemappingTable { get; }

        public int PropertyBlockSize { get; }

        public ShHeaderFlags HeaderFlags { get; }

        public int ResourceCount { get; }
    }

    internal readonly record struct PropertyData(FrameGraphResource Resource, object? Aux = null)
    {
        internal static readonly PropertyData Null = new PropertyData(FrameGraphResource.Invalid, null);
    }

    internal struct PropertyRemapData
    {
        public ShPropertyType Type;
        public ushort IndexOrByteOffset;
        public ushort ByteWidthOrChildIndex;
    }
}
