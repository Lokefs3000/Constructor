using Primary.Collections;
using Primary.Components;
using Primary.Utility;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Primary.Scenes.Components
{
    public sealed class ComponentCallbacks
    {
        private Dictionary<Type, Callbacks> _callbacks;

        internal ComponentCallbacks()
        {
            _callbacks = new Dictionary<Type, Callbacks>();
        }

        internal void RemoveListenersFrom(HashSet<Assembly> assemblies)
        {
            using RentedList<Type> pendingRemovals = new RentedList<Type>();

            foreach (var kvp in _callbacks)
            {
                Callbacks callbacks = kvp.Value;

                for (int i = 0; i < callbacks.Add.Count; i++)
                {
                    ComponentCallback callback = callbacks.Add[i];
                    if (assemblies.Contains(callback.GetType().Assembly))
                        callbacks.Add.RemoveAt(i--);
                }

                for (int i = 0; i < callbacks.Removed.Count; i++)
                {
                    ComponentCallback callback = callbacks.Removed[i];
                    if (assemblies.Contains(callback.GetType().Assembly))
                        callbacks.Removed.RemoveAt(i--);
                }

                if (callbacks.Add.Count == 0 && callbacks.Removed.Count == 0)
                    pendingRemovals.Add(kvp.Key);
            }

            if (!pendingRemovals.IsEmpty)
            {
                foreach (Type type in pendingRemovals)
                {
                    _callbacks.Remove(type);
                }
            }
        }

        internal void InvokeAdded(SceneEntity entity, Type type)
        {
            if (_callbacks.TryGetValue(type, out Callbacks value))
            {
                foreach (ComponentCallback callback in value.Add)
                {
                    try
                    {
                        callback(entity);
                    }
                    catch (Exception ex)
                    {
                        EngLog.Scene.Error(ex, "Failed to invoke add component callback for: {c}", callback);
                    }
                }
            }
        }

        internal void InvokeRemoved(SceneEntity entity, Type type)
        {
            if (_callbacks.TryGetValue(type, out Callbacks value))
            {
                foreach (ComponentCallback callback in value.Removed)
                {
                    try
                    {
                        callback(entity);
                    }
                    catch (Exception ex)
                    {
                        EngLog.Scene.Error(ex, "Failed to invoke remove component callback for: {c}", callback);
                    }
                }
            }
        }

        /// <summary>Not thread-safe</summary>
        internal void AddComponentAddedCallback<T>(ComponentCallback callback) where T : IComponent
        {
            Type t = typeof(T);
            if (!_callbacks.TryGetValue(t, out Callbacks callbacks))
            {
                callbacks = new Callbacks(new List<ComponentCallback>(), new List<ComponentCallback>());
                _callbacks.Add(t, callbacks);
            }

            callbacks.Add.AddUnique(callback);
        }

        /// <summary>Not thread-safe</summary>
        internal void RemoveComponentAddedCallback<T>(ComponentCallback callback) where T : IComponent
        {
            Type t = typeof(T);
            if (_callbacks.TryGetValue(t, out Callbacks callbacks))
            {
                callbacks.Add.Remove(callback);

                if (callbacks.Add.Count == 0 && callbacks.Removed.Count == 0)
                    _callbacks.Remove(t);
            }
        }

        /// <summary>Not thread-safe</summary>
        internal void AddComponentRemovedCallback<T>(ComponentCallback callback) where T : IComponent
        {
            Type t = typeof(T);
            if (!_callbacks.TryGetValue(t, out Callbacks callbacks))
            {
                callbacks = new Callbacks(new List<ComponentCallback>(), new List<ComponentCallback>());
                _callbacks.Add(t, callbacks);
            }

            callbacks.Removed.AddUnique(callback);
        }

        /// <summary>Not thread-safe</summary>
        internal void RemoveComponentRemovedCallback<T>(ComponentCallback callback) where T : IComponent
        {
            Type t = typeof(T);
            if (_callbacks.TryGetValue(t, out Callbacks callbacks))
            {
                callbacks.Removed.Remove(callback);

                if (callbacks.Add.Count == 0 && callbacks.Removed.Count == 0)
                    _callbacks.Remove(t);
            }
        }

        private readonly record struct Callbacks(List<ComponentCallback> Add, List<ComponentCallback> Removed);
    }

    public delegate void ComponentCallback(SceneEntity entity);
}
