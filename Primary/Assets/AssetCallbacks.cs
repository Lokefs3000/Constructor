using Primary.Assets.Types;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Assets
{
    internal sealed class AssetCallbacks
    {
        private Dictionary<Type, List<AssetReloadedCallback>> _callbacks;

        internal AssetCallbacks()
        {
            _callbacks = new Dictionary<Type, List<AssetReloadedCallback>>();
        }

        internal void Invoke(Type type, AssetId id)
        {
            if (_callbacks.TryGetValue(type, out List<AssetReloadedCallback>? callbacks))
            {
                foreach (AssetReloadedCallback callback in callbacks)
                {
                    try
                    {
                        callback(type, id);
                    }
                    catch (Exception ex)
                    {
                        EngLog.Assets.Error(ex, "Error invoking callback for {c}", callback);
                    }
                }
            }
        }

        internal void AddCallback<T>(AssetReloadedCallback callback) where T : IAssetDefinition
        {
            Type t = typeof(T);
            if (!_callbacks.TryGetValue(t, out List<AssetReloadedCallback>? callbacks))
            {
                callbacks = [callback];
                _callbacks.Add(t, callbacks);
            }
            else
            {
                callbacks.AddUnique(callback);
            }
        }

        internal void RemoveCallback<T>(AssetReloadedCallback callback) where T : IAssetDefinition
        {
            Type t = typeof(T);
            if (_callbacks.TryGetValue(t, out List<AssetReloadedCallback>? callbacks))
            {
                callbacks.Remove(callback);

                if (callbacks.Count == 0)
                    _callbacks.Remove(t);
            }
        }
    }

    public delegate void AssetReloadedCallback(Type type, AssetId id);
}
