using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using Primary.Collections.ReadOnly;

namespace PrimaryEditor.Assets.Database
{
    public sealed class AssetCategory
    {
        private readonly Type _assetType;

        private readonly HashSet<DatabaseEntry> _entries;

        private readonly Lock _lock;
        private readonly List<AssetCategoryUpdate> _updates;

        internal AssetCategory(Type type)
        {
            _assetType = type;

            _entries = new HashSet<DatabaseEntry>();

            _lock = new Lock();
            _updates = new List<AssetCategoryUpdate>();
        }

        internal void HandlePendingUpdates()
        {
            if (_updates.Count > 0)
            {
                using (_lock.EnterScope())
                {
                    foreach (AssetCategoryUpdate update in _updates)
                    {
                        switch (update.Type)
                        {
                            case AssetCategoryUpdateType.Add:
                                {
                                    _entries.Add(update.Id);
                                    OnEntryAdded?.Invoke(update.Id);
                                    break;
                                }
                            case AssetCategoryUpdateType.Remove:
                                {
                                    _entries.Remove(update.Id);
                                    OnEntryRemoved?.Invoke(update.Id);
                                    break;
                                }
                        }
                    }

                    _updates.Clear();
                }
            }
        }

        internal void AddEntry(DatabaseEntry id)
        {
            using (_lock.EnterScope())
            {
                _updates.Add(new AssetCategoryUpdate(id, AssetCategoryUpdateType.Add));
            }
        }

        internal void RemoveEntry(DatabaseEntry id)
        {
            using (_lock.EnterScope())
            {
                _updates.Remove(new AssetCategoryUpdate(id, AssetCategoryUpdateType.Remove));
            }
        }

        internal void ClearAllWithId(AssetId id)
        {
            using (_lock.EnterScope())
            {
                foreach (DatabaseEntry entry in _entries)
                {
                    if (entry.Id == id)
                    {
                        _updates.Add(new AssetCategoryUpdate(entry, AssetCategoryUpdateType.Remove));
                    }
                }
            }
        }

        public Type AssetType => _assetType;
        public ROHashSet<DatabaseEntry> Assets => _entries;

        public event Action<DatabaseEntry>? OnEntryAdded;
        public event Action<DatabaseEntry>? OnEntryRemoved;
    }

    public readonly record struct AssetCategoryUpdate(DatabaseEntry Id, AssetCategoryUpdateType Type);

    public enum AssetCategoryUpdateType : byte
    {
        Add,
        Remove
    }
}
