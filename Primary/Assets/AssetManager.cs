using CommunityToolkit.HighPerformance;
using Primary.Assets.Loaders;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Utility.Scopes;
using Serilog;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Primary.Assets
{
    public sealed class AssetManager : IDisposable
    {
        private static AssetManager? s_instance;

        private Dictionary<int, LoadedAsset> _loadedAssets;
        private FrozenDictionary<Type, AssetLoader> _loaders;

        private SemaphoreSlim _semaphore;

        private Lazy<ImmutableAssets> _immutableAssets;

        private bool _disposedValue;

        internal AssetManager()
        {
            _loadedAssets = new Dictionary<int, LoadedAsset>();
            _loaders = new Dictionary<Type, AssetLoader>
            {
                { typeof(ModelAsset), new AssetLoader(ModelAssetLoader.FactoryCreateNull, ModelAssetLoader.FactoryCreateDef, ModelAssetLoader.FactoryLoad) },
                { typeof(ShaderAsset), new AssetLoader(ShaderAssetLoader.FactoryCreateNull, ShaderAssetLoader.FactoryCreateDef, ShaderAssetLoader.FactoryLoad) },
                { typeof(MaterialAsset), new AssetLoader(MaterialAssetLoader.FactoryCreateNull, MaterialAssetLoader.FactoryCreateDef, MaterialAssetLoader.FactoryLoad) },
                { typeof(TextureAsset), new AssetLoader(TextureAssetLoader.FactoryCreateNull, TextureAssetLoader.FactoryCreateDef, TextureAssetLoader.FactoryLoad) },
            }.ToFrozenDictionary();

            _semaphore = new SemaphoreSlim(1);

            _immutableAssets = new Lazy<ImmutableAssets>(() =>
            {
                return new ImmutableAssets(
                    NullableUtility.AlwaysThrowIfNull(LoadAsset<TextureAsset>("Content/DefaultTex_White.png", true)),
                    NullableUtility.AlwaysThrowIfNull(LoadAsset<TextureAsset>("Content/DefaultTex_Normal.png", true)));
            }, LazyThreadSafetyMode.PublicationOnly);

            s_instance = this; 
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    s_instance = null;

                    foreach (var kvp in _loadedAssets)
                    {
                        kvp.Value.AssetData.Dispose();
                    }

                    _loadedAssets.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void ForceReloadAsset(ReadOnlySpan<char> sourcePath, bool synchronous = false, BundleReader? bundleToReadFrom = null)
        {
            int id = bundleToReadFrom == null ?
                sourcePath.GetDjb2HashCode() :
                $"{bundleToReadFrom.GetHashCode()}{sourcePath}".GetDjb2HashCode();

            using (new SemaphoreScope(_semaphore))
            {
                ref LoadedAsset asset = ref CollectionsMarshal.GetValueRefOrNullRef(_loadedAssets, id);
                if (!Unsafe.IsNullRef(ref asset))
                {
                    asset.AssetData.ResetInternalState();

                    ref readonly AssetLoader loader = ref _loaders.GetValueRefOrNullRef(asset.AssetData.AssetType);
                    if (Unsafe.IsNullRef(in loader))
                    {
                        Log.Error("[a:{path}] No asset loader for type: {type}", sourcePath.ToString(), asset.AssetData.AssetType.Name);
                        return;
                    }

                    object? assetRef = (object?)asset.AssetReference.Target;
                    if (assetRef == null)
                    {
                        assetRef = loader.FactoryCreateDef(asset.AssetData);
                        asset.AssetReference.Target = assetRef;
                    }

                    asset.AssetData.PromoteStateToRunning();
                    //TODO: scheduler!
                    //and wait if "synchronous" is true!
                    //though its there mainly for the sake of other resources
                    loader.FactoryLoad((IAssetDefinition)assetRef, asset.AssetData, sourcePath.ToString(), bundleToReadFrom);
                }
            }
        }

        private T? LoadAssetImpl<T>(ReadOnlySpan<char> sourcePath, bool synchronous = false, BundleReader? bundleToReadFrom = null) where T : class, IAssetDefinition
        {
            int id = bundleToReadFrom == null ?
                sourcePath.GetDjb2HashCode() :
                $"{bundleToReadFrom.GetHashCode()}{sourcePath}".GetDjb2HashCode();

            using (new SemaphoreScope(_semaphore))
            {
                ref LoadedAsset asset = ref CollectionsMarshal.GetValueRefOrNullRef(_loadedAssets, id);
                if (!Unsafe.IsNullRef(ref asset))
                {
                    T? assetRef = (T?)asset.AssetReference.Target;
                    if (assetRef == null)
                    {
                        ref readonly AssetLoader loader = ref _loaders.GetValueRefOrNullRef(typeof(T));
                        ExceptionUtility.Assert(!Unsafe.IsNullRef(in loader)); //should not be since this was created but safety first

                        assetRef = (T)loader.FactoryCreateDef(asset.AssetData);
                        asset.AssetReference.Target = assetRef;
                    }

                    return assetRef;
                }
                else
                {
                    ref readonly AssetLoader loader = ref _loaders.GetValueRefOrNullRef(typeof(T));
                    if (Unsafe.IsNullRef(in loader))
                    {
                        Log.Error("[a:{path}] No asset loader for type: {type}", sourcePath.ToString(), typeof(T).Name);
                        return null;
                    }

                    IInternalAssetData assetData = loader.FactoryCreateNull();
                    IAssetDefinition assetDef = loader.FactoryCreateDef(assetData);

                    LoadedAsset newAsset = new LoadedAsset(assetDef, assetData);
                    _loadedAssets[id] = newAsset;

                    assetData.PromoteStateToRunning();
                    //TODO: scheduler!
                    //and wait if "synchronous" is true!
                    //though its there mainly for the sake of other resources
                    loader.FactoryLoad(assetDef, assetData, sourcePath.ToString(), bundleToReadFrom);

                    return (T?)assetDef;
                }
            }
        }

        public static T? LoadAsset<T>(ReadOnlySpan<char> sourcePath, bool synchronous = false, BundleReader? bundleToReadFrom = null) where T : class, IAssetDefinition
            => NullableUtility.ThrowIfNull(s_instance).LoadAssetImpl<T>(sourcePath, synchronous, bundleToReadFrom);

        public static ImmutableAssets Static => NullableUtility.ThrowIfNull(s_instance)._immutableAssets.Value;

        private readonly record struct LoadedAsset
        {
            public readonly WeakReference AssetReference;
            public readonly IInternalAssetData AssetData;

            public LoadedAsset(IAssetDefinition asset, IInternalAssetData assetData)
            {
                AssetReference = new WeakReference(asset);
                AssetData = assetData;
            }
        }

        private readonly record struct AssetLoader
        {
            public readonly Func<IInternalAssetData> FactoryCreateNull;
            public readonly Func<IInternalAssetData, IAssetDefinition> FactoryCreateDef;
            public readonly Action<IAssetDefinition, IInternalAssetData, string, BundleReader?> FactoryLoad;

            public AssetLoader(Func<IInternalAssetData> factoryCreateNull, Func<IInternalAssetData, IAssetDefinition> factoryCreateDef, Action<IAssetDefinition, IInternalAssetData, string, BundleReader?> factoryLoad)
            {
                FactoryCreateNull = factoryCreateNull;
                FactoryCreateDef = factoryCreateDef;
                FactoryLoad = factoryLoad;
            }
        }
    }

    public record class ImmutableAssets(
        TextureAsset DefaultWhite,
        TextureAsset DefaultNormal);
}
