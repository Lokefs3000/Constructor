using CommunityToolkit.HighPerformance;
using Primary.Assets;
using System.Collections.Concurrent;

namespace Editor.Storage
{
    public sealed class AssetCategoryDatabase
    {
        private readonly Type _category;

        private HashSet<AssetDatabaseEntry> _entries;
        private ConcurrentBag<DatabaseUpdate> _updates;

        internal AssetCategoryDatabase(Type category)
        {
            _category = category;

            _entries = new HashSet<AssetDatabaseEntry>();
            _updates = new ConcurrentBag<DatabaseUpdate>();
        }

        /// <summary>Not thread-safe</summary>
        internal void HandlePendingUpdates()
        {
            while (_updates.TryTake(out DatabaseUpdate update))
            {
                if (update.IsNew)
                {
                    if (!_entries.Add(update.Entry))
                        EdLog.Assets.Warning("Duplicate asset database entry: {entry}", update.Entry);
                }
                else
                {
                    _entries.Remove(update.Entry);
                }
            }
        }

        /// <summary>Thread-safe</summary>
        public void AddEntry(AssetDatabaseEntry entry)
        {
            _updates.Add(new DatabaseUpdate(entry, true));
        }

        /// <summary>Thread-safe</summary>
        public void RemoveEntry(AssetId id)
        {
            _updates.Add(new DatabaseUpdate(new AssetDatabaseEntry(id, string.Empty), false));
        }

        internal IEnumerable<AssetDatabaseEntry> Entries => _entries;

        private readonly record struct DatabaseUpdate(AssetDatabaseEntry Entry, bool IsNew);
    }

    public readonly record struct AssetDatabaseEntry(AssetId Id, string LocalPath)
    {
        public override string ToString() => $"{LocalPath} ({Id})";
        public override int GetHashCode() => HashCode.Combine(Id, LocalPath.GetDjb2HashCode()).GetHashCode();
    }
}
