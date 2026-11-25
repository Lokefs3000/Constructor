using System;
using System.Collections.Generic;
using System.Text;
using Primary.Pooling;
using PrimaryEditor.Search.Providers;

namespace PrimaryEditor.Search
{
    public sealed class SearchManager
    {
        private readonly ObjectPool<SearchList> _searchListPool;
        private readonly ObjectPool<SearchQuery> _searchQueryPool;

        private readonly Dictionary<Type, SearchItemPool> _itemPools;

        private readonly List<ISearchProvider> _providers;

        public SearchManager()
        {
            _searchListPool = new ObjectPool<SearchList>(new SearchList.PoolPolicy(this), 16);
            _searchQueryPool = new ObjectPool<SearchQuery>(new SearchQuery.PoolPolicy(), 16);

            _itemPools = new Dictionary<Type, SearchItemPool>();

            _providers = [
                new AssetSearchProvider(this)
                ];
        }

        public SearchList Search(SearchQuery query)
        {
            SearchList list = _searchListPool.Get();

                bool hasAnyProvidersInParams = false;
            if (query.Parameters.Count > 0)
            {
                foreach (ISearchProvider provider in _providers)
                {
                    if (query.Parameters.Contains(provider.GetType()))
                    {
                        hasAnyProvidersInParams = true;
                        break;
                    }
                }
            }

            foreach (ISearchProvider provider in _providers)
            {
                if (!hasAnyProvidersInParams || query.Parameters.Contains(provider.GetType()))
                {
                    provider.Query(query, list);
                }
            }

            return list;
        }

        public SearchList Search(params object[] parameters)
        {
            SearchQuery query = GetPooledSearchQuery();
            foreach (object param in parameters)
                query.Parameters.Add(param);

            SearchList list = Search(query);
            ReturnPooledSearchQuery(query);

            return list;
        }

        public SearchList Search(string text, params object[] parameters)
        {
            SearchQuery query = GetPooledSearchQuery();
            query.Text = text;

            foreach (object param in parameters)
                query.Parameters.Add(param);

            SearchList list = Search(query);
            ReturnPooledSearchQuery(query);

            return list;
        }

        public void RegisterItemPool<T>(SearchItemPool<T> pool) where T : SearchItem, new()
        {
            _itemPools[typeof(T)] = pool;
        }

        public SearchQuery GetPooledSearchQuery() => _searchQueryPool.Get();
        public void ReturnPooledSearchQuery(SearchQuery query) => _searchQueryPool.Return(query);

        internal void ReturnSearchItem(SearchItem item)
        {
            item.ClearForPoolReturn();
            if (_itemPools.TryGetValue(item.GetType(), out SearchItemPool? pool))
            {
                pool.Return(item);
            }
        }

        internal void ReturnList(SearchList list)
        {
            _searchListPool.Return(list);
        }
    }
}
