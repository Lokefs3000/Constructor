using Primary.Assets.Types;
using System.Collections.Concurrent;

namespace Editor.Storage
{
    public sealed class AssetDatabase
    {
        private ConcurrentDictionary<Type, AssetCategoryDatabase> _categories;

        internal AssetDatabase()
        {
            _categories = new ConcurrentDictionary<Type, AssetCategoryDatabase>();
        }

        internal void HandlePendingUpdates()
        {
            foreach (var kvp in _categories)
            {
                kvp.Value.HandlePendingUpdates();
            }
        }

        internal void PurgeDatabase()
        {
            _categories.Clear();
        }

        /// <summary>Thread-safe</summary>
        public void AddEntry(Type type, AssetDatabaseEntry entry, bool createIfNull = true)
        {
            if (!_categories.TryGetValue(type, out AssetCategoryDatabase? category))
            {
                if (!createIfNull)
                    return;

                category = new AssetCategoryDatabase(type);
                category = _categories.GetOrAdd(type, category);
            }

            category.AddEntry(entry);
        }

        /// <summary>Thread-safe</summary>
        public void AddEntry<T>(AssetDatabaseEntry entry, bool createIfNull = true) where T : class => AddEntry(typeof(T), entry, createIfNull);

        /// <summary>Thread-safe</summary>
        public void RemoveEntry(Type type, AssetId id)
        {
            if (_categories.TryGetValue(type, out AssetCategoryDatabase? category))
            {
                category.RemoveEntry(id);
            }
        }

        /// <summary>Thread-safe</summary>
        public AssetCategoryDatabase? GetCategory<T>(bool createIfNull = true) where T : class
        {
            if (!_categories.TryGetValue(typeof(T), out AssetCategoryDatabase? category))
            {
                if (!createIfNull)
                    return null;

                category = new AssetCategoryDatabase(typeof(T));
                category = _categories.GetOrAdd(typeof(T), category);
            }

            return category;
        }

        /// <summary>Thread-safe</summary>
        public AssetCategoryDatabase? GetCategory(Type type, bool createIfNull = true)
        {
            if (!_categories.TryGetValue(type, out AssetCategoryDatabase? category))
            {
                if (!createIfNull)
                    return null;

                category = new AssetCategoryDatabase(type);
                category = _categories.GetOrAdd(type, category);
            }

            return category;
        }

        public IEnumerable<AssetCategoryDatabase> Categories => _categories.Values;
    }
}
