using Primary.Common;
using Primary.Editor;
using System.Buffers;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;

namespace Editor.Inspector
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

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            using PoolArray<ReflectionField> refl = ArrayPool<ReflectionField>.Shared.Rent(fields.Length + propertyInfos.Length);
            int i = 0;

            for (int j = 0; j < fields.Length; j++)
            {
                FieldInfo fi = fields[j];
                if (fi.GetCustomAttribute<IgnoreDataMemberAttribute>() == null &&
                    fi.GetCustomAttribute<InspectorHiddenAttribute>() == null)
                {
                    refl[i++] = fi;
                }
            }

            for (int j = 0; j < propertyInfos.Length; j++)
            {
                PropertyInfo pi = propertyInfos[j];
                if (pi.GetCustomAttribute<IgnoreDataMemberAttribute>() == null &&
                    pi.GetCustomAttribute<InspectorHiddenAttribute>() == null)
                {
                    refl[i++] = pi;
                }
            }

            CachedReflection reflection = new CachedReflection(type.GetCustomAttribute<InspectorHiddenAttribute>() == null, refl.AsSpan(0, i).ToArray());
            Array.Sort(reflection.Fields, (x, y) => x.Name.CompareTo(y.Name));

            _cache[type] = reflection;
            return reflection;
        }
    }

    internal record struct CachedReflection(bool IsSerialized, ReflectionField[] Fields);
}
