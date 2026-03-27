using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Helpers
{
    internal sealed class ObjectIndexer
    {
        private Dictionary<object, int> _objectIndices;
        private List<object> _objects;

        internal ObjectIndexer()
        {
            _objectIndices = new Dictionary<object, int>();
            _objects = new List<object>();
        }

        internal void Clear()
        {
            _objectIndices.Clear();
            _objects.Clear();
        }

        internal void CopyTo(ObjectIndexer indexer)
        {
            foreach (var kvp in _objectIndices)
                indexer._objectIndices.Add(kvp.Key, kvp.Value);
            indexer._objects.AddRange(_objects);
        }

        internal int Add(object? obj)
        {
            if (obj == null)
                return -1;
            if (_objectIndices.TryGetValue(obj, out int index))
                return index;

            index = _objects.Count;

            _objectIndices.Add(obj, index);
            _objects.Add(obj);

            return index;
        }

        internal object? Get(int index) => index == -1 ? null : _objects[index];
    }
}
