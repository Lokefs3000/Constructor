using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Editor.Assets.Importers;
using Editor.Platform.Windows;
using Primary.Assets;
using Primary.Profiling;
using Primary.Utility.Scopes;
using SDL;
using Serilog;
using SharpGen.Runtime;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Editor.Assets
{
    public sealed class AssetPipeline : IDisposable
    {
        private SemaphoreSlim _importSemaphore;

        private AssetIdentifier _identifier;
        private AssetConfiguration _configuration;

        private AssetFilesystemWatcher _contentWatcher;
        private AssetFilesystemWatcher _sourceWatcher;

        private List<AssetImporterData> _importerList;

        private ConcurrentDictionary<string, FileAssociationData> _fileAssocations;
        private HashSet<string> _assetsToReload;

        private HashSet<string> _pendingImports;
        private Dictionary<string, Task> _runningImports;

        private int _importerTasksTotal;
        private int _importerTasksCount;

        private int _activeAssociationAccesses;
        private bool _canAccessAssocations;

        private bool _disposedValue;

        internal AssetPipeline()
        {
            bool needsDbRefresh = false;
            if (!Directory.Exists(EditorFilepaths.LibraryImportedPath))
            {
                Directory.CreateDirectory(EditorFilepaths.LibraryImportedPath);
                needsDbRefresh = true;
            }

            if (!Directory.Exists(EditorFilepaths.LibraryIntermediatePath))
            {
                Directory.CreateDirectory(EditorFilepaths.LibraryIntermediatePath);
                needsDbRefresh = true;
            }

            _importSemaphore = new SemaphoreSlim(1);

            _identifier = new AssetIdentifier();
            _configuration = new AssetConfiguration(this);

            _contentWatcher = new AssetFilesystemWatcher(EditorFilepaths.ContentPath);
            _sourceWatcher = new AssetFilesystemWatcher(EditorFilepaths.SourcePath);

            _importerList = new List<AssetImporterData>();

            _fileAssocations = new ConcurrentDictionary<string, FileAssociationData>();
            _assetsToReload = new HashSet<string>();

            _pendingImports = new HashSet<string>();
            _runningImports = new Dictionary<string, Task>();

            _importerTasksTotal = 0;

            _activeAssociationAccesses = 0;
            _canAccessAssocations = true;

            string associationFile = Path.Combine(EditorFilepaths.LibraryIntermediatePath, "FileAssociations.dat");
            if (File.Exists(associationFile))
            {
                ReadAssociationsFile(associationFile);
            }
            else
            {
                needsDbRefresh = true;
            }

            if (!File.Exists(Path.Combine(EditorFilepaths.LibraryIntermediatePath, "ShaderMappings.dat")))
                needsDbRefresh = true;
            if (!File.Exists(Path.Combine(EditorFilepaths.LibraryIntermediatePath, "FileRemappings.dat")))
                needsDbRefresh = true;
            if (!File.Exists(AssetIdentifier.DataFilePath))
                needsDbRefresh = true;

            AddImporter<ModelAssetImporter>(".fbx", ".obj");
            AddImporter<ShaderAssetImporter>(".hlsl");
            AddImporter<TextureAssetImporter>(".png", ".jpg", ".jpeg");
            AddImporter<MaterialAssetImporter>(".mat");

            if (needsDbRefresh)
                RefreshDatabase();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _contentWatcher.Dispose();
                    _sourceWatcher.Dispose();

                    _importSemaphore.Dispose();

                    File.WriteAllText(AssetIdentifier.DataFilePath, _identifier.TrySerializeAssetIds());

                    Editor.GlobalSingleton.ProjectShaderLibrary.FlushFileMappings();
                    Editor.GlobalSingleton.ProjectSubFilesystem.FlushFileRemappings();

                    FlushAssociationsFile();
                }

                for (int i = 0; i < _importerList.Count; i++)
                {
                    _importerList[i].Importer.Dispose();
                }

                _importerList.Clear();

                _disposedValue = true;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Not thread-safe</summary>
        private void ReadAssociationsFile(string filePath)
        {
            StringBuilder sb = new StringBuilder();
            string source = File.ReadAllText(filePath);

            string[] temporaryArray = ArrayPool<string>.Shared.Rent(32);

            try
            {
                int index = 0;
                int j = 0;

                bool wasFirstABust = false;

                while (index < source.Length)
                {
                    wasFirstABust = false;

                    if (source[index] == ';' || char.IsControl(source[index]))
                    {
                        if (sb.Length > 0)
                        {
                            string localPath = sb.ToString();
                            string fullPath = Path.Combine(Editor.GlobalSingleton.ProjectPath, localPath);

                            if (File.Exists(fullPath))
                            {
                                temporaryArray[j++] = sb.ToString();
                            }
                            else if (j == 0)
                            {
                                wasFirstABust = true;
                            }
                        }
                        sb.Clear();
                    }

                    if (char.IsControl(source[index]))
                    {
                        if (j > 1)
                        {
                            MakeFileAssociations(temporaryArray[0], temporaryArray.AsMemory(1, j - 1));
                        }

                        j = 0;
                        wasFirstABust = false;

                        sb.Clear();
                    }
                    else if (source[index] != ';')
                    {
                        sb.Append(source[index]);
                    }

                    if (wasFirstABust)
                    {
                        index = source.IndexOf('\n', index + 1);
                        if (index == -1)
                            break;
                    }

                    index++;
                }
            }
            finally
            {
                ArrayPool<string>.Shared.Return(temporaryArray);
            }
        }

        /// <summary>Not thread-safe</summary>
        private void FlushAssociationsFile()
        {
            string associationFile = Path.Combine(EditorFilepaths.LibraryIntermediatePath, "FileAssociations.dat");

            StringBuilder sb = new StringBuilder();
            foreach (var kvp in _fileAssocations)
            {
                if (kvp.Value.AssociatedWith.Count == 0)
                {
                    //resolvable when loading in the file
                    continue;
                }

                sb.Append(kvp.Key);
                sb.Append(';');

                int i = 0;
                foreach (string associate in kvp.Value.AssociatedWith)
                {
                    sb.Append(associate);

                    if (i == kvp.Value.AssociatedWith.Count - 1)
                        sb.AppendLine();
                    else
                        sb.Append(';');

                    i++;
                }
            }

            File.WriteAllText(associationFile, sb.ToString());
        }

        /// <summary>Not thread-safe</summary>
        internal void AddImporter<T>(params string[] associations) where T : IAssetImporter, new()
        {
            //TODO: add verification

            _importerList.Add(new AssetImporterData([.. associations], new T()));
        }

        /// <summary>Thread-safe</summary>
        public void MakeFileAssociations(string path, params string[] assocations)
         => MakeFileAssociations(path, assocations.AsMemory());

        //TODO: prevent associating a file with itself
        /// <summary>Thread-safe</summary>
        public void MakeFileAssociations(string path, ReadOnlyMemory<string> assocations)
        {
            if (!EnsureLocalPath(path, out string? localRootPath))
            {
                //bad
                return;
            }

            using (new SemaphoreScope(_importSemaphore))
            {
                ReadOnlySpan<string> span = assocations.Span;
                for (int i = 0; i < assocations.Length; i++)
                {
                    if (!EnsureLocalPath(span[i], out string? localPath))
                    {
                        //bad
                        continue;
                    }

                    _fileAssocations.AddOrUpdate(localPath, (x) =>
                    {
                        FileAssociationData data = new FileAssociationData(new HashSet<string>(), new HashSet<string>());
                        data.ExternalAssociates.Add(localRootPath);

                        return data;
                    }, (x, y) =>
                    {
                        lock (y.ExternalAssociates)
                        {
                            y.ExternalAssociates.Add(localRootPath);
                        }

                        return y;
                    });
                }

                _fileAssocations.AddOrUpdate(localRootPath, (x) =>
                {
                    FileAssociationData data = new FileAssociationData(new HashSet<string>(), new HashSet<string>());

                    ReadOnlySpan<string> span = assocations.Span;
                    for (int i = 0; i < assocations.Length; i++)
                    {
                        if (EnsureLocalPath(span[i], out string? localPath))
                            data.AssociatedWith.Add(localPath);
                    }

                    return data;

                }, (x, y) =>
                {
                    ReadOnlySpan<string> span = assocations.Span;
                    for (int i = 0; i < assocations.Length; i++)
                    {
                        if (EnsureLocalPath(span[i], out string? localPath))
                            lock (y.AssociatedWith)
                            {
                                y.AssociatedWith.Add(localPath);
                            }
                    }

                    return y;
                });
            }
        }

        /// <summary>Thread-safe</summary>
        public void ReloadAsset(string assetPath)
        {
            if (EnsureLocalPath(assetPath, out string? localPath))
                assetPath = localPath;

            lock (_assetsToReload)
            {
                _assetsToReload.Add(assetPath);
            }
        }

        /// <summary>Thread-safe</summary>
        internal bool CanBeImported(ReadOnlySpan<char> path)
        {
            ReadOnlySpan<char> extensionSpan = Path.GetExtension(path);
            if (extensionSpan.IsEmpty)
                return false;

            string extension = extensionSpan.ToString();

            Span<AssetImporterData> importers = _importerList.AsSpan();
            for (int i = 0; i < importers.Length; i++)
            {
                ref AssetImporterData importerData = ref importers[i];
                if (importerData.AssociatedExtensions.Contains(extension))
                {
                    return true;
                }
            }

            return false;
        }

        //TODO: implement an actual check as this can return false if something updates before PollFilesystem is called
        /// <summary>Thread-safe</summary>
        internal bool IsAssetUpToDate(ReadOnlySpan<char> path)
        {
            string str = path.ToString();

            using (new SemaphoreScope(_importSemaphore))
            {
                if (_runningImports.ContainsKey(str))
                    return false;
                if (_pendingImports.Contains(str))
                    return false;
            }

            return true;
        }

        /// <summary>Thread-safe</summary>
        internal bool IsImportingAsset(ReadOnlySpan<char> path)
        {
            using (new SemaphoreScope(_importSemaphore))
            {
                return _runningImports.ContainsKey(path.ToString());
            }
        }

        /// <summary>Thread-safe</summary>
        internal Task? ImportChangesOrGetRunning(ReadOnlySpan<char> path)
        {
            return ImportNewFile(path.ToString());
        }

        /// <summary>Not thread-safe</summary>
        private void RefreshDatabase()
        {
            try
            {
                _contentWatcher.ResetInternalState();
                _sourceWatcher.ResetInternalState();

                _contentWatcher.ClearEventQueue();
                _sourceWatcher.ClearEventQueue();

                _fileAssocations.Clear();
                _assetsToReload.Clear();

                Directory.Delete(EditorFilepaths.LibraryImportedPath, true);
                Directory.CreateDirectory(EditorFilepaths.LibraryImportedPath);

                foreach (string file in Directory.EnumerateFiles(EditorFilepaths.ContentPath, "*.*", SearchOption.AllDirectories))
                {
                    string localPath = file.Substring(Editor.GlobalSingleton.ProjectPath.Length).Replace('\\', '/');
                    //AssetId id = _identifier.GetOrRegisterAsset(localPath);

                    ImportNewFile(localPath);
                }

                File.WriteAllText(AssetIdentifier.DataFilePath, _identifier.TrySerializeAssetIds());

                using ImporterDisplay display = new ImporterDisplay();

                while (true)
                {
                    display.Poll();

                    //PollContentUpdates();

                    int total = _importerTasksTotal;
                    int completed = total - _importerTasksCount;

                    display.Progress = completed / (float)total;
                    display.Completed = completed;
                    display.Total = total;

                    display.Draw();

                    if (completed >= total)
                        break;
                }

                Editor.GlobalSingleton.ProjectShaderLibrary.FlushFileMappings();
                Editor.GlobalSingleton.ProjectSubFilesystem.FlushFileRemappings();

                FlushAssociationsFile();
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Failed to refresh asset pipeline database");
                throw;
            }
        }

        //TODO: avoid importing files that are not changed
        /// <summary>Thread-safe</summary>
        private Task? ImportNewFile(string localPath)
        {
            using (new SemaphoreScope(_importSemaphore))
            {
                if (_runningImports.TryGetValue(localPath, out Task? task))
                {
                    return task;
                }

                string extension = Path.GetExtension(localPath);

                Span<AssetImporterData> importers = _importerList.AsSpan();

                for (int i = 0; i < importers.Length; i++)
                {
                    AssetImporterData importer = importers[i];
                    if (importer.AssociatedExtensions.Contains(extension))
                    {
                        //Log.Information("importing: {x}", localPath);

                        string realPath = Path.Combine(Editor.GlobalSingleton.ProjectPath, localPath);
                        string assetPath = GetAssetPath(localPath);

                        _importerTasksTotal++;
                        if (!_runningImports.ContainsKey(localPath))
                        {
                            _pendingImports.Remove(localPath);

                            task = Task.Run(() =>
                            {
                                try
                                {
                                    importer.Importer.Import(this, realPath, assetPath);
                                }
                                catch (Exception ex)
                                {
                                    EdLog.Assets.Error(ex, "Exception occured trying to import asset: {x}", localPath);
#if DEBUG
                                    throw;
#endif
                                }
                                finally
                                {
                                    Interlocked.Decrement(ref _importerTasksCount);
                                    using (new SemaphoreScope(_importSemaphore))
                                    {
                                        _runningImports.Remove(localPath);
                                        //if (_pendingImports.Contains(localPath))
                                        //{
                                        //    ImportNewFile(localPath);
                                        //}
                                    }
                                }
                            });

                            Interlocked.Increment(ref _importerTasksCount);

                            _runningImports.Add(localPath, task);
                            return task;
                        }
                        else
                        {
                            _pendingImports.Add(localPath);
                            return _runningImports[localPath];
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>Thread-safe</summary>
        private void CleanOldFile(string localPath)
        {
            string assetPath = GetAssetPath(localPath);

        }

        private HashSet<string> _assocationRingAvoidance = new HashSet<string>();
        /// <summary>Not thread-safe</summary>
        private void ImportAssociatedFiles(string localPath)
        {
            _assocationRingAvoidance.Clear();
            Logic(localPath);

            void Logic(string path)
            {
                if (!_assocationRingAvoidance.Add(path))
                {
                    return;
                }

                if (_fileAssocations.TryGetValue(path, out FileAssociationData associationData))
                {
                    lock (associationData.ExternalAssociates)
                    {
                        foreach (string association in associationData.ExternalAssociates)
                        {
                            ImportNewFile(association);
                            Logic(association);
                        }
                    }
                }
            }
        }

        /// <summary>Not thread-safe</summary>
        internal void PollRemainingEvents()
        {
            using (new ProfilingScope("PollFilesystem"))
            {
                PollContentUpdates();

                if (_assetsToReload.Count > 0)
                {
                    ReloadPendingAssets();
                }
            }
        }

        /// <summary>Not thread-safe</summary>
        private void PollContentUpdates()
        {
            if (_importSemaphore.Wait(0))
            {
                try
                {
                    while (_contentWatcher.PollEvent(out FilesystemEvent @event))
                    {
                        switch (@event.Type)
                        {
                            case FilesystemEventType.FileAdded:
                            case FilesystemEventType.FileChanged:
                                {
                                    ImportNewFile(@event.LocalPath);
                                    ImportAssociatedFiles(@event.LocalPath);
                                    break;
                                }
                            case FilesystemEventType.FileRemoved: CleanOldFile(@event.LocalPath); break;
                        }
                    }
                }
                finally
                {
                    _importSemaphore.Release();
                }

                if (_importerTasksTotal > 0)
                {
                    if (_importSemaphore.Wait(0))
                    {
                        int total = _runningImports.Count + _pendingImports.Count;
                        if (total == 0)
                        {
                            _importerTasksTotal = 0;
                        }

                        _importSemaphore.Release();
                    }
                }
            }
        }

        /// <summary>Thread-safe</summary>
        private void ReloadPendingAssets()
        {
            lock (_assetsToReload)
            {
                AssetManager manager = Editor.GlobalSingleton.AssetManager;
                foreach (string asset in _assetsToReload)
                {
                    EdLog.Assets.Debug("Reloading asset: {x}", asset);
                    manager?.ForceReloadAsset(asset);
                }

                _assetsToReload.Clear();
            }
        }

        public bool IsImporting => _importerTasksTotal > 0;
        public float ImportProgress => (_importerTasksTotal - (_runningImports.Count + _pendingImports.Count)) / (float)_importerTasksTotal;

        internal IReadOnlyList<AssetImporterData> Importers => _importerList;

        internal AssetFilesystemWatcher ContentWatcher => _contentWatcher;
        internal AssetFilesystemWatcher SourceWatcher => _sourceWatcher;

        public AssetIdentifier Identifier => _identifier;
        public AssetConfiguration Configuration => _configuration;

        private string GetAssetPath(string localPath) => Path.Combine(EditorFilepaths.LibraryImportedPath, _identifier.GetOrRegisterAsset(localPath).Id.ToString() + ".iaf");

        //TODO: add support for already local paths
        private static bool EnsureLocalPath(string fullPath, [NotNullWhen(true)] out string? localPath)
        {
            localPath = null;

            string project = Editor.GlobalSingleton.ProjectPath;

            if (!File.Exists(fullPath))
            {
                string tempFullPath = Path.Combine(Editor.GlobalSingleton.ProjectPath, fullPath);
                if (File.Exists(tempFullPath))
                {
                    localPath = fullPath.Replace('\\', '/');
                    return true;
                }

                return false;
            }

            fullPath = Path.GetFullPath(fullPath);

            if (fullPath.Length > project.Length && fullPath.StartsWith(project))
            {
                localPath = fullPath.Substring(project.Length).Replace('\\', '/');
                return true;
            }

            return false;
        }

        internal record struct AssetImporterData(HashSet<string> AssociatedExtensions, IAssetImporter Importer);
        private record struct FileAssociationData(HashSet<string> AssociatedWith, HashSet<string> ExternalAssociates);
    }
}
