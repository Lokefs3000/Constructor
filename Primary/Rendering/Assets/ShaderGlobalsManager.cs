using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Resources;
using Primary.RHI;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Assets
{
    public sealed class ShaderGlobalsManager
    {
        private readonly FrameGraph _frameGraph;

        private readonly Dictionary<FastStringHash, PropertyData> _globalProperties;
        private readonly Dictionary<int, FastStringHash> _transitionalProperties;

        internal ShaderGlobalsManager(FrameGraph frameGraph)
        {
            s_instance.Target = this;

            _frameGraph = frameGraph;

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
                    if (!Unsafe.IsNullRef(ref data) && !data.FGResource.IsNull)
                    {
                        _globalProperties.Remove(hash);
                    }
                }

                _transitionalProperties.Clear();
            }
        }

        internal void SetBuffer(string name, BufferHandle buffer)
        {
            _globalProperties[name] = new PropertyData(ushort.MaxValue, buffer);
            _transitionalProperties[buffer.Index] = name;
        }

        internal void SetTexture(string name, TextureHandle texture, PropertyBindIntent intent = PropertyBindIntent.Default)
        {
            _globalProperties[name] = new PropertyData(ushort.MaxValue, texture, Intent: intent);
            _transitionalProperties[texture.Index] = name;
        }

        internal void SetTexture(string name, TextureAsset texture)
        {
            _globalProperties[name] = new PropertyData(ushort.MaxValue, TextureHandle.Null, texture);
        }

        internal bool TryGetPropertyValue(string propertyName, out PropertyData data)
        {
            return _globalProperties.TryGetValue(propertyName, out data);
        }

        #region Public
        public static void SetGlobalBuffer(string name, BufferHandle buffer) => Instance.SetBuffer(name, buffer);

        public static void SetGlobalTexture(string name, TextureHandle texture) => Instance.SetTexture(name, texture);
        public static void SetGlobalTexture(string name, TextureAsset texture) => Instance.SetTexture(name, texture);
        #endregion

        internal Dictionary<FastStringHash, PropertyData> GlobalProperties => _globalProperties;
        internal Dictionary<int, FastStringHash> TransitionalProperties => _transitionalProperties;

        private static readonly WeakReference s_instance = new WeakReference(null);
        internal static ShaderGlobalsManager Instance => NullableUtility.ThrowIfNull(Unsafe.As<ShaderGlobalsManager>(s_instance.Target));
    }

    public readonly record struct FastStringHash(string String)
    {
        public override int GetHashCode() => String.GetDjb2HashCode();
        public override string ToString() => String;

        public static implicit operator FastStringHash(string @string) => new FastStringHash(@string);
        public static implicit operator string(FastStringHash @string) => @string.String;
    }
}
