using System;
using System.Collections.Generic;
using System.Text;
using Editor.UI.Elements;

namespace Editor.Gui.Inspector
{
    internal sealed class ObjectTypePool<T> : IOpaqueObjectTypePool where T : new()
    {
        private List<T> _pool;
        private int _head;

        public ObjectTypePool()
        {
            _pool = new List<T>();
            _head = 0;
        }

        internal T GetPooledObject()
        {
            if (_head == _pool.Count)
            {
                T element = new T();
                _pool.Add(element);

                ++_head;
                return element;
            }

            return _pool[_head++];
        }

        public object GetOpaquePooledObject() => GetPooledObject()!;
    }

    internal interface IOpaqueObjectTypePool
    {
        public object GetOpaquePooledObject();
    }
}
