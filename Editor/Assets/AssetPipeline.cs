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
using System.Text;

namespace Editor.Assets
{
    public sealed class AssetPipeline : IDisposable
    {
        private object _importLock;
        private object _reloadLock;

        private AssetIdentifier _identifier;
        private AssetConfiguration _configuration;

        private AssetFilesystemWatcher _contentWatcher;
        private AssetFilesystemWatcher _sourceWatcher;

        private AssetFilesystemWatcher? _engineContentWatcher;
        private AssetFilesystemWatcher? _editorCotentWatcher;

        private List<AssetImporterData> _importerList;
        private Dictionary<string, IAssetImporter> _importerLookup;

        private ConcurrentDictionary<string, FileAssociationData> _fileAssocations;
        private HashSet<AssetId> _assetsToReload;

        private HashSet<string> _pendingImports;
        private Dictionary<string, Task> _runningImports;

        private ConcurrentDictionary<AssetId, DateTime> _importedAssets;

        private ProjectSubFilesystem[] _filesystems;

        private int _importerTasksTotal;
        private int _importerTasksCount;

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

            _importLock = new object();
            _reloadLock = new object();

            _identifier = new AssetIdentifier();
            _configuration = new AssetConfiguration(this);

            _contentWatcher = new AssetFilesystemWatcher(EditorFilepaths.ContentPath, Editor.GlobalSingleton.ProjectSubFilesystem, this);
            _sourceWatcher = new AssetFilesystemWatcher(EditorFilepaths.SourcePath, null!, this);

#if true || DEBUG
            _engineContentWatcher = new AssetFilesystemWatcher(@"D:/source/repos/Constructor/Source/Engine", Editor.GlobalSingleton.EngineFilesystem, this);
            _editorCotentWatcher = new AssetFilesystemWatcher(@"D:/source/repos/Constructor/Source/Editor", Editor.GlobalSingleton.EditorFilesystem, this);
#endif

            _importerList = new List<AssetImporterData>();
            _importerLookup = new Dictionary<string, IAssetImporter>();

            _fileAssocations = new ConcurrentDictionary<string, FileAssociationData>();
            _assetsToReload = new HashSet<AssetId>();

            _pendingImports = new HashSet<string>();
            _runningImports = new Dictionary<string, Task>();

            _importedAssets = new ConcurrentDictionary<AssetId, DateTime>();

            _importerTasksTotal = 0;

            _filesystems = [Editor.GlobalSingleton.ProjectSubFilesystem, Editor.GlobalSingleton.EditorFilesystem, Editor.GlobalSingleton.EngineFilesystem];

            string associationFile = AssocationsFilePath;
            if (File.Exists(associationFile))
                ReadAssociationsFile(associationFile);
            else
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
            AddImporter<TextureAssetImporter>(".png", ".jpg", ".jpeg");
            AddImporter<MaterialAssetImporter>(".mat");
            AddImporter<GeoSceneAssetImporter>(".geoscn");

            RefreshDatabase(needsDbRefresh);
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

                    FlushAssociationsFile();
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
        private void ReadAssociationsFile(string filePath)
        {
            StringBuilder sb = new StringBuilder();
            string[] lines = File.ReadAllLines(filePath);

            string[] temporaryArray = ArrayPool<string>.Shared.Rent(32);

            try
            {
                foreach (string line in lines)
                {
                    int i = 0;
                    foreach (ReadOnlySpan<char> file in line.Tokenize(';'))
                    {
                        temporaryArray[i++] = file.ToString();
                    }

                    MakeFileAssociations(temporaryArray[0], temporaryArray.AsMemory(1, i - 1));
                }
            }
            finally
            {
                ArrayPool<string>.Shared.Return(temporaryArray);
            }
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

                AssetId id = new AssetId(ulong.Parse(localPath));
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

            lock (_importLock)
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

        //TODO: implement an actual check as this can return false if something updates before PollFilesystem is called
        /// <summary>Thread-safe</summary>
        internal bool IsAssetUpToDate(ReadOnlySpan<char> path)
        {
            string str = path.ToString();

            lock (_importLock)
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
            lock (_importLock)
            {
                return _runningImports.ContainsKey(path.ToString());
            }
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
        internal Task? ImportChangesOrGetRunning(ReadOnlySpan<char> path)
        {
            return ImportNewFile(path.ToString());
        }

        /// <summary>Not thread-safe</summary>
        private void RefreshDatabase(bool cleanOldData)
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

                    _fileAssocations.Clear();
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

                        if (!IsFileImportedAndValid(id) && ImportNewFile(localPath) != null)
                            foundImportableFile = true;
                    }
                }

                PollContentUpdates();

                foundImportableFile = foundImportableFile || IsImporting;

                if (foundImportableFile)
                {
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
                }

                File.WriteAllText(AssetIdentifier.DataFilePath, _identifier.TrySerializeAssetIds());

                Editor.GlobalSingleton.ProjectShaderLibrary.FlushFileMappings();
                Editor.GlobalSingleton.ProjectSubFilesystem.FlushFileRemappings();

                Editor.GlobalSingleton.EngineFilesystem.FlushFileRemappings();
                Editor.GlobalSingleton.EditorFilesystem.FlushFileRemappings();

                FlushAssociationsFile();
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
        private Task? ImportNewFile(string localPath)
        {
            lock (_importLock)
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

                        _importerTasksTotal++;
                        if (!_runningImports.ContainsKey(localPath))
                        {
                            _pendingImports.Remove(localPath);

                            task = Task.Run(() =>
                            {
                                try
                                {
                                    int firstIndex = localPath.IndexOf('/');
                                    string @namespace = localPath.Substring(0, firstIndex);

                                    ProjectSubFilesystem filesystem = SelectAppropriateFilesystem(@namespace);

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
                                    EdLog.Assets.Error(ex, "Exception occured trying to import asset: {x}", localPath);
#if DEBUG
                                    throw;
#endif
                                }
                                finally
                                {
                                    Interlocked.Decrement(ref _importerTasksCount);
                                    lock (_importLock)
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
                                        ImportNewFile(@event.LocalPath);
                                        ImportAssociatedFiles(@event.LocalPath);
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

        private static int s_contentNamespaceHash = "Content".GetDjb2HashCode();
        private static int s_sourceNamespaceHash = "Source".GetDjb2HashCode();
        private static int s_engineNamespaceHash = "Engine".GetDjb2HashCode();
        private static int s_editorNamespaceHash = "Editor".GetDjb2HashCode();

        public string AssocationsFilePath = Path.Combine(EditorFilepaths.LibraryIntermediatePath, "FileAssociations.dat");
        public string ImportedAssetsFilePath = Path.Combine(EditorFilepaths.LibraryIntermediatePath, "ImportedAssets.dat");

        internal record struct AssetImporterData(HashSet<string> AssociatedExtensions, IAssetImporter Importer);
        private record struct FileAssociationData(HashSet<string> AssociatedWith, HashSet<string> ExternalAssociates);
    }
}
