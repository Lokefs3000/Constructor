using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

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
                switch (update.Type)
                {
                    case DatabaseUpdateType.Add:
                        {
                            if (!_entries.Add(update.Entry))
                                EdLog.Assets.Warning("Duplicate asset database entry: {entry}", update.Entry);
                            break;
                        }
                    case DatabaseUpdateType.AddOrUpdate:
                        {
                            _entries.Remove(update.Entry);
                            _entries.Add(update.Entry);
                            break;
                        }
                    case DatabaseUpdateType.Remove:
                        {
                            _entries.Remove(update.Entry);
                            break;
                        }
                    case DatabaseUpdateType.Update:
                        {
                            if (_entries.Remove(update.Entry))
                                _entries.Add(update.Entry);
                            break;
                        }
                }
            }
        }

        /// <summary>Thread-safe</summary>
        public void AddEntry(AssetDatabaseEntry entry, bool overrideIfExists = true)
        {
            _updates.Add(new DatabaseUpdate(entry, overrideIfExists ? DatabaseUpdateType.AddOrUpdate : DatabaseUpdateType.Add));
        }

        /// <summary>Thread-safe</summary>
        public void SetEntryImportStatus(AssetId id, bool isImported)
        {
            _updates.Add(new DatabaseUpdate(new AssetDatabaseEntry(id, string.Empty, isImported), DatabaseUpdateType.Update));
        }

        /// <summary>Thread-safe</summary>
        public void RemoveEntry(AssetId id)
        {
            _updates.Add(new DatabaseUpdate(new AssetDatabaseEntry(id, string.Empty, false), DatabaseUpdateType.Remove));
        }

        public Type AssetType => _category;
        internal IEnumerable<AssetDatabaseEntry> Entries => _entries;

        private readonly record struct DatabaseUpdate(AssetDatabaseEntry Entry, DatabaseUpdateType Type);

        private enum DatabaseUpdateType : byte
        {
            Add,
            AddOrUpdate,
            Remove,
            Update
        }
    }

    public readonly struct AssetDatabaseEntry(AssetId Id, string LocalPath, bool IsImported) : IEquatable<AssetDatabaseEntry>
    {
        public AssetId Id { get; init; } = Id;
        public string LocalPath { get; init; } = LocalPath;
        public bool IsImported { get; init; } = IsImported;

        public override string ToString() => $"{LocalPath} ({Id})";
        public override int GetHashCode() => HashCode.Combine(Id, LocalPath.GetDjb2HashCode()).GetHashCode();
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is AssetDatabaseEntry entry && Equals(entry);
        public bool Equals(AssetDatabaseEntry other) => Id == other.Id;
    }
}
