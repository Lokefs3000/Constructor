using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Editor.PropertiesViewer
{
    internal class PropReflectionCache
    {
        private Dictionary<Assembly, AsmReflectionCache> _cache;

        internal PropReflectionCache()
        {
            _cache = new Dictionary<Assembly, AsmReflectionCache>();
        }

        internal CachedReflection Get(Type type)
        {
            Assembly assembly = type.Assembly;
            if (_cache.TryGetValue(assembly, out AsmReflectionCache? cache))
                return cache.Get(type);

            cache = new AsmReflectionCache(assembly);
            _cache[assembly] = cache;

            return cache.Get(type);
        }
    }
}
