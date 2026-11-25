using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Rendering.Recording
{
    internal sealed class ObjectsCache
    {
        private readonly Dictionary<object, int> _objectIndices;
        private readonly List<object> _objects;

        internal ObjectsCache()
        {
            _objectIndices = new Dictionary<object, int>();
            _objects = new List<object>();
        }

        internal void ClearStoredObjects()
        {
            _objectIndices.Clear();
            _objects.Clear();
        }

        internal int GetIndexForObject(object obj)
        {
            ref int index = ref CollectionsMarshal.GetValueRefOrAddDefault(_objectIndices, obj, out bool exists);
            if (!exists)
            {
                index = _objects.Count;
                _objects.Add(obj);
            }

            return index;
        }

        internal T RetrieveObject<T>(int idx) => (T)_objects[idx];
    }
}
