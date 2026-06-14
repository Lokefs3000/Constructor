using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Resources;
using Primary.RHI;
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

        private FrozenDictionary<FastStringHash, PropertyRemapData> _remapDict;
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

            _remapDict = FrozenDictionary<FastStringHash, PropertyRemapData>.Empty;
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
            if (_propertyBlockSize > 0)
                NativeMemory.Clear(_propertyData.ToPointer(), (nuint)_propertyBlockSize);
            Array.Clear(_properties);

            ++_updateIndex;
        }

        /// <summary>Not thread-safe</summary>
        public bool Reload(IShaderResourceSource? replacementShader = null)
        {
            _shader = replacementShader ?? _shader;
            if (_shader != null && !_shader.IsLoaded)
            {
                _loadIndex = -2;
                return false;
            }

            _loadIndex = _shader?.LoadIndex ?? -1;

            if (_shader == null)
            {
                if (_propertyData != nint.Zero)
                    NativeMemory.Free(_propertyData.ToPointer());

                _remapDict = FrozenDictionary<FastStringHash, PropertyRemapData>.Empty;
                _propertyBlockSize = 0;

                _propertyData = nint.Zero;
                _properties = Array.Empty<PropertyData>();

                _resourceCount = 0;
            }
            else
            {
                if (Flags.HasFlag(_shader.HeaderFlags, ShHeaderFlags.ExternalProperties))
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
                    _remapDict = FrozenDictionary<FastStringHash, PropertyRemapData>.Empty;
                else
                {
                    Dictionary<FastStringHash, PropertyRemapData> remapDict = new Dictionary<FastStringHash, PropertyRemapData>();

                    ReadOnlySpan<ShaderProperty> span = _shader.Properties;
                    for (int i = 0; i < span.Length; ++i)
                    {
                        ref readonly ShaderProperty property = ref span[i];
                        if (property.ChildIndex != ushort.MaxValue)
                        {
                            _properties[property.ChildIndex] = new PropertyData(property.IndexOrByteOffset, FrameGraphResource.Invalid);
                        }
                    }

                    foreach (ref readonly ShaderProperty property in _shader.Properties)
                    {
                        if (!Flags.HasFlag(property.Flags, ShPropertyFlags.Global))
                        {
                            PropertyRemapData remapData = new PropertyRemapData
                            {
                                Type = property.Type,
                                IndexOrByteOffset = property.IndexOrByteOffset,
                                ByteWidthOrChildIndex = property.Type <= ShPropertyType.Texture ? property.ChildIndex : property.ByteWidth,

                                ParentIndex = property.Type <= ShPropertyType.Sampler ? _properties[property.IndexOrByteOffset].ParentIndex : ushort.MaxValue
                            };

                            remapDict[property.Name] = remapData;

                            if (property.DisplayName != property.Name)
                                remapDict[property.DisplayName] = remapData;
                        }

                        if (property.Type <= ShPropertyType.Sampler)
                            _resourceCount++;
                    }

                    _remapDict = remapDict.ToFrozenDictionary();
                }
            }

            ++_updateIndex;
            return true;
        }

        private T GetRawPropertyValue<T>(string id, ShPropertyType type, T @default = default) where T : unmanaged
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

        private void SetRawPropertyValue<T>(string id, ShPropertyType type, T value) where T : unmanaged
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
        public void SetResource(string id, FrameGraphBuffer buffer)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Buffer)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(remap.ParentIndex, buffer);
                ++_updateIndex;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(string id, FrameGraphTexture texture, PropertyBindIntent intent = PropertyBindIntent.Default)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return;

                if (remap.ByteWidthOrChildIndex != ushort.MaxValue)
                    _properties[remap.ByteWidthOrChildIndex] = new PropertyData(remap.IndexOrByteOffset, FrameGraphResource.Invalid);

                _properties[remap.IndexOrByteOffset] = new PropertyData(remap.ParentIndex, texture, Intent: intent);
                ++_updateIndex;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(string id, RHIBuffer buffer)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Buffer)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(remap.ParentIndex, new FrameGraphResource(buffer, null));
                ++_updateIndex;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(string id, RHITexture texture, PropertyBindIntent intent = PropertyBindIntent.Default)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return;

                if (remap.ByteWidthOrChildIndex != ushort.MaxValue)
                    _properties[remap.ByteWidthOrChildIndex] = new PropertyData(remap.IndexOrByteOffset, FrameGraphResource.Invalid);

                _properties[remap.IndexOrByteOffset] = new PropertyData(remap.ParentIndex, new FrameGraphResource(texture, null), Intent: intent);
                ++_updateIndex;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(string id, TextureAsset texture)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != ShPropertyType.Texture)
                    return;

                _properties[remap.IndexOrByteOffset] = new PropertyData(remap.ParentIndex, FrameGraphResource.Invalid, texture);

                if (remap.ByteWidthOrChildIndex != ushort.MaxValue)
                {
                    _properties[remap.ByteWidthOrChildIndex] = new PropertyData(remap.IndexOrByteOffset, FrameGraphResource.Invalid);
                }

                ++_updateIndex;
            }
        }

        /// <summary>Not thread-safe</summary>
        public FrameGraphBuffer GetFrameGraphBuffer(string id)
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
        public RHIBuffer? GetRHIBuffer(string id)
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
        public FrameGraphTexture GetFrameGraphTexture(string id)
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
        public RHITexture? GetRHITexture(string id)
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
        public TextureAsset? GetTextureAsset(string id)
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

        /// <summary>Not thread-safe</summary>
        public void ClearResource(string id)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type > ShPropertyType.Sampler)
                    return;

                if (remap.ByteWidthOrChildIndex != ushort.MaxValue)
                {
                    PropertyData currentData = _properties[remap.IndexOrByteOffset];
                    if (currentData.Aux != null)
                        _properties[remap.ByteWidthOrChildIndex] = PropertyData.Null;
                }

                _properties[remap.IndexOrByteOffset] = PropertyData.Null;
                ++_updateIndex;
            }
        }
        #endregion

        #region Values
        /// <summary>Not thread-safe</summary>
        public void SetSingle(string id, float value) => SetRawPropertyValue(id, ShPropertyType.Single, value);
        /// <summary>Not thread-safe</summary>
        public void SetDouble(string id, double value) => SetRawPropertyValue(id, ShPropertyType.Double, value);
        /// <summary>Not thread-safe</summary>
        public void SetUInt(string id, uint value) => SetRawPropertyValue(id, ShPropertyType.UInt32, value);
        /// <summary>Not thread-safe</summary>
        public void SetInt(string id, int value) => SetRawPropertyValue(id, ShPropertyType.Int32, value);
        /// <summary>Not thread-safe</summary>
        public void SetVector2(string id, Vector2 value) => SetRawPropertyValue(id, ShPropertyType.Vector2, value);
        /// <summary>Not thread-safe</summary>
        public void SetVector3(string id, Vector3 value) => SetRawPropertyValue(id, ShPropertyType.Vector3, value);
        /// <summary>Not thread-safe</summary>
        public void SetVector4(string id, Vector4 value) => SetRawPropertyValue(id, ShPropertyType.Vector4, value);
        /// <summary>Not thread-safe</summary>
        public void SetMatrix4x4(string id, Matrix4x4 value) => SetRawPropertyValue(id, ShPropertyType.Matrix4x4, value);
        /// <summary>Not thread-safe</summary>
        public void SetStruct<T>(string id, T value) where T : unmanaged
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
        public float GetSingle(string id) => GetRawPropertyValue(id, ShPropertyType.Single, 0.0f);
        /// <summary>Not thread-safe</summary>
        public double GetDouble(string id) => GetRawPropertyValue(id, ShPropertyType.Double, 0.0);
        /// <summary>Not thread-safe</summary>
        public uint GetUInt(string id) => GetRawPropertyValue(id, ShPropertyType.UInt32, 0u);
        /// <summary>Not thread-safe</summary>
        public int GetInt(string id) => GetRawPropertyValue(id, ShPropertyType.Int32, 0);
        /// <summary>Not thread-safe</summary>
        public Vector2 GetVector2(string id) => GetRawPropertyValue(id, ShPropertyType.Vector2, Vector2.Zero);
        /// <summary>Not thread-safe</summary>
        public Vector3 GetVector3(string id) => GetRawPropertyValue(id, ShPropertyType.Vector3, Vector3.Zero);
        /// <summary>Not thread-safe</summary>
        public Vector4 GetVector4(string id) => GetRawPropertyValue(id, ShPropertyType.Vector4, Vector4.Zero);
        /// <summary>Not thread-safe</summary>
        public Matrix4x4 GetMatrix4x4(string id) => GetRawPropertyValue(id, ShPropertyType.Matrix4x4, new Matrix4x4());
        /// <summary>Not thread-safe</summary>
        public T GetStruct<T>(string id) where T : unmanaged
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
    }

    public interface IShaderResourceSource
    {
        public int LoadIndex { get; }
        public bool IsLoaded { get; }

        public ReadOnlySpan<ShaderProperty> Properties { get; }
        public IReadOnlyDictionary<int, int> RemappingTable { get; }

        public int PropertyBlockSize { get; }

        public ShHeaderFlags HeaderFlags { get; }

        public int ResourceCount { get; }
    }

    internal readonly record struct PropertyData(ushort ParentIndex, FrameGraphResource Resource, object? Aux = null, PropertyBindIntent Intent = PropertyBindIntent.Default)
    {
        internal static readonly PropertyData Null = new PropertyData(ushort.MaxValue, FrameGraphResource.Invalid, null);
    }

    internal struct PropertyRemapData
    {
        public ShPropertyType Type;
        public ushort IndexOrByteOffset;
        public ushort ByteWidthOrChildIndex;

        public ushort ParentIndex;
    }

    public enum PropertyBindIntent : byte
    {
        Default = 0,

        AsStencil
    }
}
