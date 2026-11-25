using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Helpers
{
    public class DataIdentifier<T> where T : notnull
    {
        private Dictionary<T, int> _indices;
        private List<T> _values;

        public DataIdentifier()
        {
            _indices = new Dictionary<T, int>();
            _values = new List<T>();
        }

        public void Clear()
        {
            _indices.Clear();
            _values.Clear();
        }

        public int AddOrGet(T item)
        {
            if (_indices.TryGetValue(item, out int idx))
                return idx;

            idx = _values.Count;

            _indices.Add(item, idx);
            _values.Add(item);

            return idx;
        }

        public int Find(T item)
        {
            if (_indices.TryGetValue(item, out int idx))
                return idx;
            return int.MinValue;
        }

        public T Get(int index)
        {
            return _values[index];
        }
    }
}
