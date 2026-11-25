using Primary.Collections.ReadOnly;
using Primary.Pooling;

namespace PrimaryEditor.Search
{
    public sealed class SearchList
    {
        private readonly SearchManager _manager;
        private readonly List<SearchItem> _items;

        internal SearchList(SearchManager manager)
        {
            _manager = manager;
            _items = new List<SearchItem>();
        }

        public void Return()
        {
            foreach (SearchItem item in _items)
            {
                _manager.ReturnSearchItem(item);
            }

            _items.Clear();
            _manager.ReturnList(this);
        }

        public void AddItem(SearchItem item)
        {
            _items.Add(item);
        }

        public ROList<SearchItem> Items => _items;

        internal record class PoolPolicy(SearchManager Manager) : IObjectPoolPolicy<SearchList>
        {
            public SearchList Create() => new SearchList(Manager);
            public bool Return(ref SearchList obj) => true;
        }
    }
}
