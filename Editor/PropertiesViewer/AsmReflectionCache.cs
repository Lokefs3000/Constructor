using Primary.Common;
using Primary.Components;
using Primary.Editor;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Editor.PropertiesViewer
{
    internal sealed class AsmReflectionCache
    {
        private Assembly _assembly;
        private Dictionary<Type, CachedReflection> _cache;

        internal AsmReflectionCache(Assembly assembly)
        {
            _assembly = assembly;
            _cache = new Dictionary<Type, CachedReflection>();
        }

        internal CachedReflection Get(Type type)
        {
            Debug.Assert(type.Assembly == _assembly);

            if (_cache.TryGetValue(type, out CachedReflection cached))
                return cached;

            if (type.GetCustomAttribute<InspectorHiddenAttribute>() != null)
            {
                cached = new CachedReflection(false, Array.Empty<ReflectionField>());

                _cache[type] = cached;
                return cached;
            }

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            using PoolArray<ReflectionField> refl = ArrayPool<ReflectionField>.Shared.Rent(fields.Length + propertyInfos.Length);
            int i = 0;
            
            for (int j = 0; j < fields.Length; j++)
            {
                FieldInfo fi = fields[j];
                if (fi.GetCustomAttribute<IgnoreDataMemberAttribute>() == null ||
                    fi.GetCustomAttribute<InspectorHiddenAttribute>() == null)
                {
                    refl[i++] = fi;
                }
            }

            for (int j = 0; j < propertyInfos.Length; j++)
            {
                PropertyInfo pi = propertyInfos[j];
                if (pi.GetCustomAttribute<IgnoreDataMemberAttribute>() == null ||
                    pi.GetCustomAttribute<InspectorHiddenAttribute>() == null)
                {
                    refl[i++] = pi;
                }
            }

            CachedReflection reflection = new CachedReflection(true, refl.AsSpan(0, i).ToArray());
            _cache[type] = reflection;

            return reflection;
        }
    }

    internal record struct CachedReflection(bool IsSerialized, ReflectionField[] Fields);
}
