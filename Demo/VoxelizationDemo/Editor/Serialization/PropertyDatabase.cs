using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace VoxelizationDemo.Editor.Serialization
{
    internal sealed class PropertyDatabase
    {
        private readonly ConcurrentDictionary<Type, PropertyList> _entries;

        internal PropertyDatabase()
        {
            _entries = new ConcurrentDictionary<Type, PropertyList>();
        }

        internal PropertyList QueryProperties(Type type)
        {
            return _entries.GetOrAdd(type, CreateEntry);
        }

        private PropertyList CreateEntry(Type type)
        {
            PropertyProvider? provider = null;

            // TODO: Implement predefined property providers from fx. source generators
            provider ??= new ReflectionPropertyProvider(type);

            Dictionary<byte[], ValueSource> propertySources = new Dictionary<byte[], ValueSource>();

            SerializationBag bag = provider.Properties;
            foreach (SerializationProperty property in bag.Properties)
            {
                if (property.IsProperty)
                {
                    PropertyInfo info = property.Property;

                    MethodInfo? setMethod = info.SetMethod;
                    MethodInfo? getMethod = info.GetMethod;
                    if (setMethod == null || getMethod == null)
                    {
                        AppLog.Editor.Warning("Property '{n}' get and/or set method is null and won't be serializable", info.Name);
                        continue;
                    }

                    byte[] nameAsBytes = new byte[info.Name.Length];
                    Encoding.UTF8.GetBytes(info.Name, nameAsBytes);

                    ref ValueSource valueSource = ref CollectionsMarshal.GetValueRefOrAddDefault(propertySources, nameAsBytes, out bool exists);
                    if (!exists)
                    {
                        if (false)
                        {
                            Type proxyClass = typeof(PropertyMethodProxy<,>).MakeGenericType(info.DeclaringType!, info.PropertyType);

                            Type setterDelegateType = typeof(Action<,>).MakeGenericType(info.DeclaringType!, info.PropertyType);
                            Type getterDelegateType = typeof(Func<,>).MakeGenericType(info.DeclaringType!, info.PropertyType);

                            Delegate setterDelegate = Delegate.CreateDelegate(setterDelegateType, setMethod);
                            Delegate setterProxyDelegate = Delegate.CreateDelegate(typeof(ValueSourceSetter<>).MakeGenericType(info.PropertyType), proxyClass.GetMethod("Set", BindingFlags.Public | BindingFlags.Static)!);

                            Delegate getterDelegate = Delegate.CreateDelegate(getterDelegateType, getMethod);
                            Delegate getterProxyDelegate = Delegate.CreateDelegate(typeof(ValueSourceGetter<>).MakeGenericType(info.PropertyType), proxyClass.GetMethod("Get", BindingFlags.Public | BindingFlags.Static)!);

                            valueSource = new ValueSource(setterDelegate, setterProxyDelegate, getterDelegate, getterProxyDelegate, info.PropertyType);
                        }
                        else
                        {
                            valueSource = new ValueSource(info);
                        }
                    }
                    else
                    {
                        AppLog.Editor.Warning("Duplicate serialization property name '{n}'", info.Name);
                    }
                }
                else
                {
                    FieldInfo info = property.Field;

                    ReadOnlySpan<char> name = info.Name;
                    if (name.EndsWith(">k__BackingField"))
                        name = name[1..(name.Length - ">k__BackingField".Length)];

                    byte[] nameAsBytes = new byte[name.Length];
                    Encoding.UTF8.GetBytes(name, nameAsBytes);

                    ref ValueSource valueSource = ref CollectionsMarshal.GetValueRefOrAddDefault(propertySources, nameAsBytes, out bool exists);
                    if (!exists)
                    {
                        valueSource = new ValueSource(info);
                    }
                    else
                    {
                        AppLog.Editor.Warning("Duplicate serialization property name '{n}'", name.ToString());
                    }
                }
            }

            return new PropertyList(provider, propertySources.ToFrozenDictionary());
        }

        private sealed class PropertyMethodProxy<TObject, TValue>
        {
            public static void Set(Delegate action, object boxedValue, TValue? value) => ((Action<TObject, TValue?>)action)((TObject)boxedValue, value);
            public static TValue? Get(Delegate func, object boxedValue) => ((Func<TObject, TValue?>)func)((TObject)boxedValue);
        }
    }
}
