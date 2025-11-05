using Primary.Assets;
using Primary.Rendering2.Resources;
using Primary.RHI;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Assets
{
    public unsafe sealed class PropertyBlock : IDisposable
    {
        private ShaderAsset? _shader;

        private FrozenDictionary<int, PropertyRemapData> _remapDict;
        private int _propertyBlockSize;

        private nint _propertyData;
        private PropertyData[] _properties;

        private bool _disposedValue;

        internal PropertyBlock(ShaderAsset? shader = null)
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
        public void Clear()
        {
            NativeMemory.Clear(_propertyData.ToPointer(), (nuint)_propertyBlockSize);
            Array.Clear(_properties);
        }

        /// <summary>Not thread-safe</summary>
        public void Reload(ShaderAsset? replacementShader = null)
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
                
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, FrameGraphBuffer buffer)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != PropertyType.Buffer)
                    return;

                _properties[remap.IndexOrOffset] = new PropertyData(buffer);
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, FrameGraphTexture texture)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != PropertyType.Texture)
                    return;

                _properties[remap.IndexOrOffset] = new PropertyData(texture);
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, RHI.Buffer buffer)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != PropertyType.Buffer)
                    return;

                _properties[remap.IndexOrOffset] = new PropertyData(new FrameGraphResource(buffer));
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, RHI.Texture texture)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != PropertyType.Texture)
                    return;

                _properties[remap.IndexOrOffset] = new PropertyData(new FrameGraphResource(texture));
            }
        }

        /// <summary>Not thread-safe</summary>
        public void SetResource(int id, TextureAsset texture)
        {
            ref readonly PropertyRemapData remap = ref _remapDict.GetValueRefOrNullRef(id);
            if (!Unsafe.IsNullRef(in remap))
            {
                if (remap.Type != PropertyType.Texture)
                    return;

                _properties[remap.IndexOrOffset] = texture.RawRHITexture == null ? default : new PropertyData(FrameGraphResource.Invalid, texture);
            }
        }
    }

    internal readonly record struct PropertyData(FrameGraphResource Resource, object? Aux = null);

    internal struct PropertyRemapData
    {
        public PropertyType Type;
        public int IndexOrOffset;
    }

    internal enum PropertyType : byte
    {
        Buffer = 0,
        Texture,

        Single,
        Double,
        UInt32,

        Vector2,
        Vector3,
        Vector4,

        Matrix4x4
    }
}
