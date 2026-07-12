using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Primary.Utility;

namespace PrimaryEditor.Reflection
{
    public sealed class AssemblyTypeLoader
    {
        private Dictionary<Type, List<AssemblyTypeCallback>> _callbacks;

        internal AssemblyTypeLoader()
        {
            _callbacks = new Dictionary<Type, List<AssemblyTypeCallback>>();
        }

        internal void ScanAssemblyForTypes(Assembly assembly)
        {
            long startTimestamp = Stopwatch.GetTimestamp();

            foreach (TypeInfo typeInfo in assembly.DefinedTypes)
            {
                foreach (Attribute? attributeData in typeInfo.GetCustomAttributes())
                {
                    if (_callbacks.TryGetValue(attributeData.GetType(), out List<AssemblyTypeCallback>? callbacks))
                    {
                        foreach (AssemblyTypeCallback callback in callbacks)
                        {
                            try
                            {
                                callback(typeInfo, attributeData);
                            }
                            catch (Exception ex)
                            {
                                EdLog.Reflection.Warning(ex, "Failed to invoke callback '{callback}' for attribute '{attrib}'", attributeData.GetType(), callback);
                            }
                        }
                    }
                }
            }

            EdLog.Reflection.Debug("Finished scanning assembly for types in {durr}ms", Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
        }

        public void AddCallback<T>(AssemblyTypeCallback callback) where T : Attribute
        {
            Type t = typeof(T);

            if (_callbacks.TryGetValue(t, out List<AssemblyTypeCallback>? callbacks))
            {
                callbacks.AddUnique(callback);
            }
            else
            {
                _callbacks[t] = [callback];
            }
        }

        public void RemoveCallback<T>(AssemblyTypeCallback callback)
        {
            Type t = typeof(T);

            if (_callbacks.TryGetValue(t, out List<AssemblyTypeCallback>? callbacks))
            {
                if (callbacks.Remove(callback) && callbacks.Count == 0)
                {
                    _callbacks.Remove(t);
                }
            }
        }
    }

    public delegate void AssemblyTypeCallback(Type type, object attribute);
}
