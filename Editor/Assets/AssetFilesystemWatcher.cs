using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using Primary.Common;
using System.Collections.Concurrent;
using System.Text;
using System.Xml.Linq;

namespace Editor.Assets
{
    internal sealed class AssetFilesystemWatcher : IDisposable
    {
        private string _directory;
        private string _rootDirectory;

        private ProjectSubFilesystem _subFilesystem;
        private AssetPipeline _pipeline;

        private ConcurrentDictionary<string, AssetFile> _activeFiles;
        private HashSet<string> _rememberedFiles;

        private Dictionary<string, AssetDirectory> _directoryTree;

        private ConcurrentQueue<FilesystemEvent> _eventQueue;

        private FileSystemWatcher _watcher;

        private bool _disposedValue;

        internal AssetFilesystemWatcher(string directory, ProjectSubFilesystem subFilesystem, AssetPipeline pipeline)
        {
            _directory = directory;
            _rootDirectory = Path.GetDirectoryName(directory)!;

            _subFilesystem = subFilesystem;
            _pipeline = pipeline;

            _activeFiles = new ConcurrentDictionary<string, AssetFile>();
            _rememberedFiles = new HashSet<string>();

            _directoryTree = new Dictionary<string, AssetDirectory>();

            _eventQueue = new ConcurrentQueue<FilesystemEvent>();

            string dataFilePath = Path.Combine(FilesystemDataFile);
            if (File.Exists(dataFilePath))
            {
                DeserializeDataFile(dataFilePath);
            }

            SearchInitialDirectory(directory);

            _watcher = new FileSystemWatcher(directory)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                InternalBufferSize = 65536/*64kb*/,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            _watcher.Created += (a, b) =>
            {
                try
                {
                    FileAttributes attributes = File.GetAttributes(b.FullPath);
                    if (FlagUtility.HasFlag(attributes, FileAttributes.Directory))
                    {
                        string localDirectory = b.FullPath.Substring(_rootDirectory.Length + 1).Replace('\\', '/');
                        AddLocalDirectory(null, b.FullPath, localDirectory);
                    }
                    else
                    {
                        string localFile = b.FullPath.Substring(_rootDirectory.Length + 1).Replace('\\', '/');
                        if (_rememberedFiles.Contains(localFile))
                        {
                            return;
                        }

                        PushFileAddedEvent(localFile);

                        _rememberedFiles.Add(localFile);
                        _activeFiles.TryAdd(localFile, new AssetFile(File.GetLastWriteTime(localFile)));

                        string? directory = Path.GetDirectoryName(localFile);
                        if (directory != null)
                        {
                            string localDirectory = (Path.IsPathFullyQualified(directory) ? directory.Substring(_rootDirectory.Length + 1) : directory).Replace('\\', '/');
                            AddLocalDirectory(localFile, directory, localDirectory);
                        }
                    }
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "Failed to handle callback for file creation");
                }
            };

            _watcher.Deleted += (a, b) =>
            {
                try
                {
                    string localFile = b.FullPath.Substring(_rootDirectory.Length + 1).Replace('\\', '/');

                    if (_directoryTree.ContainsKey(localFile))
                    {
                        RemoveLocalDirectory(b.FullPath.Substring(_rootDirectory.Length + 1).Replace('\\', '/'), true);
                    }
                    else if (_rememberedFiles.Contains(localFile))
                    {
                        PushFileRemovedEvent(localFile);

                        _rememberedFiles.Remove(localFile);
                        _activeFiles.Remove(localFile, out _);

                        string? directory = Path.GetDirectoryName(b.FullPath);
                        if (directory != null)
                        {
                            string localDirectory = (Path.IsPathFullyQualified(directory) ? directory.Substring(_rootDirectory.Length + 1) : directory).Replace('\\', '/');
                            RemoveFromLocalDirectory(localFile, localDirectory);
                        }
                    }
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "Failed to handle callback for file deletion");
                }
            };

            _watcher.Renamed += (a, b) =>
            {
                try
                {
                    FileAttributes attributes = File.GetAttributes(b.FullPath);
                    if (FlagUtility.HasFlag(attributes, FileAttributes.Directory))
                    {
                        string oldLocalDirectory = b.OldFullPath.Substring(_rootDirectory.Length + 1).Replace('\\', '/');
                        string newLocalDirectory = b.FullPath.Substring(_rootDirectory.Length + 1).Replace('\\', '/');
                        RenameLocalDirectory(oldLocalDirectory, newLocalDirectory);
                    }
                    else
                    {
                        string oldLocalFile = b.OldFullPath.Substring(_rootDirectory.Length + 1).Replace('\\', '/');
                        if (!_rememberedFiles.Contains(oldLocalFile))
                        {
                            return;
                        }

                        string newLocalFile = b.FullPath.Substring(_rootDirectory.Length + 1).Replace('\\', '/');

                        PushFileRenamedEvent(oldLocalFile, newLocalFile);

                        _rememberedFiles.Remove(oldLocalFile);
                        _rememberedFiles.Add(newLocalFile);

                        _activeFiles.TryRemove(oldLocalFile, out _);
                        _activeFiles.TryAdd(newLocalFile, new AssetFile(File.GetLastWriteTime(newLocalFile)));

                        string? directory = Path.GetDirectoryName(newLocalFile);
                        if (directory != null)
                        {
                            string localDirectory = (Path.IsPathFullyQualified(directory) ? directory.Substring(_rootDirectory.Length + 1) : directory).Replace('\\', '/');
                            RenameLocalFileInDirectory(oldLocalFile, newLocalFile, localDirectory);
                        }
                    }
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "Failed to handle callback for file rename");
                }
            };

            _watcher.Changed += (a, b) =>
            {
                try
                {
                    FileAttributes attributes = File.GetAttributes(b.FullPath);
                    if (!FlagUtility.HasFlag(attributes, FileAttributes.Directory))
                    {
                        string localFile = b.FullPath.Substring(_rootDirectory.Length + 1).Replace('\\', '/');
                        if (_activeFiles.TryGetValue(localFile, out AssetFile file))
                        {
                            DateTime lastModified = File.GetLastWriteTime(b.FullPath);
                            if (file.LastModifiedDate == lastModified)
                            {
                                return;
                            }

                            _activeFiles[localFile] = new AssetFile(lastModified);
                        }
                        else
                        {
                            PushFileAddedEvent(localFile);

                            _rememberedFiles.Add(localFile);
                            _activeFiles.TryAdd(localFile, new AssetFile(File.GetLastWriteTime(localFile)));

                            return;
                        }

                        PushFileChangedEvent(localFile);
                    }
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "Failed to handle callback for file change");
                }
            };
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _watcher.Dispose();

                    FlushFilesystemData();

                    foreach (var kvp in _directoryTree)
                    {
                        kvp.Value.Subdirectories.Dispose();
                        kvp.Value.Files.Dispose();
                    }

                    _directoryTree.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ResetInternalState()
        {
            _activeFiles.Clear();
            _rememberedFiles.Clear();
            _eventQueue.Clear();

            lock (_directoryTree)
            {
                foreach (var kvp in _directoryTree)
                {
                    kvp.Value.Subdirectories.Dispose();
                    kvp.Value.Files.Dispose();
                }

                _directoryTree.Clear();
            }

            SearchInitialDirectory(_directory);
            FlushFilesystemData();
        }

        internal void ClearEventQueue()
        {
            _eventQueue.Clear();
        }

        internal bool PollEvent(out FilesystemEvent @event) => _eventQueue.TryDequeue(out @event);

        internal bool IsFileUpToDate(string path)
        {
            if (_activeFiles.TryGetValue(path, out AssetFile value))
            {
                return value.LastModifiedDate == File.GetLastWriteTime(path);
            }

            return false;
        }

        private void DeserializeDataFile(string dataFilePath)
        {
            string source = File.ReadAllText(dataFilePath);

            int i = 0;
            while (i < source.Length)
            {
                int end = source.IndexOf(';', i);

                string localFilePath = source.Substring(i, end - i);

                i = end + 1;
                end = source.IndexOf('\n', end);

                string src = source.Substring(i, end - i);
                DateTime lastModifiedData = new DateTime((long)ulong.Parse(src));

                string rootFilePath = Path.Combine(_subFilesystem.AbsolutePath, localFilePath);

                if (!File.Exists(rootFilePath))
                {
                    PushFileRemovedEvent(localFilePath);
                }
                else
                {
                    DateTime newLastModified = File.GetLastWriteTime(rootFilePath);
                    if (newLastModified != lastModifiedData)
                    {
                        PushFileChangedEvent(localFilePath);
                        lastModifiedData = newLastModified;
                    }

                    _activeFiles.TryAdd(localFilePath, new AssetFile(lastModifiedData));
                    _rememberedFiles.Add(localFilePath);
                }

                i = end + 1;
            }
        }

        private void SearchInitialDirectory(string searchPath)
        {
            bool hasChangeOccured = false;

            AddLocalDirectory(null, searchPath, Path.GetFileName(searchPath));

            int length = _rootDirectory.Length + 1;
            foreach (string directory in Directory.EnumerateDirectories(searchPath, "*.*", SearchOption.AllDirectories))
            {
                string localDirectory = directory.Substring(length).Replace('\\', '/');
                AddLocalDirectory(null, directory, localDirectory);
            }

            foreach (string file in Directory.EnumerateFiles(searchPath, "*.*", SearchOption.AllDirectories))
            {
                string localPath = file.Substring(length).Replace('\\', '/');
                AssetId id = _pipeline.Identifier.GetOrRegisterAsset(localPath);

                bool needsNew = _activeFiles.TryAdd(localPath, new AssetFile(File.GetLastWriteTime(file)));
                needsNew = _rememberedFiles.Add(localPath) || needsNew;

                if (needsNew)
                {
                    PushFileAddedEvent(localPath);
                    hasChangeOccured = true;
                }

                string? directory = Path.GetDirectoryName(file);
                if (directory != null)
                {
                    string localDirectory = directory.Substring(length).Replace('\\', '/');
                    AddLocalDirectory(localPath, directory, localDirectory);
                }
            }

            if (hasChangeOccured)
            {
                FlushFilesystemData();
            }
        }

        private void AddLocalDirectory(string? localPath, string directory, string localDirectory)
        {
            lock (_directoryTree)
            {
                if (!_directoryTree.TryGetValue(localDirectory, out AssetDirectory value))
                {
                    value = new AssetDirectory(new PooledList<string>(), new PooledList<string>());
                    foreach (string subDir in Directory.EnumerateDirectories(directory, "*.*"))
                    {
                        value.Subdirectories.Add(subDir.Substring(localDirectory.Length + _rootDirectory.Length + 2).Replace('\\', '/'));
                    }

                    _directoryTree.Add(localDirectory, value);
                }

                if (localPath != null)
                    value.Files.Add(localPath.Substring(localDirectory.Length + 1));

                string? parentDirectory = Path.GetDirectoryName(localDirectory);
                if (parentDirectory != null && _directoryTree.TryGetValue(parentDirectory.Replace('\\', '/'), out value))
                {
                    string nameOnly = localDirectory.Substring(parentDirectory.Length + 1);
                    if (!value.Subdirectories.Contains(nameOnly))
                        value.Subdirectories.Add(nameOnly);
                }
            }
        }

        private void RemoveFromLocalDirectory(string localFile, string localDirectory)
        {
            lock (_directoryTree)
            {
                if (_directoryTree.TryGetValue(localDirectory, out AssetDirectory value))
                {
                    value.Files.Remove(localFile.Substring(localDirectory.Length + 1));
                }
            }
        }

        private void RenameLocalFileInDirectory(string oldLocalFile, string newLocalFile, string localDirectory)
        {
            lock (_directoryTree)
            {
                if (_directoryTree.TryGetValue(localDirectory, out AssetDirectory value))
                {
                    oldLocalFile = oldLocalFile.Substring(localDirectory.Length + 1);
                    if (value.Files.Remove(oldLocalFile))
                    {
                        newLocalFile = newLocalFile.Substring(localDirectory.Length + 1);
                        value.Files.Add(newLocalFile);
                    }
                }
            }
        }

        private void RemoveLocalDirectory(string localDirectory, bool recursive)
        {
            lock (_directoryTree)
            {
                string? parentDirectory = Path.GetDirectoryName(localDirectory);
                if (parentDirectory != null && _directoryTree.TryGetValue(parentDirectory.Replace('\\', '/'), out AssetDirectory value))
                {
                    value.Subdirectories.Remove(localDirectory.Substring(parentDirectory.Length + 1));
                }

                if (recursive)
                {
                    RecursiveTreeDeletion(localDirectory);

                    void RecursiveTreeDeletion(string newLocalDir)
                    {
                        if (_directoryTree.TryGetValue(newLocalDir, out AssetDirectory subDir))
                        {
                            for (int i = 0; i < subDir.Subdirectories.Count; i++)
                            {
                                RecursiveTreeDeletion($"{newLocalDir}/{subDir.Subdirectories[i]}");
                            }

                            subDir.Subdirectories.Dispose();
                            subDir.Files.Dispose();
                        }

                        _directoryTree.Remove(newLocalDir);
                    }
                }
                else
                {
                    if (_directoryTree.TryGetValue(localDirectory, out value))
                    {
                        value.Subdirectories.Dispose();
                        value.Files.Dispose();
                    }

                    _directoryTree.Remove(localDirectory);
                }
            }
        }

        internal void RenameLocalDirectory(string oldLocalDirectory, string newLocalDirectory)
        {
            lock (_directoryTree)
            {
                string? parentDirectory = Path.GetDirectoryName(oldLocalDirectory);
                if (parentDirectory != null && _directoryTree.TryGetValue(parentDirectory.Replace('\\', '/'), out AssetDirectory value))
                {
                    if (value.Subdirectories.Remove(oldLocalDirectory.Substring(parentDirectory.Length + 1)))
                        value.Subdirectories.Add(newLocalDirectory.Substring(parentDirectory.Length + 1));
                }

                if (_directoryTree.TryGetValue(oldLocalDirectory, out AssetDirectory directory))
                {
                    for (int i = 0; i < directory.Files.Count; i++)
                    {
                        string fullLocalFile = $"{oldLocalDirectory}/{directory.Files[i]}";
                        if (_rememberedFiles.Contains(fullLocalFile))
                        {
                            string newFilePath = $"{newLocalDirectory}/{directory.Files[i]}";

                            _rememberedFiles.Remove(fullLocalFile);
                            _rememberedFiles.Add(newFilePath);

                            PushFileRenamedEvent(fullLocalFile, newFilePath);
                        }
                        else
                            EdLog.Assets.Warning("Unexpected missing file within rename: {f} (to: {n})", fullLocalFile, $"{newLocalDirectory}/{directory.Files[i]}");
                    }

                    for (int i = 0; i < directory.Subdirectories.Count; i++)
                    {
                        RecursiveRename($"{oldLocalDirectory}/{directory.Subdirectories[i]}");
                    }

                    _directoryTree.Add(newLocalDirectory, directory);
                    _directoryTree.Remove(oldLocalDirectory);
                }

                void RecursiveRename(string localDir)
                {
                    if (_directoryTree.TryGetValue(localDir, out AssetDirectory subDir))
                    {
                        string newLocalDir = newLocalDirectory + localDir.Remove(0, oldLocalDirectory.Length);
                        for (int i = 0; i < subDir.Files.Count; i++)
                        {
                            string fullLocalFile = $"{localDir}/{directory.Files[i]}";
                            if (_rememberedFiles.Contains(fullLocalFile))
                            {
                                string newFilePath = $"{newLocalDir}/{directory.Files[i]}";

                                _rememberedFiles.Remove(fullLocalFile);
                                _rememberedFiles.Add(newFilePath);

                                PushFileRenamedEvent(fullLocalFile, newFilePath);
                            }
                            else
                                EdLog.Assets.Warning("Unexpected missing file within rename: {f} (to: {n})", fullLocalFile, $"{newLocalDir}/{directory.Files[i]}");
                        }

                        for (int i = 0; i < subDir.Subdirectories.Count; i++)
                        {
                            RecursiveRename($"{localDir}/{directory.Subdirectories[i]}");
                        }

                        _directoryTree.Add(newLocalDir, directory);
                        _directoryTree.Remove(localDir);
                    }
                }
            }
        }

        private void FlushFilesystemData()
        {
            int length = Editor.GlobalSingleton.ProjectPath.Length;

            StringBuilder sb = new StringBuilder();
            foreach (var kvp in _activeFiles)
            {
                sb.Append(kvp.Key);
                sb.Append(';');
                sb.Append(((ulong)kvp.Value.LastModifiedDate.Ticks).ToString());
                sb.Append('\n');
            }

            File.WriteAllText(FilesystemDataFile, sb.ToString());
        }

        private void PushFileAddedEvent(string localFilePath)
            => _eventQueue.Enqueue(new FilesystemEvent(FilesystemEventType.FileAdded, localFilePath, null));

        private void PushFileRemovedEvent(string localFilePath)
            => _eventQueue.Enqueue(new FilesystemEvent(FilesystemEventType.FileRemoved, localFilePath, null));

        private void PushFileChangedEvent(string localFilePath)
            => _eventQueue.Enqueue(new FilesystemEvent(FilesystemEventType.FileChanged, localFilePath, null));

        private void PushFileRenamedEvent(string localFilePath, string newLocalFilePath)
            => _eventQueue.Enqueue(new FilesystemEvent(FilesystemEventType.FileRenamed, localFilePath, newLocalFilePath));

        internal bool GetDirectory(string localDirectoryPath, out AssetDirectory directory)
            => _directoryTree.TryGetValue(localDirectoryPath, out directory);

        private void WatcherThreadProc()
        {
            Queue<string> activeQueue = new Queue<string>();
            HashSet<string> newExistingFiles = new HashSet<string>();

            string projectPath = Editor.GlobalSingleton.ProjectPath;

            while (true)
            {
                activeQueue.Enqueue(EditorFilepaths.ContentPath);
                activeQueue.Enqueue(EditorFilepaths.SourcePath);

                while (activeQueue.TryDequeue(out string? result))
                {
                    try
                    {
                        //find files
                        foreach (string file in Directory.EnumerateFiles(result, "*.*", SearchOption.TopDirectoryOnly))
                        {
                            string localFile = file.Substring(projectPath.Length + 1);

                            _activeFiles.AddOrUpdate(localFile, (x) =>
                            {
                                PushFileAddedEvent(localFile);
                                return new AssetFile(File.GetLastWriteTime(file));
                            }, (x, y) =>
                            {
                                DateTime lastModifiedDate = File.GetLastWriteTime(file);
                                if (y.LastModifiedDate != lastModifiedDate)
                                {
                                    PushFileChangedEvent(localFile);
                                    return new AssetFile(lastModifiedDate);
                                }

                                return y;
                            });
                        }

                        //find subdirs
                        foreach (string dir in Directory.EnumerateDirectories(result))
                        {
                            activeQueue.Enqueue(dir);
                        }

                        Thread.Yield();
                    }
                    catch (Exception ex)
                    {
                        //bad
                    }
                }

                Thread.Sleep(2000);
            }
        }

        public string FilesystemDataFile => Path.Combine(EditorFilepaths.LibraryIntermediatePath, $"{(uint)_directory.GetDjb2HashCode()}.fsdat");

        internal object LockableTree => _directoryTree;
        internal ProjectSubFilesystem SubFilesystem => _subFilesystem;
    }

    internal record struct AssetFile(DateTime LastModifiedDate);
    internal record struct AssetDirectory(PooledList<string> Subdirectories, PooledList<string> Files);
    internal record struct FilesystemEvent(FilesystemEventType Type, string LocalPath, string? NewLocalPath);

    internal enum FilesystemEventType : byte
    {
        Unknown = 0,

        FileAdded,
        FileRemoved,
        FileChanged,
        FileRenamed
    }
}
