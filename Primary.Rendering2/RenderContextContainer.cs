using Primary.Rendering2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2
{
    public sealed class RenderContextContainer
    {
        private Dictionary<Type, IContextItem> _items;

        internal RenderContextContainer()
        {
            _items = new Dictionary<Type, IContextItem>();
        }

        internal T GetOrCreate<T>(Func<T> constructor) where T : class, IContextItem
        {
            if (_items.TryGetValue(typeof(T), out IContextItem? item))
                return Unsafe.As<T>(item);

            item = constructor();
            _items.Add(typeof(T), item);
            
            return Unsafe.As<T>(item);
        }

        public T? Get<T>() where T : class, IContextItem
        {
            _items.TryGetValue(typeof(T), out IContextItem? item);
            return Unsafe.As<T>(item);
        }
    }
}
