using Microsoft.Extensions.DependencyModel;
using Primary.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Editor.Reflection
{
    public sealed class AssemblyTypeLoader : IDisposable
    {
        private Dictionary<Type, List<AssemblyTypeCallback>> _callbacks;

        private FrozenDictionary<Type, List<AssemblyTypeCallback>>? _cachedFrozenDict;

        private bool _disposedValue;

        internal AssemblyTypeLoader()
        {
            _callbacks = new Dictionary<Type, List<AssemblyTypeCallback>>();

            _cachedFrozenDict = null;

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ScanLoadedAssemblies()
        {
            ScanAssemblyForTypes(Assembly.GetAssembly(typeof(EditorRuntime))!);
        }

        private void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.IsDynamic)
                return;

            ScanAssemblyForTypes(args.LoadedAssembly);
        }

        private void ScanAssemblyForTypes(Assembly assembly)
        {
            DateTime scanStart = DateTime.Now;

            if (_cachedFrozenDict == null)
                _cachedFrozenDict = _callbacks.ToFrozenDictionary();

            ConcurrentBag<Type> typesWithCallbacks = new ConcurrentBag<Type>();

            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                foreach (CustomAttributeData? attributeData in type.CustomAttributes)
                {
                    if (_cachedFrozenDict.ContainsKey(attributeData.AttributeType))
                    {
                        typesWithCallbacks.Add(type);
                        break;
                    }
                }
            }

            while (typesWithCallbacks.TryTake(out Type? result))
            {
                foreach (Attribute attribute in result.GetCustomAttributes(true))
                {
                    if (_cachedFrozenDict.TryGetValue(attribute.GetType(), out List<AssemblyTypeCallback>? callbacks))
                    {
                        foreach (AssemblyTypeCallback callback in callbacks)
                        {
                            try
                            {
                                callback(result, attribute);
                            }
                            catch (Exception ex)
                            {
                                EdLog.Reflection.Error(ex, "Failed to invoke callback for attribute {attr} ({t})", attribute, result);
                            }
                        }
                    }
                }
            }

            EdLog.Reflection.Debug("Finished scanning assembly {asm} for types in {secs:F3} seconds", assembly.GetName().Name ?? assembly.FullName, (DateTime.Now - scanStart).TotalSeconds);
        }

        public void AddCallback<T>(AssemblyTypeCallback callback) where T : Attribute
        {
            Type t = typeof(T);

            if (_callbacks.TryGetValue(t, out List<AssemblyTypeCallback>? list))
            {
                list.AddUnique(callback);
            }
            else
            {
                list = [callback];

                _callbacks[t] = list;
                _cachedFrozenDict = null;
            }
        }

        public void RemoveCallback<T>(AssemblyTypeCallback callback) where T : Attribute
        {
            Type t = typeof(T);

            if (_callbacks.TryGetValue(t, out List<AssemblyTypeCallback>? list))
            {
                if (list.Remove(callback) && list.Count == 0)
                {
                    _callbacks.Remove(t);
                    _cachedFrozenDict = null;
                }
            }
        }
    }

    public delegate void AssemblyTypeCallback(Type type, object attribute);
}
