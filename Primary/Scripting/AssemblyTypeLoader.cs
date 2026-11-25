using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Primary.Collections;
using Primary.Utility;

namespace Primary.Scripting
{
    public sealed class AssemblyTypeLoader
    {
        private readonly ScriptingManager _manager;

        private readonly Dictionary<Type, List<TypeLoadCallback>> _callbacks;

        internal AssemblyTypeLoader(ScriptingManager manager)
        {
            _manager = manager;

            _callbacks = new Dictionary<Type, List<TypeLoadCallback>>();

            manager.AssemblyUnloading += OnAssemblyUnloading;
        }

        private void OnAssemblyUnloading(Assembly assembly)
        {
            using RentedList<Type> removalQueue = new RentedList<Type>();
            foreach (var (type, list) in _callbacks)
            {
                if (type.Assembly == assembly)
                {
                    removalQueue.Add(type);
                }
                else
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        TypeLoadCallback callback = list[i];
                        if (callback.Method.DeclaringType?.Assembly == assembly)
                        {
                            list.RemoveAt(i--);
                        }
                    }

                    if (list.Count == 0)
                    {
                        removalQueue.Add(type);
                    }
                }
            }

            if (!removalQueue.IsEmpty)
            {
                foreach (Type type in removalQueue)
                {
                    _callbacks.Remove(type);
                }
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public void LoadTypesOnAssembly(Assembly assembly)
        {
            Type[] types = assembly.GetTypes();
            int real = 0;

            foreach (Type type in types)
            {
                Attribute[] attributesArray = Attribute.GetCustomAttributes(type);
                foreach (Attribute attribute in attributesArray)
                {
                    if (_callbacks.TryGetValue(attribute.GetType(), out List<TypeLoadCallback>? callbacks))
                    {
                        ++real;

                        foreach (TypeLoadCallback callback in callbacks)
                        {
                            try
                            {
                                callback(type, attribute, attributesArray.AsSpan());
                            }
                            catch (Exception ex)
                            {
                                EngLog.Script.Warning(ex, "Failed to invoke type load callback for '{attrib}'", attribute.GetType().FullName);
                            }
                        }
                    }
                }
            }

            EngLog.Script.Information("Loaded #{real} (#{to} total) types with callbacks from assembly '{asm}'", real, types.Length, assembly.GetName().Name ?? assembly.FullName);
        }

        public void AddCallbackForTypeLoad<T>(TypeLoadCallback callback)
        {
            if (_callbacks.TryGetValue(typeof(T), out List<TypeLoadCallback>? list))
            {
                list.AddUnique(callback);
            }
            else
            {
                _callbacks[typeof(T)] = [callback];
            }
        }

        public void RemoveCallbackForTypeLoad<T>(TypeLoadCallback callback)
        {
            if (_callbacks.TryGetValue(typeof(T), out List<TypeLoadCallback>? list) && list.Remove(callback) && list.Count == 0)
            {
                _callbacks.Remove(typeof(T));
            }
        }
    }

    public delegate void TypeLoadCallback(Type type, Attribute targetAttribute, ReadOnlySpan<Attribute> allAttributes);
}
