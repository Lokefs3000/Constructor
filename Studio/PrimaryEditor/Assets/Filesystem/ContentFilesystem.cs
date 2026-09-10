using System.Collections.Concurrent;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Console;

namespace PrimaryEditor.Assets.Filesystem
{
    public sealed class ContentFilesystem : BaseFilesystem, IDisposable
    {
        private readonly AssetPipeline _pipeline;

        private readonly string _workingDirectory;
        private readonly string _namespaceKey;

        private readonly FileSystemWatcher _watcher;
        private readonly FilesystemStructure _structure;

        private Lock _lock;
        private bool _isLockCurrentlyActive;

        private DateTime _lastEventTime;
        private List<FileEvent> _currentPendingFileEvents;

        private ConcurrentDictionary<string, byte> _directories;

        private bool _disposedValue;

        internal ContentFilesystem(AssetPipeline pipeline, string workingDirectory, string namespaceKey)
        {
            if (Path.DirectorySeparatorChar == '\\')
                workingDirectory = workingDirectory.Replace('/', '\\');
            else
                workingDirectory = workingDirectory.Replace('\\', '/');

            _pipeline = pipeline;

            _workingDirectory = workingDirectory;
            _namespaceKey = namespaceKey;

            _watcher = new FileSystemWatcher(workingDirectory)
            {
                IncludeSubdirectories = true,
                InternalBufferSize = ushort.MaxValue,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };
            _structure = new FilesystemStructure(namespaceKey);

            _lock = new Lock();
            _isLockCurrentlyActive = false;

            _lastEventTime = DateTime.MinValue;
            _currentPendingFileEvents = new List<FileEvent>();

            _directories = new ConcurrentDictionary<string, byte>();

            foreach (string directoryName in Directory.GetDirectories(workingDirectory))
            {
                if (TryGetLocalPath(directoryName, out string? localPath))
                {
                    _directories.TryAdd(localPath, 0);
                }
                else
                {
                    EdLog.Assets.Error("Failed to get local path for directory '{dir}'", directoryName);
                }
            }

            _watcher.Changed += OnFileChangedCallback;
            _watcher.Renamed += OnFileRenamedCallback;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _watcher.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override bool Exists(string localPath)
        {
            string fullPath = $"{_workingDirectory}{localPath.AsSpan()[_namespaceKey.Length..]}";
            return File.Exists(fullPath);
        }

        public override string? ReadAllText(string localPath)
        {
            string fullPath = $"{_workingDirectory}{localPath.AsSpan()[_namespaceKey.Length..]}";

            IOException? exception = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return File.ReadAllText(fullPath);
                }
                catch (FileNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (DirectoryNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (IOException ex)
                {
                    exception = ex;
                }

                Thread.Sleep(50);
            }

            EdLog.Assets.Error("Failed to read '{p}' because '{msg}'", fullPath, exception?.Message);
            return null;
        }

        public override byte[]? ReadAllBytes(string localPath)
        {
            string fullPath = $"{_workingDirectory}{localPath.AsSpan()[_namespaceKey.Length..]}";

            IOException? exception = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return File.ReadAllBytes(fullPath);
                }
                catch (FileNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (DirectoryNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (IOException ex)
                {
                    exception = ex;
                }

                Thread.Sleep(50);
            }

            EdLog.Assets.Error("Failed to read '{p}' because '{msg}'", fullPath, exception?.Message);
            return null;
        }

        public override Stream? OpenStream(string localPath)
        {
            string fullPath = $"{_workingDirectory}{localPath.AsSpan()[_namespaceKey.Length..]}";

            IOException? exception = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return File.OpenRead(fullPath);
                }
                catch (FileNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (DirectoryNotFoundException)
                {
                    EdLog.Assets.Error("Failed to read '{p}' because the file does not exist!", fullPath);
                    return null;
                }
                catch (IOException ex)
                {
                    exception = ex;
                }

                Thread.Sleep(50);
            }

            EdLog.Assets.Error("Failed to read '{p}' because '{msg}'", fullPath, exception?.Message);
            return null;
        }

        private void TryPushNewFileEvent(string fullPath, string? newFullPath, FileEventType eventType)
        {
            if (!TryGetLocalPath(fullPath, out string? localPath))
            {
                EdLog.Assets.Warning("Failed to push filesystem event '{ev}' because no local path was found for '{p}'", eventType, fullPath);
                return;
            }

            string? newLocalPath = null;
            if (newFullPath != null)
            {
                if (!TryGetLocalPath(newFullPath, out newLocalPath))
                {
                    EdLog.Assets.Warning("Failed to push filesystem events '{ev}' because no local path was found for '{p}'", eventType, newFullPath);
                    return;
                }
            }

            bool isDirectory = Directory.Exists(newFullPath ?? fullPath);
            if (isDirectory)
            {
                if (eventType == FileEventType.Renamed && newLocalPath != null)
                {
                    if (_directories.TryRemove(localPath, out _))
                        _directories.TryAdd(newLocalPath, 0);
                }
                else
                {
                    _directories.TryAdd(localPath, 0);
                }
            }
            else if (eventType == FileEventType.Deleted)
            {
                if (_directories.TryRemove(localPath, out _))
                    isDirectory = true;
            }

            using (_lock.EnterScope())
            {
                _isLockCurrentlyActive = true;

                _lastEventTime = DateTime.Now;

                bool shouldPushNewEvent = true;
                for (int i = _currentPendingFileEvents.Count - 1; i >= 0; i--)
                {
                    FileEvent currentEventData = _currentPendingFileEvents[i];
                    if (currentEventData.LocalFilePath == localPath)
                    {
                        if (currentEventData.EventType == eventType)
                        {
                            shouldPushNewEvent = false;
                            break;
                        }

                        switch (eventType)
                        {
                            case FileEventType.Created:
                                {
                                    if (currentEventData.EventType == FileEventType.Deleted)
                                    {
                                        _currentPendingFileEvents[i] = new FileEvent(
                                            currentEventData.LocalFilePath,
                                            localPath,
                                            FileEventType.Moved,
                                            isDirectory);

                                        shouldPushNewEvent = false;
                                    }
                                    else if (currentEventData.EventType == FileEventType.Moved)
                                    {
                                        shouldPushNewEvent = false;
                                    }

                                    break;
                                }
                            case FileEventType.Deleted:
                                {
                                    if (currentEventData.EventType == FileEventType.Created)
                                    {
                                        _currentPendingFileEvents[i] = new FileEvent(
                                            localPath,
                                            currentEventData.LocalFilePath,
                                            FileEventType.Moved,
                                            isDirectory);

                                        shouldPushNewEvent = false;
                                    }
                                    else if (currentEventData.EventType == FileEventType.Moved)
                                    {
                                        shouldPushNewEvent = false;
                                    }

                                    break;
                                }
                            case FileEventType.Renamed:
                                {
                                    if (currentEventData.LocalFilePath == newLocalPath && currentEventData.EventType == FileEventType.Moved)
                                    {
                                        shouldPushNewEvent = false;
                                    }

                                    break;
                                }
                        }

                        if (!shouldPushNewEvent)
                            break;
                    }
                }

                if (shouldPushNewEvent)
                {
                    _currentPendingFileEvents.Add(new FileEvent(localPath, newLocalPath, eventType, isDirectory));
                }

                _isLockCurrentlyActive = false;
            }
        }

        private void OnFileChangedCallback(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    {
                        TryPushNewFileEvent(e.FullPath, null, FileEventType.Created);
                        break;
                    }
                case WatcherChangeTypes.Deleted:
                    {
                        TryPushNewFileEvent(e.FullPath, null, FileEventType.Deleted);
                        break;
                    }
                case WatcherChangeTypes.Changed:
                    {
                        TryPushNewFileEvent(e.FullPath, null, FileEventType.Changed);
                        break;
                    }
            }
        }

        private void OnFileRenamedCallback(object sender, RenamedEventArgs e)
        {
            if (e.FullPath.EndsWith(".assetdat") || e.OldFullPath.EndsWith(".assetdat"))
                return;

            TryPushNewFileEvent(e.OldFullPath, e.FullPath, FileEventType.Renamed);
        }

        internal void FlushPendingFileEvents(ref RentedList<FileEvent> fileEvents)
        {
            if (_currentPendingFileEvents.Count > 0)
            {
                fileEvents.AddRange(_currentPendingFileEvents.AsSpan());
                _currentPendingFileEvents.Clear();
            }
        }

        internal bool TryEnterLock()
        {
            if (_lock.TryEnter(0))
            {
                _isLockCurrentlyActive = true;
                return true;
            }

            return false;
        }

        internal void ExitLock()
        {
            _isLockCurrentlyActive = false;
            _lock.Exit();
        }

        public override string WorkingDirectory => _workingDirectory;
        public override string NamespaceKey => _namespaceKey;

        public FilesystemStructure Structure => _structure;

        public bool AreFileUpdatesAvailable => _currentPendingFileEvents.Count > 0 && !_isLockCurrentlyActive;
        public bool IsWithinTimeoutPeriod => DateTime.Now < _lastEventTime + TimeoutDuration;

        public static readonly ConsoleVar<TimeSpan> TimeoutDuration = new ConsoleVar<TimeSpan>(TimeSpan.FromSeconds(0.2));
    }

    public readonly record struct FileEvent(string LocalFilePath, string? NewLocalFilePath, FileEventType EventType, bool IsDirectory);

    [Flags]
    public enum FileEventType : byte
    {
        Created = 1 << 0,
        Deleted = 1 << 1,
        Renamed = 1 << 2,
        Changed = 1 << 3,
        Moved = 1 << 4
    }
}
