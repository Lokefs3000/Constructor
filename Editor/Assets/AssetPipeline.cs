using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Editor.Assets.Importers;
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
        private SemaphoreSlim _semaphore;

        private AssetFilesystemWatcher _contentWatcher;
        private AssetFilesystemWatcher _sourceWatcher;

        private List<AssetImporterData> _importerList;

        private ConcurrentDictionary<string, FileAssociationData> _fileAssocations;
        private HashSet<string> _assetsToReload;

        private HashSet<string> _pendingImports;
        private Dictionary<string, Task> _runningImports;

        private int _importerTasksTotal;

        private int _activeAssociationAccesses;
        private bool _canAccessAssocations;

        private bool _disposedValue;

        internal AssetPipeline()
        {
            _semaphore = new SemaphoreSlim(1);

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

            bool needsDbRefresh = false;

            string associationFile = Path.Combine(EditorFilepaths.LibraryPath, "FileAssociations.dat");
            if (File.Exists(associationFile))
            {
                ReadAssociationsFile(associationFile);
            }
            else
            {
                needsDbRefresh = true;
            }

            if (!Directory.Exists(EditorFilepaths.LibraryAssetsPath))
            {
                Directory.CreateDirectory(EditorFilepaths.LibraryAssetsPath);
                needsDbRefresh = true;
            }

            if (!File.Exists(Path.Combine(EditorFilepaths.LibraryPath, "ShaderMappings.dat")))
                needsDbRefresh = true;
            if (!File.Exists(Path.Combine(EditorFilepaths.LibraryPath, "FileRemappings.dat")))
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

                    _semaphore.Dispose();

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

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

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

        private void FlushAssociationsFile()
        {
            string associationFile = Path.Combine(EditorFilepaths.LibraryPath, "FileAssociations.dat");

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

        internal void AddImporter<T>(params string[] associations) where T : IAssetImporter, new()
        {
            //TODO: add verification

            _importerList.Add(new AssetImporterData([.. associations], new T()));
        }

        public void MakeFileAssociations(string path, params string[] assocations)
         => MakeFileAssociations(path, assocations.AsMemory());

        //TODO: prevent associating a file with itself
        public void MakeFileAssociations(string path, ReadOnlyMemory<string> assocations)
        {
            if (!EnsureLocalPath(path, out string? localRootPath))
            {
                //bad
                return;
            }

            using (new SemaphoreScope(_semaphore))
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
                        y.ExternalAssociates.Add(localRootPath);
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
                            y.AssociatedWith.Add(localPath);
                    }

                    return y;
                });
            }
        }

        public void ReloadAsset(string assetPath)
        {
            if (EnsureLocalPath(assetPath, out string? localPath))
                assetPath = localPath;

            lock (_assetsToReload)
            {
                _assetsToReload.Add(assetPath);
            }
        }

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

        internal bool IsAssetUpToDate(ReadOnlySpan<char> path)
        {
            string str = path.ToString();

            lock (_runningImports)
            {
                if (_runningImports.ContainsKey(str))
                    return false;
            }

            lock (_pendingImports)
            {
                if (_pendingImports.Contains(str))
                    return false;
            }

            return true;
        }

        internal bool IsImportingAsset(ReadOnlySpan<char> path)
        {
            lock (_runningImports)
            {
                return _runningImports.ContainsKey(path.ToString());
            }
        }

        internal Task? ImportChangesOrGetRunning(ReadOnlySpan<char> path)
        {
            return ImportNewFile(path.ToString());
        }

        private void RefreshDatabase()
        {
            _contentWatcher.ResetInternalState();
            _sourceWatcher.ResetInternalState();

            _contentWatcher.ClearEventQueue();
            _sourceWatcher.ClearEventQueue();

            _fileAssocations.Clear();
            _assetsToReload.Clear();

            Directory.Delete(EditorFilepaths.LibraryAssetsPath, true);
            Directory.CreateDirectory(EditorFilepaths.LibraryAssetsPath);

            foreach (string file in Directory.EnumerateFiles(EditorFilepaths.ContentPath, "*.*", SearchOption.AllDirectories))
            {
                ImportNewFile(file.Substring(Editor.GlobalSingleton.ProjectPath.Length).Replace('\\', '/'));
            }

            unsafe
            {
                //TODO: implement gui (prob just OS widgets) and show a list of pending assets

                SDL_Window* window = SDL3.SDL_CreateWindow("Importing assets..", 500, 200, (SDL_WindowFlags)0);
                SDL3.SDL_SetWindowProgressState(window, SDL_ProgressState.SDL_PROGRESS_STATE_NORMAL);

                while (_runningImports.Count > 0)
                {
                    SDL3.SDL_PumpEvents();
                    SDL3.SDL_FlushEvents(SDL_EventType.SDL_EVENT_FIRST, SDL_EventType.SDL_EVENT_LAST);
                    SDL3.SDL_SetWindowProgressValue(window, ImportProgress);

                    Thread.Sleep(250);
                }

                SDL3.SDL_SetWindowProgressState(window, SDL_ProgressState.SDL_PROGRESS_STATE_NONE);
                SDL3.SDL_DestroyWindow(window);
            }

            Editor.GlobalSingleton.ProjectShaderLibrary.FlushFileMappings();
            Editor.GlobalSingleton.ProjectSubFilesystem.FlushFileRemappings();

            FlushAssociationsFile();
        }

        //TODO: avoid importing files that are not changed
        private Task? ImportNewFile(string localPath)
        {
            lock (_runningImports)
            {
                if (_runningImports.TryGetValue(localPath, out Task? task))
                {
                    return task;
                }
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

                    lock (_runningImports)
                    {
                        _importerTasksTotal++;
                        if (!_runningImports.ContainsKey(localPath))
                        {
                            lock (_pendingImports)
                            {
                                _pendingImports.Remove(localPath);

                                Task task = Task.Run(() =>
                                {
                                    try
                                    {
                                        importer.Importer.Import(this, realPath, assetPath);
                                    }
                                    catch (Exception ex)
                                    {
                                        EdLog.Assets.Error(ex, "Exception occured trying to import asset: {x}", localPath);
                                    }
                                    finally
                                    {
                                        //using (new SemaphoreScope(_incrSemaphore))
                                        //    _importerTasksCompleted++;

                                        lock (_runningImports)
                                        {
                                            _runningImports.Remove(localPath);
                                        }

                                        lock (_pendingImports)
                                        {
                                            if (_pendingImports.Contains(localPath))
                                            {
                                                ImportNewFile(localPath);
                                            }
                                        }
                                    }
                                });

                                _runningImports.Add(localPath, task);
                                return task;
                            }
                        }
                        else
                        {
                            lock (_pendingImports)
                            {
                                _pendingImports.Add(localPath);
                            }

                            return _runningImports[localPath];
                        }
                    }
                }
            }

            return null;
        }

        private void CleanOldFile(string localPath)
        {
            string assetPath = GetAssetPath(localPath);

        }

        private HashSet<string> _assocationRingAvoidance = new HashSet<string>();
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
                    foreach (string association in associationData.ExternalAssociates)
                    {
                        ImportNewFile(association);
                        Logic(association);
                    }
                }
            }
        }

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

        private void PollContentUpdates()
        {
            if (_semaphore.Wait(0))
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
                    _semaphore.Release();
                }

                if (_importerTasksTotal > 0)
                {
                    int total = _runningImports.Count + _pendingImports.Count;
                    if (total == 0)
                    {
                        lock (_runningImports)
                        {
                            lock (_pendingImports)
                            {
                                total = _runningImports.Count + _pendingImports.Count;
                                if (total == 0)
                                {
                                    _importerTasksTotal = 0;
                                }
                            }
                        }
                    }
                }
            }
        }

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

        private static string GetAssetPath(string localPath) => Path.Combine(EditorFilepaths.LibraryAssetsPath, ((uint)localPath.GetDjb2HashCode()).ToString() + ".iaf");

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
