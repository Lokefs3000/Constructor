using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Profiling;
using Primary.Threading;
using Primary.Timing;

namespace Primary.Assets
{
    public sealed class AssetManager : IDisposable
    {
        private static AssetManager? s_instance;

        private readonly AssetCallbacks _callbacks;
        private readonly AssetListener _listener;
        private readonly AssetLoadTimings _timings;
        private readonly AssetLoadScheduler _scheduler;

        private ConcurrentQueue<AssetLoadCallback> _loadCallbacks;

        private IAssetIdProvider? _assetIdProvider;
        private IAssetCacheProvider? _assetCacheProvider;

        private Dictionary<AssetId, LoadedAsset> _loadedAssets;
        private FrozenDictionary<Type, IAssetLoader> _loaders;

        private Dictionary<Type, (IAssetDefinition, IInternalAssetData)> _invalidAssets;

        private Lock _loadLock;

        private Lazy<ImmutableAssets> _immutableAssets;

        private bool _disposedValue;

        internal AssetManager()
        {
            _callbacks = new AssetCallbacks();
            _listener = new AssetListener();
            _timings = new AssetLoadTimings();
            _scheduler = new AssetLoadScheduler(this, _timings);

            _loadCallbacks = new ConcurrentQueue<AssetLoadCallback>();

            _loadedAssets = new Dictionary<AssetId, LoadedAsset>();
            _loaders = new Dictionary<Type, IAssetLoader>
            {
                { typeof(ModelAsset), new ModelAssetLoader() },
                { typeof(MeshAsset), new MeshAssetLoader() },
                { typeof(ShaderAsset), new ShaderAssetLoader() },
                { typeof(MaterialAsset), new MaterialAssetLoader() },
                { typeof(TextureAsset), new TextureAssetLoader() },
                { typeof(ComputeShaderAsset), new ComputeShaderAssetLoader() },
                { typeof(TextureAtlasAsset), new TextureAtlasAssetLoader() }
            }.ToFrozenDictionary();

            _invalidAssets = new Dictionary<Type, (IAssetDefinition, IInternalAssetData)>();

            _loadLock = new Lock();

            _immutableAssets = new Lazy<ImmutableAssets>(() =>
            {
                return new ImmutableAssets(
                    NullableUtility.AlwaysThrowIfNull(LoadAsset<TextureAsset>("Engine/Textures/DefaultTex_White.png", AssetId.NoLocalId, true)),
                    NullableUtility.AlwaysThrowIfNull(LoadAsset<TextureAsset>("Engine/Textures/DefaultTex_Black.png", AssetId.NoLocalId, true)),
                    NullableUtility.AlwaysThrowIfNull(LoadAsset<TextureAsset>("Engine/Textures/DefaultTex_Normal.png", AssetId.NoLocalId, true)),
                    NullableUtility.AlwaysThrowIfNull(LoadAsset<TextureAsset>("Engine/Textures/DefaultTex_Mask.png", AssetId.NoLocalId, true)),
                    NullableUtility.AlwaysThrowIfNull(LoadAsset<TextureAsset>("Engine/Textures/DebugTex_Loading.png", AssetId.NoLocalId, true)),
                    NullableUtility.AlwaysThrowIfNull(LoadAsset<TextureAsset>("Engine/Textures/DebugTex_Error.png", AssetId.NoLocalId, true)));
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

        /// <summary>Not thread-safe</summary>
        public void UpdateAssets()
        {
            using (new ProfilingScope("UpdateAssets"))
            {
                while (_loadCallbacks.TryDequeue(out AssetLoadCallback loadCallback))
                {
                    _callbacks.Invoke(loadCallback.Asset.GetType(), loadCallback.Id);
                    _listener.InvokeListeners(loadCallback.Id, loadCallback.Asset, loadCallback.WasReloaded);
                }
            }
        }

        /// <summary>Thread-safe</summary>
        public void ForceReloadAsset(AssetId assetId, string? newName = null, bool synchronous = false)
        {
            using Lock.Scope lockScope = _loadLock.EnterScope();

            ref LoadedAsset asset = ref CollectionsMarshal.GetValueRefOrNullRef(_loadedAssets, assetId);
            if (!Unsafe.IsNullRef(ref asset))
            {
                ref readonly IAssetLoader loader = ref _loaders.GetValueRefOrNullRef(asset.AssetData.AssetType);
                if (Unsafe.IsNullRef(in loader))
                {
                    EngLog.Assets.Error("[a:{id}] No asset loader for type: {type}", assetId, asset.AssetData.AssetType.Name);
                    asset.AssetData.SetAssetInternalStatus(ResourceStatus.Error);
                    return;
                }

                if (assetId.IsInvalid)
                {
                    EngLog.Assets.Error("Invalid asset id provided to reload asset.");
                    return;
                }

                if (_assetIdProvider == null)
                {
                    EngLog.Assets.Error("No asset id provider has been assigned and thus no resources can be reloaded.");
                    return;
                }

                if (!_assetIdProvider.TryGetLocalAndAssetPathsForId(assetId, out string? localPath, out string? assetPath))
                {
                    EngLog.Assets.Error("No reloadable asset found in filesystem with id: {id}", assetId);
                    return;
                }

                object? assetRef = asset.AssetReference.Target;
                if (assetRef == null)
                {
                    assetRef = loader.FactoryCreateDef(asset.AssetData);
                    asset.AssetReference.Target = assetRef;
                }

                if (newName != null)
                    asset.AssetData.SetAssetInternalName(newName);
                asset.AssetData.SetAssetInternalStatus(ResourceStatus.Running);

                ValueTask task = _scheduler.Schedule(loader, assetId, (IAssetDefinition)assetRef, asset.AssetData, assetPath ?? localPath, localPath, true);

                if (synchronous)
                {
                    lockScope.Dispose();
                    task.AsTask().Wait();
                }
            }
        }

        /// <summary>Thread-safe</summary>
        private T CreateBadAsset<T>(AssetId id, bool nullifyIfExists = false) where T : class, IAssetDefinition => (T)CreateBadAsset(typeof(T), id, nullifyIfExists);

        /// <summary>Thread-safe</summary>
        private object CreateBadAsset(Type type, AssetId id, bool nullifyIfExists = false)
        {
            using Lock.Scope lockScope = _loadLock.EnterScope();

            if (id.IsInvalid)
            {
                if (_invalidAssets.TryGetValue(type, out ValueTuple<IAssetDefinition, IInternalAssetData> invalidTuple))
                    return invalidTuple.Item1;

                ref readonly IAssetLoader loader = ref _loaders.GetValueRefOrNullRef(type);
                if (Unsafe.IsNullRef(in loader))
                {
                    throw new NotSupportedException(type.Name);
                }

                IInternalAssetData assetData = loader.FactoryCreateNull(id);
                IAssetDefinition assetDef = loader.FactoryCreateDef(assetData);

                _invalidAssets[type] = (assetDef, assetData);

                assetData.SetAssetInternalStatus(ResourceStatus.Bad);
                return assetDef;
            }

            ref LoadedAsset asset = ref CollectionsMarshal.GetValueRefOrNullRef(_loadedAssets, id);
            if (Unsafe.IsNullRef(ref asset))
            {
                ref readonly IAssetLoader loader = ref _loaders.GetValueRefOrNullRef(type);
                if (Unsafe.IsNullRef(in loader))
                {
                    throw new NotSupportedException(type.Name);
                }

                IInternalAssetData assetData = loader.FactoryCreateNull(id);
                IAssetDefinition assetDef = loader.FactoryCreateDef(assetData);

                LoadedAsset newAsset = new LoadedAsset(assetDef, assetData);
                _loadedAssets[id] = newAsset;

                assetData.SetAssetInternalStatus(ResourceStatus.Error);
                return assetDef;
            }
            else
            {
                object? assetRef = asset.AssetReference.Target;
                if (assetRef == null)
                {
                    ref readonly IAssetLoader loader = ref _loaders.GetValueRefOrNullRef(type);
                    Debug.Assert(!Unsafe.IsNullRef(in loader));

                    assetRef = loader.FactoryCreateDef(asset.AssetData);
                    asset.AssetReference.Target = assetRef;
                }

                if (nullifyIfExists)
                {
                    asset.AssetData.SetAssetInternalStatus(ResourceStatus.Error);
                }

                return assetRef;
            }
        }

        /// <summary>Thread-safe</summary>
        private object LoadAssetImpl(Type type, string sourcePath, string? assetPath, AssetId id, bool synchronous, BundleReader? bundleToReadFrom)
        {
            using Lock.Scope lockScope = _loadLock.EnterScope();

            ref LoadedAsset asset = ref CollectionsMarshal.GetValueRefOrNullRef(_loadedAssets, id);
            if (!Unsafe.IsNullRef(ref asset))
            {
                object? assetRef = asset.AssetReference.Target;
                if (assetRef == null)
                {
                    ref readonly IAssetLoader loader = ref _loaders.GetValueRefOrNullRef(type);
                    Debug.Assert(!Unsafe.IsNullRef(in loader));

                    assetRef = loader.FactoryCreateDef(asset.AssetData);
                    asset.AssetReference.Target = assetRef;
                }

                return assetRef;
            }
            else
            {
                ref readonly IAssetLoader loader = ref _loaders.GetValueRefOrNullRef(type);
                if (Unsafe.IsNullRef(in loader))
                {
                    EngLog.Assets.Error("[a:{p}] No asset loader for type: {n}", sourcePath, type.Name);
                    return CreateBadAsset(type, id);
                }

                IInternalAssetData assetData = loader.FactoryCreateNull(id);
                IAssetDefinition assetDef = loader.FactoryCreateDef(assetData);

                LoadedAsset newAsset = new LoadedAsset(assetDef, assetData);
                _loadedAssets[id] = newAsset;

                assetData.SetAssetInternalName(Path.GetFileNameWithoutExtension(sourcePath));
                assetData.SetAssetInternalStatus(ResourceStatus.Running);

                ValueTask task = _scheduler.Schedule(loader, id, assetDef, assetData, assetPath ?? sourcePath, sourcePath, false);

                if (synchronous)
                {
                    lockScope.Dispose();
                    task.AsTask().Wait();
                }

                return assetDef;
            }
        }

        /// <summary>Thread-safe</summary>
        public static T LoadAsset<T>(ReadOnlySpan<char> sourcePath, int localId = AssetId.NoLocalId, bool synchronous = false) where T : class, IAssetDefinition
        {
            AssetManager @this = NullableUtility.ThrowIfNull(s_instance);
            if (@this._assetIdProvider == null)
            {
                EngLog.Assets.Error("[a:{path}]: No asset id provider has been assigned and thus no resources can be loaded.", sourcePath.ToString());
                return @this.CreateBadAsset<T>(AssetId.Invalid);
            }

            if (!@this._assetIdProvider.TryLookupIdForPath(sourcePath, out FileId id))
            {
                EngLog.Assets.Error("[a:{path}]: Failed to find asset id", sourcePath.ToString());
                return @this.CreateBadAsset<T>(AssetId.Invalid);
            }

            @this._assetIdProvider.TryGetAssetPathForId(id, out string? assetPath);
            return (T)@this.LoadAssetImpl(typeof(T), sourcePath.ToString(), assetPath, new AssetId(id, localId), synchronous, null);
        }

        /// <summary>Thread-safe</summary>
        public static T LoadAsset<T>(AssetId assetId, bool synchronous = false) where T : class, IAssetDefinition
        {
            AssetManager @this = NullableUtility.ThrowIfNull(s_instance);
            if (@this._assetIdProvider == null)
            {
                EngLog.Assets.Error("[a:{id}]: No asset id provider has been assigned and thus no resources can be loaded.", assetId);
                return @this.CreateBadAsset<T>(AssetId.Invalid);
            }

            if (assetId.IsInvalid)
            {
                EngLog.Assets.Error("[a:{id}]: Invalid asset id provided to load asset.", assetId);
                return @this.CreateBadAsset<T>(AssetId.Invalid);
            }

            if (!@this._assetIdProvider.TryGetLocalAndAssetPathsForId(assetId, out string? localPath, out string? assetPath))
            {
                EngLog.Assets.Error("[a:{id}]: No asset found in filesystem", assetId);
                return @this.CreateBadAsset<T>(assetId);
            }

            return (T)@this.LoadAssetImpl(typeof(T), localPath, assetPath, assetId, synchronous, null);
        }

        /// <summary>Thread-safe</summary>
        public static object LoadAsset(Type type, AssetId assetId, bool synchronous = false)
        {
            if (!type.IsAssignableTo(typeof(IAssetDefinition)))
            {
                EngLog.Assets.Error("[a:{id}]: Cannot load asset from generic type that does not inherit from: {t}", assetId, typeof(IAssetDefinition));
            }

            AssetManager @this = NullableUtility.ThrowIfNull(s_instance);
            if (@this._assetIdProvider == null)
            {
                EngLog.Assets.Error("[a:{id}]: No asset id provider has been assigned and thus no resources can be loaded.", assetId);
                return @this.CreateBadAsset(type, AssetId.Invalid);
            }

            if (assetId.IsInvalid)
            {
                EngLog.Assets.Error("[a:{id}]: Invalid asset id provided to load asset.", assetId);
                return @this.CreateBadAsset(type, AssetId.Invalid);
            }

            if (!@this._assetIdProvider.TryGetLocalAndAssetPathsForId(assetId, out string? localPath, out string? assetPath))
            {
                EngLog.Assets.Error("[a:{id}]: No asset found in filesystem", assetId);
                return @this.CreateBadAsset(type, assetId);
            }

            return @this.LoadAssetImpl(type, localPath, assetPath, assetId, synchronous, null);
        }

        /// <summary>Thread-safe</summary>
        public static object LoadAsset(Type type, ReadOnlySpan<char> sourcePath, int localId = AssetId.NoLocalId, bool synchronous = false)
        {
            if (!type.IsAssignableTo(typeof(IAssetDefinition)))
            {
                EngLog.Assets.Error("[a:{id}]: Cannot load asset from generic type that does not inherit from: {t}", sourcePath.ToString(), typeof(IAssetDefinition));
            }

            AssetManager @this = NullableUtility.ThrowIfNull(s_instance);
            if (@this._assetIdProvider == null)
            {
                EngLog.Assets.Error("[a:{id}]: No asset id provider has been assigned and thus no resources can be loaded.", sourcePath.ToString());
                return @this.CreateBadAsset(type, AssetId.Invalid);
            }

            if (@this._assetIdProvider == null)
            {
                EngLog.Assets.Error("[a:{path}]: No asset id provider has been assigned and thus no resources can be loaded.", sourcePath.ToString());
                return @this.CreateBadAsset(type, AssetId.Invalid);
            }

            if (!@this._assetIdProvider.TryLookupIdForPath(sourcePath, out FileId id))
            {
                EngLog.Assets.Error("[a:{path}]: Failed to find asset id", sourcePath.ToString());
                return @this.CreateBadAsset(type, AssetId.Invalid);
            }

            @this._assetIdProvider.TryGetAssetPathForId(id, out string? assetPath);
            return @this.LoadAssetImpl(type, sourcePath.ToString(), assetPath, new AssetId(id, localId), synchronous, null);
        }

        /// <summary>Thread-safe</summary>
        public static void WaitForAssetLoad(AssetId assetId)
        {
            AssetManager @this = NullableUtility.ThrowIfNull(s_instance);
            if (@this._scheduler.TryGetRunningTask(assetId, out Task? task))
                task?.Wait();
        }

        /// <summary>Thread-safe</summary>
        public static void WaitForAssetLoad(ReadOnlySpan<char> sourcePath, int localId = AssetId.NoLocalId)
        {
            AssetManager @this = NullableUtility.ThrowIfNull(s_instance);
            if (@this._assetIdProvider == null)
            {
                EngLog.Assets.Error("[a:{path}]: No asset id provider has been assigned and thus no resource id can be realized from path.", sourcePath.ToString());
                return;
            }

            if (!@this._assetIdProvider.TryLookupIdForPath(sourcePath, out FileId id))
            {
                EngLog.Assets.Error("[a:{path}]: Failed to find asset id", sourcePath.ToString());
                return;
            }

            WaitForAssetLoad(new AssetId(id, localId));
        }

        /// <summary>Thread-safe</summary>
        public static async Task WaitForAssetLoadAsync(AssetId assetId)
        {
            //empty for now
        }

        /// <summary>Thread-safe</summary>
        public static async Task WaitForAssetLoadAsync(string sourcePath, int localId = AssetId.NoLocalId)
        {
            AssetManager @this = NullableUtility.ThrowIfNull(s_instance);
            if (@this._assetIdProvider == null)
            {
                EngLog.Assets.Error("[a:{path}]: No asset id provider has been assigned and thus no resource id can be realized from path.", sourcePath.ToString());
                return;
            }

            if (!@this._assetIdProvider.TryLookupIdForPath(sourcePath, out FileId id))
            {
                EngLog.Assets.Error("[a:{path}]: Failed to find asset id", sourcePath.ToString());
                return;
            }

            await WaitForAssetLoadAsync(new AssetId(id, localId));
        }

        /// <summary>Not thread-safe</summary>
        public static void AddAssetReloadedCallback<T>(AssetReloadedCallback callback) where T : class, IAssetDefinition
        {
            AssetManager @this = NullableUtility.ThrowIfNull(s_instance);
            @this._callbacks.AddCallback<T>(callback);
        }

        /// <summary>Not thread-safe</summary>
        public static void RemoveAssetReloadedCallback<T>(AssetReloadedCallback callback) where T : class, IAssetDefinition
        {
            AssetManager @this = NullableUtility.ThrowIfNull(s_instance);
            @this._callbacks.RemoveCallback<T>(callback);
        }

        /// <summary>Thread-safe</summary>
        public static void ListenForAssetLoad<TSelf, T>(T asset, TSelf obj, Action<T, bool>? action) where TSelf : class where T : class, IAssetDefinition
        {
            AssetManager @this = NullableUtility.ThrowIfNull(s_instance);
            if (action != null)
                @this._listener.SetAssetLoadListener<TSelf, T>(asset.Id, obj, action);
            else
                @this._listener.RemoveAssetLoadListener<TSelf, T>(asset.Id, obj);
        }

        /// <summary>Not thread-safe</summary>
        public void LockInIdProvider(IAssetIdProvider provider)
        {
            if (_assetIdProvider == null)
            {
                _assetIdProvider = provider;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void LockInCacheProvider(IAssetCacheProvider provider)
        {
            if (_assetCacheProvider == null)
            {
                _assetCacheProvider = provider;
            }
        }

        /// <summary>Thread-safe</summary>
        public void RegisterCustomAsset<T>(IAssetLoader loader) where T : IAssetDefinition
        {
            lock (_loadLock)
            {
                Dictionary<Type, IAssetLoader> dict = _loaders.ToDictionary();
                dict[typeof(T)] = loader;

                _loaders = dict.ToFrozenDictionary();
            }
        }

        /// <summary>Thread-safe</summary>
        internal void RegisterAssetLoad(AssetId id, IAssetDefinition asset, bool wasReloaded)
        {
            _loadCallbacks.Enqueue(new AssetLoadCallback(id, asset, wasReloaded));
        }

        public IAssetIdProvider IdProvider => _assetIdProvider!;
        public IAssetCacheProvider CacheProvider => _assetCacheProvider!;

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

        private readonly record struct AssetLoadCallback(AssetId Id, IAssetDefinition Asset, bool WasReloaded);
    }

    public record class ImmutableAssets(
        TextureAsset DefaultWhite,
        TextureAsset DefaultBlack,
        TextureAsset DefaultNormal,
        TextureAsset DefaultMask,
        TextureAsset DebugTexLoading,
        TextureAsset DebugTexError);
}
