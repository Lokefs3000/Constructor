using CommunityToolkit.HighPerformance;
using Editor.Assets.Importers;
using Editor.Assets.Loaders;
using Editor.Assets.Types;
using Editor.Platform.Windows;
using Primary.Assets;
using Primary.Common;
using Primary.Profiling;
using SharpGen.Runtime;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.Assets
{
    public sealed class AssetPipeline : IDisposable
    {
        private object _importLock;
        private object _reloadLock;

        private AssetIdentifier _identifier;
        private AssetConfiguration _configuration;
        private AssetAssociator _associator;

        private AssetFilesystemWatcher _contentWatcher;
        private AssetFilesystemWatcher _sourceWatcher;

        private AssetFilesystemWatcher? _engineContentWatcher;
        private AssetFilesystemWatcher? _editorCotentWatcher;

        private List<AssetImporterData> _importerList;
        private Dictionary<string, IAssetImporter> _importerLookup;

        private HashSet<AssetId> _assetsToReload;

        private HashSet<AssetId> _pendingImports;
        private Dictionary<AssetId, Task> _runningImports;

        private ConcurrentDictionary<AssetId, DateTime> _importedAssets;

        private ProjectSubFilesystem[] _filesystems;

        private int _importerTasksTotal;
        private int _importerTasksCount;

        private bool _disposedValue;

        internal AssetPipeline(StartupDisplayUI startupUi)
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

            _importLock = new object();
            _reloadLock = new object();

            _identifier = new AssetIdentifier();
            _configuration = new AssetConfiguration(this);
            _associator = new AssetAssociator();

            _contentWatcher = new AssetFilesystemWatcher(EditorFilepaths.ContentPath, Editor.GlobalSingleton.ProjectSubFilesystem, this);
            _sourceWatcher = new AssetFilesystemWatcher(EditorFilepaths.SourcePath, null!, this);

#if true || DEBUG
            _engineContentWatcher = new AssetFilesystemWatcher(EditorFilepaths.EnginePath, Editor.GlobalSingleton.EngineFilesystem, this);
            _editorCotentWatcher = new AssetFilesystemWatcher(EditorFilepaths.EditorPath, Editor.GlobalSingleton.EditorFilesystem, this);
#endif

            _importerList = new List<AssetImporterData>();
            _importerLookup = new Dictionary<string, IAssetImporter>();

            _assetsToReload = new HashSet<AssetId>();

            _pendingImports = new HashSet<AssetId>();
            _runningImports = new Dictionary<AssetId, Task>();

            _importedAssets = new ConcurrentDictionary<AssetId, DateTime>();

            _importerTasksTotal = 0;

            _filesystems = [Editor.GlobalSingleton.ProjectSubFilesystem, Editor.GlobalSingleton.EditorFilesystem, Editor.GlobalSingleton.EngineFilesystem];

            if (!_associator.ReadAssocations())
                needsDbRefresh = true;

            string importedAssetsFile = ImportedAssetsFilePath;
            if (File.Exists(importedAssetsFile))
                ReadImportedAssetsFile(importedAssetsFile);

            if (!File.Exists(Path.Combine(EditorFilepaths.LibraryIntermediatePath, "ShaderMappings.dat")))
                needsDbRefresh = true;
            if (!File.Exists(Path.Combine(EditorFilepaths.LibraryIntermediatePath, Editor.GlobalSingleton.ProjectSubFilesystem.FileRemappingsFile)))
                needsDbRefresh = true;
            if (!File.Exists(Path.Combine(EditorFilepaths.LibraryIntermediatePath, Editor.GlobalSingleton.EngineFilesystem.FileRemappingsFile)))
                needsDbRefresh = true;
            if (!File.Exists(Path.Combine(EditorFilepaths.LibraryIntermediatePath, Editor.GlobalSingleton.EditorFilesystem.FileRemappingsFile)))
                needsDbRefresh = true;
            if (!File.Exists(AssetIdentifier.DataFilePath))
                needsDbRefresh = true;

            AddImporter<ModelAssetImporter>(".fbx", ".obj");
            AddImporter<ShaderAssetImporter>(".hlsl");
            AddImporter<TextureAssetImporter>(".png", ".jpg", ".jpeg", ".texcomp", ".cubemap");
            AddImporter<MaterialAssetImporter>(".mat");
            AddImporter<GeoSceneAssetImporter>(".geoscn");
            AddImporter<EffectVolumeAssetImporter>(".fxvol");

            RefreshDatabase(needsDbRefresh, startupUi);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _engineContentWatcher?.Dispose();
                    _editorCotentWatcher?.Dispose();

                    _contentWatcher.Dispose();
                    _sourceWatcher.Dispose();

                    File.WriteAllText(AssetIdentifier.DataFilePath, _identifier.TrySerializeAssetIds());

                    Editor.GlobalSingleton.ProjectShaderLibrary.FlushFileMappings();
                    Editor.GlobalSingleton.ProjectSubFilesystem.FlushFileRemappings();

                    Editor.GlobalSingleton.EngineFilesystem.FlushFileRemappings();
                    Editor.GlobalSingleton.EditorFilesystem.FlushFileRemappings();

                    _associator.FlushAssociations();
                    FlushImportedAssets();
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
        private void ReadImportedAssetsFile(string filePath)
        {
            string? fileSource = FileUtility.TryReadAllText(filePath);

            if (fileSource == null)
            {
                EdLog.Assets.Warning("Failed to open imported assets file: {f}", filePath);
                return;
            }

            Range[] ranges = new Range[2];

            foreach (ReadOnlySpan<char> line in fileSource.Tokenize('\n'))
            {
                if (ReadOnlySpanExtensions.Count(line, ';') != 1)
                    continue;

                line.Split(ranges, ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                ReadOnlySpan<char> localPath = line.Slice(ranges[0].Start.Value, ranges[0].End.Value - ranges[0].Start.Value);
                ReadOnlySpan<char> lastWriteTime = line.Slice(ranges[1].Start.Value, ranges[1].End.Value - ranges[1].Start.Value);

                AssetId id = new AssetId(uint.Parse(localPath));
                if (!_identifier.IsIdValid(id))
                {
                    EdLog.Assets.Warning("Invalid asset id in imported assets: {id}", id);
                    continue;
                }

                if (!_importedAssets.TryAdd(id, new DateTime(long.Parse(lastWriteTime))))
                {
                    EdLog.Assets.Warning("Duplicate imported asset with id: {id} (p:{path})", id, _identifier.RetrievePathForId(id));
                    continue;
                }
            }
        }

        /// <summary>Not thread-safe</summary>
        private void FlushImportedAssets()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in _importedAssets)
            {
                sb.Append(kvp.Key.Value);
                sb.Append(';');
                sb.Append(kvp.Value.Ticks);
                sb.AppendLine();
            }

            File.WriteAllText(ImportedAssetsFilePath, sb.ToString());
        }

        /// <summary>Not thread-safe</summary>
        internal void AddImporter<T>(params string[] associations) where T : class, IAssetImporter, new()
        {
            //TODO: add verification

            T importer = new T();
            _importerList.Add(new AssetImporterData([.. associations], importer));

            for (int i = 0; i < associations.Length; i++)
            {
                if (!_importerLookup.TryAdd(associations[i], importer))
                {
                    EdLog.Assets.Warning("[i:{im}]: A previous asset importer has already taken association: {asso}", typeof(T).Name, associations[i]);
                }
            }
        }

        /// <summary>Thread-safe</summary>
        public void ReloadAsset(AssetId assetId)
        {
            if (assetId.IsInvalid)
                return;

            lock (_reloadLock)
            {
                _assetsToReload.Add(assetId);
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

        /// <summary>Thread-safe</summary>
        private bool IsAssetUpToDate(AssetId id, string localPath)
        {
            ProjectSubFilesystem? filesystem = SelectAppropriateFilesystem(GetFileNamespace(localPath));
            if (filesystem == null)
            {
                EdLog.Assets.Error("Failed to find filesystem for local path: {lp}", localPath);
                return false;
            }

            if (_importedAssets.TryGetValue(id, out DateTime time))
            {
                return File.GetLastWriteTime(filesystem.GetFullPath(localPath)) == time;
            }

            return false;
        }

        /// <summary>Thread-safe</summary>
        internal bool IsAssetUpToDate(AssetId id)
        {
            string? localPath = _identifier.RetrievePathForId(id);
            if (localPath == null)
            {
                EdLog.Assets.Error("Failed to find local path for id: {id}", id);
                return false;
            }

            return IsAssetUpToDate(id, localPath);
        }

        /// <summary>Thread-safe</summary>
        internal bool IsAssetUpToDate(string localPath)
        {
            if (!_identifier.TryGetAssetId(localPath, out AssetId id))
            {
                EdLog.Assets.Error("Failed to find id for local path: {lp}", localPath);
                return false;
            }

            return IsAssetUpToDate(id, localPath);
        }

        /// <summary>Thread-safe</summary>
        internal bool IsImportingAsset(AssetId id)
        {
            lock (_importLock)
            {
                return _runningImports.ContainsKey(id);
            }
        }

        /// <summary>Thread-safe</summary>
        internal bool IsImportingAsset(string localPath)
        {
            if (_identifier.TryGetAssetId(localPath, out AssetId id))
            {
                lock (_importLock)
                {
                    return _runningImports.ContainsKey(id);
                }
            }

            EdLog.Assets.Error("Failed to find id for local path: {lp}", localPath);
            return false;
        }

        /// <summary>Not thread-safe</summary>
        internal bool IsFileImportedAndValid(AssetId id, ProjectSubFilesystem? subFilesystem = null)
        {
            string? path = _identifier.RetrievePathForId(id);
            if (path == null)
                return false;
            if (!TryGetImporter(path, out IAssetImporter? importer))
                return false;

            if (subFilesystem == null)
            {
                int firstIndex = path.IndexOf('/');
                if (firstIndex == -1)
                    return false;

                ReadOnlySpan<char> @namespace = path.AsSpan().Slice(0, firstIndex);

                subFilesystem = SelectAppropriateFilesystem(@namespace);
                if (subFilesystem == null)
                    return false;
            }

            if (_importedAssets.TryGetValue(id, out DateTime lastWriteTime))
            {
                DateTime lastFileWriteTime = File.GetLastWriteTime(subFilesystem.GetFullPath(path));
                if (lastFileWriteTime != lastWriteTime)
                    return false;
            }
            else
                return false;

            if (subFilesystem.Exists(path))
            {
                bool ret = importer.ValidateFile(path.ToString(), subFilesystem, this);
                return ret;
            }

            return false;
        }

        /// <summary>Thread-safe</summary>
        internal Task? ImportChangesOrGetRunning(AssetId id) => ImportNewFile(id);

        /// <summary>Thread-safe</summary>
        internal Task? ImportChangesOrGetRunning(string localPath)
        {
            if (_identifier.TryGetAssetId(localPath, out AssetId id))
                return ImportNewFile(id);

            EdLog.Assets.Warning("Failed to find id for local path: {lp}", localPath);
            return null;
        }

        /// <summary>Not thread-safe</summary>
        private void RefreshDatabase(bool cleanOldData, StartupDisplayUI startupUi)
        {
            long timeStart = Stopwatch.GetTimestamp();

            try
            {
                if (cleanOldData)
                {
                    _contentWatcher.ResetInternalState();
                    _sourceWatcher.ResetInternalState();

                    _engineContentWatcher?.ResetInternalState();
                    _editorCotentWatcher?.ResetInternalState();

                    _contentWatcher.ClearEventQueue();
                    _sourceWatcher.ClearEventQueue();

                    _engineContentWatcher?.ClearEventQueue();
                    _editorCotentWatcher?.ClearEventQueue();

                    _associator.ClearAllAssocations();
                    _assetsToReload.Clear();

                    Directory.Delete(EditorFilepaths.LibraryImportedPath, true);
                    Directory.CreateDirectory(EditorFilepaths.LibraryImportedPath);
                }

                Editor.GlobalSingleton.AssetDatabase.PurgeDatabase();

                foreach (var kvp in _importedAssets)
                {
                    string? path = _identifier.RetrievePathForId(kvp.Key);
                    if (path != null)
                    {
                        if (!TryGetImporter(path, out IAssetImporter? importer))
                            continue;

                        int idx = path.IndexOf('/');
                        ProjectSubFilesystem? filesystem = SelectAppropriateFilesystem(idx != -1 ? path.AsSpan(0, idx) : path.AsSpan());
                        if (filesystem == null || !filesystem.Exists(path))
                            continue;

                        importer.Preload(path, filesystem, this);
                    }
                }

                bool foundImportableFile = false;

                IterateFiles(EditorFilepaths.ContentPath);
                IterateFiles("D:/source/repos/Constructor/Source/Engine");
                IterateFiles("D:/source/repos/Constructor/Source/Editor");

                void IterateFiles(string sourceDir)
                {
                    string rootDir = Path.GetDirectoryName(sourceDir)!;

                    foreach (string file in Directory.EnumerateFiles(sourceDir, "*.*", SearchOption.AllDirectories))
                    {
                        string localPath = file.Substring(rootDir.Length + 1).Replace('\\', '/');
                        AssetId id = _identifier.GetOrRegisterAsset(localPath);

                        if (!IsFileImportedAndValid(id))
                        {
                            if (TryGetImporter(localPath, out IAssetImporter? importer))
                            {
                                ProjectSubFilesystem? filesystem = SelectAppropriateFilesystem(GetFileNamespace(localPath));
                                if (filesystem != null && filesystem.Exists(localPath))
                                {
                                    importer?.Preload(localPath, filesystem, this);
                                }
                            }

                            if (ImportNewFile(id) != null)
                                foundImportableFile = true;
                        }
                    }
                }

                PollContentUpdates();

                foundImportableFile = foundImportableFile || IsImporting;

                if (foundImportableFile)
                {
                    startupUi.PushStep("Importing assets");

                    while (true)
                    {
                        //PollContentUpdates();

                        int total = _importerTasksTotal;
                        int completed = total - _importerTasksCount;

                        startupUi.Description = $"{completed}/{total}";
                        startupUi.Progress = completed / (float)total;

                        if (completed >= total)
                            break;

                        Thread.Sleep(50);
                    }

                    startupUi.PopStep();
                }

                File.WriteAllText(AssetIdentifier.DataFilePath, _identifier.TrySerializeAssetIds());

                Editor.GlobalSingleton.ProjectShaderLibrary.FlushFileMappings();
                Editor.GlobalSingleton.ProjectSubFilesystem.FlushFileRemappings();

                Editor.GlobalSingleton.EngineFilesystem.FlushFileRemappings();
                Editor.GlobalSingleton.EditorFilesystem.FlushFileRemappings();

                _associator.FlushAssociations();
                FlushImportedAssets();
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Failed to refresh asset pipeline database");
                throw;
            }

            long diff = Stopwatch.GetTimestamp() - timeStart;
            EdLog.Assets.Information("Asset refresh took: {secs}s", diff / (double)Stopwatch.Frequency);
        }

        //TODO: avoid importing files that are not changed
        /// <summary>Thread-safe</summary>
        private Task? ImportNewFile(AssetId id)
        {
            lock (_importLock)
            {
                if (_runningImports.TryGetValue(id, out Task? task))
                {
                    return task;
                }

                string? localPath = _identifier.RetrievePathForId(id);
                if (localPath == null)
                {
                    EdLog.Assets.Error("Failed to find path for id: {id}", id);
                    return null;
                }

                ProjectSubFilesystem? filesystem = SelectAppropriateFilesystem(GetFileNamespace(localPath));
                if (filesystem == null)
                {
                    EdLog.Assets.Error("Failed to find filesystem for local path: {p}", localPath);
                    return null;
                }

                string extension = Path.GetExtension(localPath);

                Span<AssetImporterData> importers = _importerList.AsSpan();

                for (int i = 0; i < importers.Length; i++)
                {
                    AssetImporterData importer = importers[i];
                    if (importer.AssociatedExtensions.Contains(extension))
                    {
                        //Log.Information("importing: {x}", localPath);

                        _importerTasksTotal++;
                        if (!_runningImports.ContainsKey(id))
                        {
                            _pendingImports.Remove(id);

                            task = Task.Run(() =>
                            {
                                try
                                {
                                    string realPath = Path.Combine(filesystem.AbsolutePath, localPath);
                                    string assetPath = GetAssetPath(localPath);

                                    bool ret = importer.Importer.Import(this, filesystem, realPath, assetPath, assetPath.Substring(Editor.GlobalSingleton.ProjectPath.Length));
                                    if (ret)
                                    {
                                        _importedAssets[_identifier.GetOrRegisterAsset(localPath)] = File.GetLastWriteTime(realPath);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    EdLog.Assets.Error(ex, "Exception occured trying to import asset: {x}", id);
#if DEBUG
                                    throw;
#endif
                                }
                                finally
                                {
                                    Interlocked.Decrement(ref _importerTasksCount);
                                    lock (_importLock)
                                    {
                                        _runningImports.Remove(id);
                                        //if (_pendingImports.Contains(localPath))
                                        //{
                                        //    ImportNewFile(localPath);
                                        //}
                                    }
                                }
                            });

                            Interlocked.Increment(ref _importerTasksCount);

                            _runningImports.Add(id, task);
                            return task;
                        }
                        else
                        {
                            _pendingImports.Add(id);
                            return _runningImports[id];
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

        private HashSet<AssetId> _assocationRingAvoidance = new HashSet<AssetId>();
        /// <summary>Not thread-safe</summary>
        private void ImportAssociatedFiles(AssetId localPath)
        {
            _assocationRingAvoidance.Clear();
            Logic(localPath);

            void Logic(AssetId path)
            {
                if (!_assocationRingAvoidance.Add(path))
                {
                    return;
                }

                using (_associator.GetDependentsWithLockScope(path, out HashSet<AssetId>? dependencies))
                {
                    if (dependencies != null)
                    {
                        foreach (AssetId association in dependencies)
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
        private void PollContentUpdates(bool waitForSemaphore = false)
        {
            if (Monitor.TryEnter(_importLock, waitForSemaphore ? Timeout.Infinite : 0))
            {
                try
                {
                    PumpWatcher(_contentWatcher);
                    if (_engineContentWatcher != null)
                        PumpWatcher(_engineContentWatcher);
                    if (_editorCotentWatcher != null)
                        PumpWatcher(_editorCotentWatcher);

                    void PumpWatcher(AssetFilesystemWatcher watcher)
                    {
                        while (watcher.PollEvent(out FilesystemEvent @event))
                        {
                            NewFilesystemEvent?.Invoke(watcher.SubFilesystem, @event);
                            switch (@event.Type)
                            {
                                case FilesystemEventType.FileAdded:
                                case FilesystemEventType.FileChanged:
                                    {
                                        AssetId id = _identifier.GetOrRegisterAsset(@event.LocalPath);

                                        ImportAssociatedFiles(id);
                                        ImportNewFile(id);
                                        break;
                                    }
                                case FilesystemEventType.FileRemoved: CleanOldFile(@event.LocalPath); break;
                                case FilesystemEventType.FileRenamed:
                                    {
                                        Debug.Assert(@event.NewLocalPath != null);

                                        if (_identifier.TryGetAssetId(@event.LocalPath, out AssetId id))
                                            _identifier.ChangeIdPath(id, @event.LocalPath, @event.NewLocalPath);

                                        FileRenamed?.Invoke(@event.LocalPath, @event.NewLocalPath);
                                        break;
                                    }
                            }
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(_importLock);
                }

                if (_importerTasksTotal > 0)
                {
                    int total = _runningImports.Count + _pendingImports.Count;
                    if (total == 0)
                    {
                        _importerTasksTotal = 0;
                    }
                }
            }
        }

        /// <summary>Thread-safe</summary>
        private void ReloadPendingAssets()
        {
            //avoiding a deadlock that can occur somehow when the filesystem waits on a file import on the main thread
            if (Monitor.TryEnter(_reloadLock, 0))
            {
                try
                {
                    AssetManager manager = Editor.GlobalSingleton.AssetManager;
                    foreach (AssetId asset in _assetsToReload)
                    {
                        manager?.ForceReloadAsset(asset);
                    }

                    _assetsToReload.Clear();
                }
                finally
                {
                    Monitor.Exit(_reloadLock);
                }
            }
        }

        /// <summary>Thread-safe</summary>
        /// <param name="key">Accepts as format: "Path/To/File.extension" or ".extension"</param>
        private bool TryGetImporter(ReadOnlySpan<char> key, [NotNullWhen(true)] out IAssetImporter? importer)
        {
            importer = null;

            int idx = key.LastIndexOf('.');
            if (idx == -1)
                return false;

            if (idx != 0)
                key = key.Slice(idx);

            return _importerLookup.TryGetValue(key.ToString(), out importer);
        }

        public bool IsImporting => _importerTasksTotal > 0;
        public float ImportProgress => (_importerTasksTotal - (_runningImports.Count + _pendingImports.Count)) / (float)_importerTasksTotal;

        internal IReadOnlyList<AssetImporterData> Importers => _importerList;

        internal AssetFilesystemWatcher ContentWatcher => _contentWatcher;
        internal AssetFilesystemWatcher SourceWatcher => _sourceWatcher;

        internal AssetFilesystemWatcher? EngineWatcher => _engineContentWatcher;
        internal AssetFilesystemWatcher? EditorWatcher => _editorCotentWatcher;

        public AssetIdentifier Identifier => _identifier;
        public AssetConfiguration Configuration => _configuration;
        public AssetAssociator Associator => _associator;

        internal event Action<ProjectSubFilesystem, FilesystemEvent>? NewFilesystemEvent;
        internal event Action<string, string>? FileRenamed;

        /// <summary>Thread-safe</summary>
        private string GetAssetPath(string localPath) => Path.Combine(EditorFilepaths.LibraryImportedPath, _identifier.GetOrRegisterAsset(localPath).ToString() + ".iaf").Replace('\\', '/');

        //TODO: add support for already local paths
        private bool EnsureLocalPath(string fullPath, [NotNullWhen(true)] out string? localPath)
        {
            localPath = null;

            int firstIndex = fullPath.IndexOf('/');

            if (firstIndex != -1)
            {
                string @namespace = fullPath.Substring(0, firstIndex);
                ProjectSubFilesystem? filesystem = SelectAppropriateFilesystem(@namespace);

                if (filesystem != null)
                {
                    localPath = fullPath;
                    return filesystem.Exists(fullPath);
                }
            }

            for (int i = 0; i < _filesystems.Length; i++)
            {
                ProjectSubFilesystem subFilesystem = _filesystems[i];
                if (fullPath.Length > subFilesystem.AbsolutePath.Length && fullPath.StartsWith(subFilesystem.AbsolutePath))
                {
                    string subString = fullPath.Substring(subFilesystem.AbsolutePath.Length).Replace('\\', '/');
                    if (subFilesystem.Exists(subString))
                    {
                        localPath = subString;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Thread-safe</summary>
        public static ProjectSubFilesystem? SelectAppropriateFilesystem(ReadOnlySpan<char> @namespace)
        {
            int hash = (int)@namespace.GetDjb2HashCode();

            if (hash == s_contentNamespaceHash)
                return Editor.GlobalSingleton.ProjectSubFilesystem;
            else if (hash == s_sourceNamespaceHash)
                throw new NotImplementedException();
            else if (hash == s_engineNamespaceHash)
                return Editor.GlobalSingleton.EngineFilesystem;
            else if (hash == s_editorNamespaceHash)
                return Editor.GlobalSingleton.EditorFilesystem;

            return null;
        }

        /// <summary>Thread-safe</summary>
        public static ReadOnlySpan<char> GetFileNamespace(ReadOnlySpan<char> path)
        {
            int idx = path.IndexOf('/');
            if (idx == -1 || idx == 0)
                return path;

            return path.Slice(0, idx);
        }

        /// <summary>Thread-safe</summary>
        public static bool IsLocalPath(ReadOnlySpan<char> path)
        {
            int idx = path.IndexOf('/');
            if (idx == -1 || idx == 0)
                return false;

            int hash = path.Slice(0, idx).GetDjb2HashCode();

            if (hash == s_contentNamespaceHash || hash == s_sourceNamespaceHash || hash == s_engineNamespaceHash || hash == s_editorNamespaceHash)
                return true;
            else
                return false;
        }

        /// <summary>Thread-safe</summary>
        public static bool TryGetLocalPathFromFull(string fullPath, [NotNullWhen(true)] out string? localPath)
        {
            fullPath = fullPath.Replace('\\', '/');
            ReadOnlySpan<char> span = fullPath.AsSpan();

            if (GetFileNamespace(span.Slice(EditorFilepaths.ContentPath.Length - 7)).GetDjb2HashCode() == s_contentNamespaceHash)
            {
                localPath = fullPath.Substring(EditorFilepaths.ContentPath.Length - 7);
                return true;
            }
            else if (GetFileNamespace(span.Slice(EditorFilepaths.SourcePath.Length - 6)).GetDjb2HashCode() == s_sourceNamespaceHash)
            {
                localPath = fullPath.Substring(EditorFilepaths.SourcePath.Length - 6);
                return true;
            }
            else if (GetFileNamespace(span.Slice(EditorFilepaths.EnginePath.Length - 6)).GetDjb2HashCode() == s_engineNamespaceHash)
            {
                localPath = fullPath.Substring(EditorFilepaths.EnginePath.Length - 6);
                return true;
            }
            else if (GetFileNamespace(span.Slice(EditorFilepaths.EditorPath.Length - 5)).GetDjb2HashCode() == s_editorNamespaceHash)
            {
                localPath = fullPath.Substring(EditorFilepaths.EditorPath.Length - 5);
                return true;
            }

            localPath = null;
            return false;
        }

        private static readonly int s_contentNamespaceHash = "Content".GetDjb2HashCode();
        private static readonly int s_sourceNamespaceHash = "Source".GetDjb2HashCode();
        private static readonly int s_engineNamespaceHash = "Engine".GetDjb2HashCode();
        private static readonly int s_editorNamespaceHash = "Editor".GetDjb2HashCode();

        public string ImportedAssetsFilePath = Path.Combine(EditorFilepaths.LibraryIntermediatePath, "ImportedAssets.dat");

        internal record struct AssetImporterData(HashSet<string> AssociatedExtensions, IAssetImporter Importer);
        private record struct FileAssociationData(Lock Lock, HashSet<AssetVariantId> ExternalAssociates);
    }

    public readonly record struct AssetVariantId : IEquatable<AssetVariantId>
    {
        private readonly AssetId _id;
        private readonly string? _path;

        public AssetVariantId(AssetId id)
        {
            _id = id;
            _path = null;
        }

        public AssetVariantId(string path)
        {
            _id = AssetId.Invalid;
            _path = null;
        }

        public override int GetHashCode() => _path?.GetHashCode() ?? _id.GetHashCode();
        public override string ToString() => _path ?? _id.ToString();

        public bool Equals(AssetVariantId other)
        {
            if (_path == null)
                return other._id == _id;
            else
                return _path == other._path;
        }

        public AssetId Id => _id;
        public string? Path => _path;

        public static explicit operator AssetVariantId(AssetId id) => new AssetVariantId(id);
        public static explicit operator AssetVariantId(string path) => new AssetVariantId(path);
    }
}
