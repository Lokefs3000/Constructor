using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Search
{
    public abstract class SearchItemPool
    {
        protected internal abstract void Return(SearchItem item);
    }

    public sealed class SearchItemPool<T> : SearchItemPool where T : SearchItem, new()
    {
        private readonly int _maxCount;
        private readonly Stack<T> _itemPool;

        public SearchItemPool(int maxCount = 64)
        {
            _maxCount = maxCount;
            _itemPool = new Stack<T>(maxCount);
        }

        public T Get()
        {
            if (_itemPool.TryPop(out T? item))
                return item;
            return new T();
        }

        protected internal override void Return(SearchItem item)
        {
            if (_itemPool.Count < _maxCount)
                _itemPool.Push((T)item);
        }
    }
}
