using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Assets.Exceptions;

namespace PrimaryEditor.Assets
{
    public readonly record struct ImportContext(AssetPipeline Pipeline, FileId Id, string LocalPath, string LocalOutputPath, bool IsTrialImport, Stream InputStream, AssetDataConfig Config, Stream OutputStream)
    {
        private readonly List<(Type Type, int LocalId, string? Name)> _subAssets = new List<(Type Type, int LocalId, string? Name)>();
        private readonly HashSet<AssetId> _dependencies = new HashSet<AssetId>();
        private readonly HashSet<AssetId> _reloadConnections = new HashSet<AssetId>();

        public void AddSubAsset<T>(int localId, string? name = null)
        {
            if (localId == AssetId.NoLocalId)
                return;

            for (int i = 0; i < _subAssets.Count; ++i)
            {
                if (_subAssets[i].LocalId == localId)
                {
                    _subAssets[i] = (typeof(T), localId, name);
                    return;
                }
            }

            _subAssets.Add((typeof(T), localId, name));
            AddReloadConnection(new AssetId(Id, localId));
        }

        public void AddDependency(AssetId id)
        {
            _dependencies.Add(id);
        }

        public void AddDependencies(ReadOnlySpan<AssetId> ids)
        {
            foreach (AssetId id in ids)
            {
                _dependencies.Add(id);
            }
        }

        public void AddDependencies(ReadOnlySpan<FileId> ids)
        {
            foreach (AssetId id in ids)
            {
                _dependencies.Add(id);
            }
        }

        public void AddReloadConnection(AssetId id)
        {
            _reloadConnections.Add(id);
        }

        public void AddReloadConnections(ReadOnlySpan<AssetId> ids)
        {
            foreach (AssetId id in ids)
            {
                _reloadConnections.Add(id);
            }
        }

        public void AddReloadConnections(ReadOnlySpan<FileId> ids)
        {
            foreach (AssetId id in ids)
            {
                _reloadConnections.Add(id);
            }
        }

        public T GetAssetConfiguration<T>() where T : class, new()
        {
            return (T?)Config.UniqueConfig ?? throw new AssetImportException("Expected unique configuration for asset");
        }

        internal void SortSubAssets()
        {
            _subAssets.Sort(static (x, y) => x.LocalId.CompareTo(y.LocalId));
        }

        internal ROList<(Type Type, int LocalId, string? Name)> SubAssets => _subAssets;
        internal ROHashSet<AssetId> Dependencies => _dependencies;
        internal ROHashSet<AssetId> ReloadConnections => _reloadConnections;
    }
}
