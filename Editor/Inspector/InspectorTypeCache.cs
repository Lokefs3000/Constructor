using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Primary.Common;

namespace Editor.Inspector
{
    internal class InspectorTypeCache
    {
        private Dictionary<Type, FrozenDictionary<string, object>> _typeCache;

        internal InspectorTypeCache()
        {
            _typeCache = new Dictionary<Type, FrozenDictionary<string, object>>();
        }

        internal FrozenDictionary<string, object> GetTypeCache(Type type)
        {
            if (_typeCache.TryGetValue(type, out FrozenDictionary<string, object>? value))
                return value;

            Dictionary<string, object> tempDict = new Dictionary<string, object>();

            Type? currentType = type;

            do
            {
                FieldInfo[] fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo field in fields)
                {
                    if (Flags.HasFlag(field.Attributes, FieldAttributes.Public))
                    {
                        tempDict.TryAdd(field.Name, field);
                    }
                }

                PropertyInfo[] properties = currentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (PropertyInfo property in properties)
                {
                    MethodInfo? method = property.GetMethod ?? property.SetMethod;
                    if (method != null && Flags.HasFlag(method.Attributes, MethodAttributes.Public))
                    {
                        tempDict.TryAdd(property.Name, property);
                    }
                }
            } while ((currentType = currentType.BaseType!) != null);

            value = tempDict.ToFrozenDictionary();

            _typeCache.Add(type, value);
            return value;
        }
    }
}
