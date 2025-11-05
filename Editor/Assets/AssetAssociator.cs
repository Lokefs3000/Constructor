using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Primary.Assets;
using Primary.Common;
using System.Collections.Concurrent;
using System.Text;

namespace Editor.Assets
{
    public sealed class AssetAssociator
    {
        private ConcurrentDictionary<AssetId, AssocationData> _assocations;

        internal AssetAssociator()
        {
            _assocations = new ConcurrentDictionary<AssetId, AssocationData>();
        }

        internal bool ReadAssocations()
        {
            if (!File.Exists(FilePath))
                return false;

            string? source = FileUtility.TryReadAllText(FilePath);
            if (source == null)
                return false;

            using PooledList<AssetId> ids = new PooledList<AssetId>();

            _assocations.Clear();

            foreach (ReadOnlySpan<char> line in source.Tokenize('\n'))
            {
                ReadOnlySpanTokenizer<char> tokenizer = line.Trim().Tokenize(';');
                if (tokenizer.MoveNext() && uint.TryParse(tokenizer.Current, out uint assetId))
                {
                    ids.Clear();
                    while (tokenizer.MoveNext())
                    {
                        if (uint.TryParse(tokenizer.Current, out uint dependencyId))
                        {
                            ids.Add((AssetId)dependencyId);
                        }
                    }

                    if (ids.Count > 0)
                    {
                        MakeAssocations((AssetId)assetId, ids.Span);
                    }
                }
            }

            return true;
        }

        /// <summary>Not thread-safe</summary>
        internal void FlushAssociations()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var kvp in _assocations)
            {
                if (kvp.Value.Dependencies.Count == 0)
                    continue;

                sb.Append(kvp.Key.Value);
                sb.Append(';');

                int lastIndex = kvp.Value.Dependencies.Count;
                foreach (AssetId item in kvp.Value.Dependencies)
                {
                    lastIndex--;

                    sb.Append(item.Value);
                    if (lastIndex > 0)
                        sb.Append(';');
                }

                sb.AppendLine();
            }

            File.WriteAllText(FilePath, sb.ToString());
        }

        /// <summary>Thread-safe</summary>
        internal void ClearAllAssocations()
        {
            _assocations.Clear();
        }

        /// <summary>Thread-safe</summary>
        private void AddAssetAsDependent(AssetId id, AssetId newDependent)
        {
            _assocations.AddOrUpdate(id, NewFactory, UpdateFactory);

            AssocationData NewFactory(AssetId _)
            {
                return new AssocationData(
                    new Lock(),
                    new HashSet<AssetId>(),
                    [newDependent]);
            }

            AssocationData UpdateFactory(AssetId _, AssocationData assocationData)
            {
                using (assocationData.Lock.EnterScope())
                {
                    assocationData.Dependents.Add(newDependent);
                }

                return assocationData;
            }
        }

        /// <summary>Thread-safe</summary>
        private void RemoveAssetFromDependents(AssetId id, AssetId oldDependent)
        {
            if (_assocations.TryGetValue(id, out AssocationData assocations))
            {
                using (assocations.Lock.EnterScope())
                {
                    assocations.Dependents.Remove(oldDependent);
                }
            }
        }

        /// <summary>Thread-safe</summary>
        public void MakeAssocations(AssetId id, ReadOnlySpan<AssetId> assets, bool clearPrevious = false)
        {
            if (assets.IsEmpty)
                return;

            _assocations.AddOrUpdate(id, NewFactory, UpdateFactory, assets);

            AssocationData NewFactory(AssetId _, ReadOnlySpan<AssetId> span)
            {
                AssocationData assocations = new AssocationData(new Lock(), new HashSet<AssetId>(), new HashSet<AssetId>());
                for (int i = 0; i < span.Length; i++)
                {
                    if (span[i] != id)
                    {
                        assocations.Dependencies.Add(span[i]);
                        AddAssetAsDependent(span[i], id);
                    }
                }

                return assocations;
            }

            AssocationData UpdateFactory(AssetId _, AssocationData assocations, ReadOnlySpan<AssetId> span)
            {
                using (assocations.Lock.EnterScope())
                {
                    if (clearPrevious)
                    {
                        while (true)
                        {
                            AssetId neededRemoval = AssetId.Invalid;
                            foreach (AssetId dependency in assocations.Dependencies)
                            {
                                if (!span.Contains(dependency))
                                {
                                    neededRemoval = dependency;
                                    RemoveAssetFromDependents(dependency, id);

                                    break;
                                }
                            }

                            if (neededRemoval.IsInvalid)
                                break;

                            assocations.Dependencies.Remove(neededRemoval);
                        }
                    }

                    for (int i = 0; i < span.Length; i++)
                    {
                        if (span[i] != id && assocations.Dependencies.Add(span[i]))
                        {
                            AddAssetAsDependent(span[i], id);
                        }
                    }
                }

                return assocations;
            }
        }

        /// <summary>Thread-safe</summary>
        public void MakeAssocation(AssetId id, AssetId asset, bool clearPrevious = false) => MakeAssocations(id, new ReadOnlySpan<AssetId>(in asset), clearPrevious);

        /// <summary>Thread-safe</summary>
        public void ClearAssocations(AssetId id)
        {
            if (_assocations.TryGetValue(id, out AssocationData assocations))
            {
                using (assocations.Lock.EnterScope())
                {
                    foreach (AssetId dependency in assocations.Dependencies)
                    {
                        RemoveAssetFromDependents(dependency, id);
                    }

                    if (assocations.Dependents.Count == 0)
                    {
                        _assocations.TryRemove(id, out _);
                    }
                }
            }
        }

        /// <summary>Thread-safe</summary>
        public void RemoveAssocations(AssetId id, ReadOnlySpan<AssetId> assets)
        {
            if (assets.IsEmpty)
                return;

            if (_assocations.TryGetValue(id, out AssocationData assocations))
            {
                using (assocations.Lock.EnterScope())
                {
                    while (true)
                    {
                        AssetId neededRemoval = AssetId.Invalid;
                        foreach (AssetId dependency in assocations.Dependencies)
                        {
                            if (assets.Contains(dependency))
                            {
                                neededRemoval = dependency;
                                RemoveAssetFromDependents(dependency, id);

                                break;
                            }
                        }

                        if (neededRemoval.IsInvalid)
                            break;

                        assocations.Dependencies.Remove(neededRemoval);
                    }
                }
            }
        }

        /// <summary>Thread-safe</summary>
        public void RemoveAssocation(AssetId id, AssetId asset) => RemoveAssocations(id, new ReadOnlySpan<AssetId>(in asset));

        /// <summary>Thread-safe</summary>
        internal Lock.Scope GetDependenciesWithLockScope(AssetId id, out HashSet<AssetId>? dependencies)
        {
            if (_assocations.TryGetValue(id, out AssocationData assocations))
            {
                dependencies = assocations.Dependencies;
                return assocations.Lock.EnterScope();
            }

            dependencies = null;
            return default;
        }

        /// <summary>Thread-safe</summary>
        internal Lock.Scope GetDependentsWithLockScope(AssetId id, out HashSet<AssetId>? dependents)
        {
            if (_assocations.TryGetValue(id, out AssocationData assocations))
            {
                dependents = assocations.Dependents;
                return assocations.Lock.EnterScope();
            }

            dependents = null;
            return default;
        }

        /// <summary>Thread-safe</summary>
        internal Lock.Scope GetDependenciesAndDependentsWithLockScope(AssetId id, out HashSet<AssetId>? dependencies, out HashSet<AssetId>? dependents)
        {
            if (_assocations.TryGetValue(id, out AssocationData assocations))
            {
                dependencies = assocations.Dependencies;
                dependents = assocations.Dependents;
                return assocations.Lock.EnterScope();
            }

            dependencies = null;
            dependents = null;
            return default;
        }

        public static string FilePath = Path.Combine(EditorFilepaths.LibraryIntermediatePath, "FileAssociations.dat");

        private readonly record struct AssocationData(Lock Lock, HashSet<AssetId> Dependencies, HashSet<AssetId> Dependents);
    }
}
