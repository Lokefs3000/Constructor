using Primary.Assets.Types;
using Primary.Collections;
using Primary.Pooling;
using Primary.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Assets
{
    internal sealed class AssetListener
    {
        private readonly Lock _lock;

        private readonly Dictionary<AssetId, AssetData> _data;
        private readonly ConditionalWeakTable<object, HashSet<AssetId>> _references;

        private readonly ObjectPool<WeakReference> _weakPool;

        internal AssetListener()
        {
            _lock = new Lock();

            _data = new Dictionary<AssetId, AssetData>();
            _references = new ConditionalWeakTable<object, HashSet<AssetId>>();

            _weakPool = new ObjectPool<WeakReference>(new WeakReferencePolicy());
        }

        private WeakReference GetWeakReference(object obj)
        {
            WeakReference reference = _weakPool.Get();
            reference.Target = obj;

            return reference;
        }

        internal void CleanupCollectedReferences()
        {
            using (_lock.EnterScope())
            {
                using RentedList<AssetId> removingDict = new RentedList<AssetId>();

                foreach (var (assetId, data) in _data)
                {
                    for (int i = 0; i < data.Listeners.Count; ++i)
                    {
                        ListenerData listener = data.Listeners[i];
                        object? target = listener.Object.Target;
                        if (target == null)
                        {
                            _weakPool.Return(listener.Object);
                            data.Listeners.RemoveAt(i--);
                        }
                    }

                    if (data.Listeners.Count == 0)
                    {
                        removingDict.Add(assetId);
                    }
                }

                if (!removingDict.IsEmpty)
                {
                    foreach (AssetId assetId in removingDict)
                    {
                        _data.Remove(assetId);
                    }
                }
            }
        }

        internal void InvokeListeners(AssetId id, object asset, bool wasReloaded)
        {
            using (_lock.EnterScope())
            {
                ref AssetData data = ref CollectionsMarshal.GetValueRefOrNullRef(_data, id);
                if (!Unsafe.IsNullRef(in data))
                {
                    for (int i = 0; i < data.Listeners.Count; ++i)
                    {
                        ListenerData listener = data.Listeners[i];
                        object? target = listener.Object.Target;

                        if (target == null)
                        {
                            _weakPool.Return(listener.Object);
                            data.Listeners.RemoveAt(i--);
                        }
                        else
                        {
                            try
                            {
                                listener.Action(listener.Callback, target, asset, wasReloaded);
                            }
                            catch (Exception ex)
                            {
                                EngLog.Assets.Error(ex, "Invoking an asset load callback caused an exception");
                            }
                        }
                    }

                    if (data.Listeners.Count == 0)
                    {
                        _data.Remove(id);
                    }
                }
            }
        }

        internal void SetAssetLoadListener<TSelf, T>(AssetId id, TSelf obj, Action<T, bool> action) where TSelf : class where T : IAssetDefinition
        {
            using (_lock.EnterScope())
            {
                HashSet<AssetId> ids = _references.GetOrAdd(obj, static (x) => new HashSet<AssetId>());
                if (ids.Add(id))
                {
                    ref AssetData data = ref CollectionsMarshal.GetValueRefOrAddDefault(_data, id, out bool exists);
                    if (!exists)
                        data = new AssetData(new List<ListenerData>());
                   
                    Action<TSelf, T, bool> basic = (Action<TSelf, T, bool>)Delegate.CreateDelegate(typeof(Action<TSelf, T, bool>), null, action.Method);
                    data.Listeners.Add(new ListenerData(GetWeakReference(obj), basic, static (action, caller, asset, wasReloaded) => ((Action<TSelf, T, bool>)action)((TSelf)caller, (T)asset, wasReloaded)));
                }
            }
        }

        internal void RemoveAssetLoadListener<TSelf, T>(AssetId id, TSelf obj) where TSelf : class
        {
            using (_lock.EnterScope())
            {
                HashSet<AssetId> ids = _references.GetOrAdd(obj, static (x) => new HashSet<AssetId>());
                if (ids.Remove(id))
                {
                    ref AssetData data = ref CollectionsMarshal.GetValueRefOrNullRef(_data, id);
                    if (!Unsafe.IsNullRef(in data))
                    {
                        for (int i = 0; i < data.Listeners.Count; ++i)
                        {
                            ListenerData listener = data.Listeners[i];
                            object? target = listener.Object.Target;
                            if (target == null || target == obj)
                            {
                                _weakPool.Return(listener.Object);
                                data.Listeners.RemoveAt(i--);
                            }
                        }

                        if (data.Listeners.Count == 0)
                        {
                            _data.Remove(id);
                        }
                    }
                }
            }
        }

        private readonly record struct AssetData(List<ListenerData> Listeners);
        private readonly record struct ListenerData(WeakReference Object, object Callback, Action<object, object, object, bool> Action);

        private sealed class WeakReferencePolicy : IObjectPoolPolicy<WeakReference>
        {
            public WeakReference Create()
            {
                return new WeakReference(null);
            }

            public bool Return(ref WeakReference obj)
            {
                obj.Target = null;
                return true;
            }
        }
    }
}
