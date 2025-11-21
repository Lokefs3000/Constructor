using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering2.Resources;
using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Assets
{
    public sealed class ShaderGlobalsManager
    {
        private Dictionary<FastStringHash, PropertyData> _globalProperties;
        private Dictionary<int, FastStringHash> _transitionalProperties;

        internal ShaderGlobalsManager()
        {
            s_instance.Target = this;

            _globalProperties = new Dictionary<FastStringHash, PropertyData>();
            _transitionalProperties = new Dictionary<int, FastStringHash>();
        }

        internal void CleanupTransitional()
        {
            if (_transitionalProperties.Count > 0)
            {
                foreach (var kvp in _transitionalProperties)
                {
                    FastStringHash hash = kvp.Value;

                    ref PropertyData data = ref CollectionsMarshal.GetValueRefOrNullRef(_globalProperties, hash);
                    if (!Unsafe.IsNullRef(ref data) && data.Resource.IsValidAndRenderGraph)
                    {
                        _globalProperties.Remove(hash);
                    }
                }

                _transitionalProperties.Clear();
            }
        }

        internal void SetBuffer(string name, FrameGraphBuffer buffer)
        {
            _globalProperties[name] = new PropertyData(buffer);
            _transitionalProperties[buffer.Index] = name;
        }

        internal void SetBuffer(string name, RHI.Buffer buffer)
        {
            _globalProperties[name] = new PropertyData(new FrameGraphResource(buffer));
        }

        internal void SetTexture(string name, FrameGraphTexture texture)
        {
            _globalProperties[name] = new PropertyData(texture);
            _transitionalProperties[texture.Index] = name;
        }

        internal void SetTexture(string name, RHI.Texture texture)
        {
            _globalProperties[name] = new PropertyData(new FrameGraphResource(texture));
        }

        internal void SetTexture(string name, TextureAsset texture)
        {
            _globalProperties[name] = new PropertyData(FrameGraphResource.Invalid, texture);
        }

        internal bool TryGetPropertyValue(string propertyName, out PropertyData data)
        {
            return _globalProperties.TryGetValue(propertyName, out data);
        }

        #region Public
        public static void SetGlobalBuffer(string name, FrameGraphBuffer buffer) => Instance.SetBuffer(name, buffer);
        public static void SetGlobalBuffer(string name, RHI.Buffer buffer) => Instance.SetBuffer(name, buffer);

        public static void SetGlobalTexture(string name, FrameGraphTexture texture) => Instance.SetTexture(name, texture);
        public static void SetGlobalTexture(string name, RHI.Texture texture) => Instance.SetTexture(name, texture);
        public static void SetGlobalTexture(string name, TextureAsset texture) => Instance.SetTexture(name, texture);
        #endregion

        private static readonly WeakReference s_instance = new WeakReference(null);
        internal static ShaderGlobalsManager Instance => NullableUtility.ThrowIfNull(Unsafe.As<ShaderGlobalsManager>(s_instance.Target));
    }

    internal readonly record struct FastStringHash(string String)
    {
        public override int GetHashCode() => String.GetDjb2HashCode();
        public override string ToString() => String;

        public static implicit operator FastStringHash(string @string) => new FastStringHash(@string);
        public static implicit operator string(FastStringHash @string) => @string.String;
    }
}
