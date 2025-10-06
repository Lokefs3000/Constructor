using System.Collections;

namespace Primary.Serialization.Structural
{
    public sealed class SDFArray : SDFBase, IList<SDFBase>
    {
        private List<SDFBase> _objects;

        public SDFArray()
        {
            _objects = new List<SDFBase>();
        }

        #region Auto-generated
        public SDFBase this[int index] { get => ((IList<SDFBase>)_objects)[index]; set => ((IList<SDFBase>)_objects)[index] = value; }

        public int Count => ((ICollection<SDFBase>)_objects).Count;

        public bool IsReadOnly => ((ICollection<SDFBase>)_objects).IsReadOnly;

        public void Add(SDFBase item)
        {
            ((ICollection<SDFBase>)_objects).Add(item);
        }

        public void Clear()
        {
            ((ICollection<SDFBase>)_objects).Clear();
        }

        public bool Contains(SDFBase item)
        {
            return ((ICollection<SDFBase>)_objects).Contains(item);
        }

        public void CopyTo(SDFBase[] array, int arrayIndex)
        {
            ((ICollection<SDFBase>)_objects).CopyTo(array, arrayIndex);
        }

        public IEnumerator<SDFBase> GetEnumerator()
        {
            return ((IEnumerable<SDFBase>)_objects).GetEnumerator();
        }

        public int IndexOf(SDFBase item)
        {
            return ((IList<SDFBase>)_objects).IndexOf(item);
        }

        public void Insert(int index, SDFBase item)
        {
            ((IList<SDFBase>)_objects).Insert(index, item);
        }

        public bool Remove(SDFBase item)
        {
            return ((ICollection<SDFBase>)_objects).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<SDFBase>)_objects).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_objects).GetEnumerator();
        }
        #endregion
    }
}
