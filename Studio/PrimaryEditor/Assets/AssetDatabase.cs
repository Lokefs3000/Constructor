using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Profiling;
using Primary.Scenes;
using PrimaryEditor.Assets.Database;

namespace PrimaryEditor.Assets
{
    public sealed class AssetDatabase
    {
        private readonly FrozenDictionary<Type, AssetCategory> _categories;
        private bool _hasDatabaseChanges;

        internal AssetDatabase()
        {
            _categories = new Dictionary<Type, AssetCategory>
            {
                { typeof(Scene), new AssetCategory(typeof(Scene))},
                { typeof(MaterialAsset), new AssetCategory(typeof(MaterialAsset))},
                { typeof(ModelAsset), new AssetCategory(typeof(ModelAsset))},
                { typeof(MeshAsset), new AssetCategory(typeof(MeshAsset))},
                { typeof(ShaderAsset), new AssetCategory(typeof(ShaderAsset))},
                { typeof(ComputeShaderAsset), new AssetCategory(typeof(ComputeShaderAsset))},
                { typeof(TextureAsset), new AssetCategory(typeof(TextureAsset))},
                { typeof(TextureAtlasAsset), new AssetCategory(typeof(TextureAtlasAsset))},
                { typeof(Sprite), new AssetCategory(typeof(Sprite))},
            }.ToFrozenDictionary();

            foreach (var (type, category) in _categories)
            {
                category.OnEntryAdded += (id) => OnEntryAdded?.Invoke(type, id);
                category.OnEntryRemoved += (id) => OnEntryRemoved?.Invoke(type, id);
            }
        }

        internal void FlushPendingUpdates()
        {
            if (_hasDatabaseChanges)
            {
                _hasDatabaseChanges = false;

                using (new ProfilingScope("UpdateAssetDb"))
                {
                    foreach (var (_, category) in _categories)
                    {
                        category.HandlePendingUpdates();
                    }
                }
            }
        }

        public void AddEntry(Type type, DatabaseEntry id)
        {
            if (_categories.TryGetValue(type, out AssetCategory? category))
            {
                category.AddEntry(id);
                _hasDatabaseChanges = true;
            }
        }

        public void RemoveEntry(Type type, DatabaseEntry id)
        {
            if (_categories.TryGetValue(type, out AssetCategory? category))
            {
                category.RemoveEntry(id);
                _hasDatabaseChanges = true;
            }
        }

        public void ClearAllWithId(Type type, AssetId id)
        {
            if (_categories.TryGetValue(type, out AssetCategory? category))
            {
                category.ClearAllWithId(id);
                _hasDatabaseChanges = true;
            }
        }

        public FrozenDictionary<Type, AssetCategory> Categories => _categories;

        public event Action<Type, DatabaseEntry>? OnEntryAdded;
        public event Action<Type, DatabaseEntry>? OnEntryRemoved;
    }

    public readonly record struct DatabaseEntry(AssetId Id, string? Key = null) : IEquatable<DatabaseEntry>
    {
        public override int GetHashCode() => HashCode.Combine(Id, Key);

        public static implicit operator DatabaseEntry(FileId id) => new DatabaseEntry(id);
        public static implicit operator DatabaseEntry(AssetId id) => new DatabaseEntry(id);
    }
}
