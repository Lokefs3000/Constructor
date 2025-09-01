using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Editor.Assets
{
    internal sealed class AssetFilesystemWatcher : IDisposable
    {
        private string _directory;

        private ConcurrentDictionary<string, AssetFile> _activeFiles;
        private HashSet<string> _rememberedFiles;

        private Dictionary<string, AssetDirectory> _directoryTree;

        private ConcurrentQueue<FilesystemEvent> _eventQueue;

        private FileSystemWatcher _watcher;

        private bool _disposedValue;

        internal AssetFilesystemWatcher(string directory)
        {
            _directory = directory;

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
                FileAttributes attributes = File.GetAttributes(b.FullPath);
                if (FlagUtility.HasFlag(attributes, FileAttributes.Directory))
                {
                    string localDirectory = b.FullPath.Substring(Editor.GlobalSingleton.ProjectPath.Length).Replace('\\', '/');
                    AddLocalDirectory(null, b.FullPath, localDirectory);
                }
                else
                {
                    string localFile = b.FullPath.Substring(Editor.GlobalSingleton.ProjectPath.Length).Replace('\\', '/');
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
                        string localDirectory = directory.Substring(Editor.GlobalSingleton.ProjectPath.Length).Replace('\\', '/');
                        AddLocalDirectory(localFile, directory, localDirectory);
                    }
                }
            };

            _watcher.Deleted += (a, b) =>
            {
                string localFile = b.FullPath.Substring(Editor.GlobalSingleton.ProjectPath.Length).Replace('\\', '/');

                if (_directoryTree.ContainsKey(localFile))
                {
                    RemoveLocalDirectory(b.FullPath.Substring(Editor.GlobalSingleton.ProjectPath.Length).Replace('\\', '/'), true);
                }
                else if (_rememberedFiles.Contains(localFile))
                {
                    PushFileRemovedEvent(localFile);

                    _rememberedFiles.Remove(localFile);
                    _activeFiles.Remove(localFile, out _);

                    string? directory = Path.GetDirectoryName(b.FullPath);
                    if (directory != null)
                    {
                        RemoveFromLocalDirectory(localFile, directory.Substring(Editor.GlobalSingleton.ProjectPath.Length).Replace('\\', '/'));
                    }
                }
            };

            _watcher.Changed += (a, b) =>
            {
                FileAttributes attributes = File.GetAttributes(b.FullPath);
                if (!FlagUtility.HasFlag(attributes, FileAttributes.Directory))
                {
                    string localFile = b.FullPath.Substring(Editor.GlobalSingleton.ProjectPath.Length).Replace('\\', '/');
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool PollEvent(out FilesystemEvent @event) => _eventQueue.TryDequeue(out @event);

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

                string rootFilePath = Path.Combine(Editor.GlobalSingleton.ProjectPath, localFilePath);

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

            int length = Editor.GlobalSingleton.ProjectPath.Length;
            foreach (string directory in Directory.EnumerateDirectories(searchPath, "*.*", SearchOption.AllDirectories))
            {
                string localDirectory = directory.Substring(length).Replace('\\', '/');
                AddLocalDirectory(null, directory, localDirectory);
            }

            foreach (string file in Directory.EnumerateFiles(searchPath, "*.*", SearchOption.AllDirectories))
            {
                string localPath = file.Substring(length).Replace('\\', '/');
                if (_activeFiles.TryAdd(localPath, new AssetFile(File.GetLastWriteTime(file))) || _rememberedFiles.Add(localPath))
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
                        value.Subdirectories.Add(subDir.Substring(localDirectory.Length + Editor.GlobalSingleton.ProjectPath.Length + 1).Replace('\\', '/'));
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
                    value.Files.Remove(localFile);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushFileAddedEvent(string localFilePath)
            => _eventQueue.Enqueue(new FilesystemEvent(FilesystemEventType.FileAdded, localFilePath));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushFileRemovedEvent(string localFilePath)
            => _eventQueue.Enqueue(new FilesystemEvent(FilesystemEventType.FileRemoved, localFilePath));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushFileChangedEvent(string localFilePath)
            => _eventQueue.Enqueue(new FilesystemEvent(FilesystemEventType.FileChanged, localFilePath));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private string FilesystemDataFile => Path.Combine(EditorFilepaths.LibraryPath, $"{(uint)_directory.GetDjb2HashCode()}.fsdat");

        internal object LockableTree => _directoryTree;
    }

    internal record struct AssetFile(DateTime LastModifiedDate);
    internal record struct AssetDirectory(PooledList<string> Subdirectories, PooledList<string> Files);
    internal record struct FilesystemEvent(FilesystemEventType Type, string LocalPath);

    internal enum FilesystemEventType : byte
    {
        Unknown = 0,

        FileAdded,
        FileRemoved,
        FileChanged
    }
}
