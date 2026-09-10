using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Threading;
using PrimaryEditor.Assets;
using PrimaryEditor.Assets.Importers;
using PrimaryEditor.Search.Database;
using PrimaryEditor.Search.Items;

namespace PrimaryEditor.Search.Providers
{
    public sealed class AssetSearchProvider : ISearchProvider
    {
        private readonly SearchItemPool<AssetSearchItem> _pool;
        private readonly Dictionary<Type, StringMatcher> _stringMatchers;

        public AssetSearchProvider(SearchManager manager)
        {
            _pool = new SearchItemPool<AssetSearchItem>();
            _stringMatchers = new Dictionary<Type, StringMatcher>();

            manager.RegisterItemPool(_pool);

            AssetPipeline? pipeline = AssetPipeline.Instance;
            if (pipeline != null)
            {
                pipeline.Database.OnEntryAdded += OnDatabaseEntryAdded;
                pipeline.Database.OnEntryRemoved += OnDatabaseEntryRemoved;

                foreach (var (type, category) in pipeline.Database.Categories)
                {
                    foreach (DatabaseEntry entry in category.Assets)
                    {
                        OnDatabaseEntryAdded(type, entry);
                    }
                }
            }
        }

        private void OnDatabaseEntryAdded(Type type, DatabaseEntry entry)
        {
            AssetPipeline? pipeline = AssetPipeline.Instance;
            if (entry.Key != null)
            {
                ref StringMatcher? matcher = ref CollectionsMarshal.GetValueRefOrAddDefault(_stringMatchers, type, out bool exists);
                if (!exists || matcher == null)
                    matcher = new StringMatcher();

                matcher.AddString(new StringMatchKey(entry.Key, entry.Id));
            }
            else
            {
                if (pipeline != null &&
                pipeline.AssetRegistry.TryGetLocalPathForId(entry.Id, out string? localPath) &&
                pipeline.ImporterRegistry.TryGetImporterForPath(localPath, out AssetImporterData importerData))
                {
                    ref StringMatcher? matcher = ref CollectionsMarshal.GetValueRefOrAddDefault(_stringMatchers, importerData.Importer.AssetDefinitionType, out bool exists);
                    if (!exists || matcher == null)
                        matcher = new StringMatcher();

                    matcher.AddString(new StringMatchKey(localPath, entry.Id));
                }
            }
        }

        private void OnDatabaseEntryRemoved(Type type, DatabaseEntry entry)
        {
            AssetPipeline? pipeline = AssetPipeline.Instance;
            if (entry.Key != null)
            {
                ref StringMatcher? matcher = ref CollectionsMarshal.GetValueRefOrAddDefault(_stringMatchers, type, out bool exists);
                if (!exists || matcher == null)
                    matcher = new StringMatcher();

                matcher.RemoveString(new StringMatchKey(entry.Key, entry.Id));
            }
            else
            {
                if (pipeline != null &&
                    pipeline.AssetRegistry.TryGetLocalPathForId(entry.Id, out string? localPath) &&
                    pipeline.ImporterRegistry.TryGetImporterForPath(localPath, out AssetImporterData importerData))
                {
                    ref StringMatcher? matcher = ref CollectionsMarshal.GetValueRefOrAddDefault(_stringMatchers, importerData.Importer.AssetDefinitionType, out bool exists);
                    if (!exists || matcher == null)
                        matcher = new StringMatcher();

                    matcher.RemoveString(new StringMatchKey(localPath, entry.Id));
                }
            }
        }

        public void Query(SearchQuery query, SearchList list)
        {
            bool isSearchLimited = query.Parameters.Overlaps(_stringMatchers.Keys);

            string? queryTerm = query.Text;
            if (string.IsNullOrEmpty(queryTerm))
            {
                foreach (var (type, matcher) in _stringMatchers)
                {
                    if (!isSearchLimited || query.Parameters.Contains(type))
                    {
                        RentedList<StringMatchKey> allStrings = new RentedList<StringMatchKey>();
                        matcher.QueryForAllStrings(ref allStrings);

                        ConvertMatchesToItems(type, allStrings.AsSpan(), list);
                        allStrings.Dispose();
                    }
                }

                return;
            }

            foreach (var (type, matcher) in _stringMatchers)
            {
                if (!isSearchLimited || query.Parameters.Contains(type))
                {
                    RentedList<StringMatchKey> possibleMatches = new RentedList<StringMatchKey>();
                    matcher.SearchForString(queryTerm, ref possibleMatches);

                    ConvertMatchesToItems(type, possibleMatches.AsSpan(), list);
                    possibleMatches.Dispose();
                }
            }
        }

        private void ConvertMatchesToItems(Type type, ReadOnlySpan<StringMatchKey> items, SearchList list)
        {
            foreach (StringMatchKey item in items)
            {
                AssetSearchItem searchItem = _pool.Get();
                searchItem.SetupItem(type, item.Value, item.Id);

                list.AddItem(searchItem);
            }
        }
    }
}
