using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Primary.Serialization.Structural
{
    public sealed class SDFObject : SDFBase, IDictionary<string, SDFBase>
    {
        private string? _name;
        private Dictionary<string, SDFBase> _objects;

        public SDFObject()
        {
            _name = null;
            _objects = new Dictionary<string, SDFBase>();
        }

        public string? Name { get => _name; set => _name = value; }

        #region Auto-generated
        public SDFBase this[string key] { get => ((IDictionary<string, SDFBase>)_objects)[key]; set => ((IDictionary<string, SDFBase>)_objects)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, SDFBase>)_objects).Keys;

        public ICollection<SDFBase> Values => ((IDictionary<string, SDFBase>)_objects).Values;

        public int Count => ((ICollection<KeyValuePair<string, SDFBase>>)_objects).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, SDFBase>>)_objects).IsReadOnly;

        public void Add(string key, SDFBase value)
        {
            ((IDictionary<string, SDFBase>)_objects).Add(key, value);
        }

        public void Add(KeyValuePair<string, SDFBase> item)
        {
            ((ICollection<KeyValuePair<string, SDFBase>>)_objects).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, SDFBase>>)_objects).Clear();
        }

        public bool Contains(KeyValuePair<string, SDFBase> item)
        {
            return ((ICollection<KeyValuePair<string, SDFBase>>)_objects).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, SDFBase>)_objects).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, SDFBase>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, SDFBase>>)_objects).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, SDFBase>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, SDFBase>>)_objects).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, SDFBase>)_objects).Remove(key);
        }

        public bool Remove(KeyValuePair<string, SDFBase> item)
        {
            return ((ICollection<KeyValuePair<string, SDFBase>>)_objects).Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out SDFBase value)
        {
            return ((IDictionary<string, SDFBase>)_objects).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_objects).GetEnumerator();
        }
        #endregion
    }
}
