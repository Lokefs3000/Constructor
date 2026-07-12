using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using Primary.Console;
using TerraFX.Interop.Windows;

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

        private DateTime _oldestEvent;
        private DynamicCircularBuffer<TimedFileEvent> _timedFileEvents;

        private ConcurrentDictionary<string, object?> _directories;

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

            _oldestEvent = DateTime.MinValue;
            _timedFileEvents = new DynamicCircularBuffer<TimedFileEvent>(16);

            _directories = new ConcurrentDictionary<string, object?>();

            foreach (string directoryName in Directory.GetDirectories(workingDirectory))
            {
                if (TryGetLocalPath(directoryName, out string? localPath))
                {
                    _directories.TryAdd(localPath, null);
                }
                else
                {
                    EdLog.Assets.Error("Failed to get local path for directory '{dir}'", directoryName);
                }
            }

            _watcher.Changed += OnFileChangedCallback;
            _watcher.Renamed += OnFileRenamedCallback;
            _watcher.Created += OnFileCreatedCallback;
            _watcher.Deleted += OnFileDeletedCallback;
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

        private void OnFileChangedCallback(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                return;
            }

            if (!TryGetLocalPath(e.FullPath, out string? localPath))
            {
                EdLog.Assets.Warning("Failed to handle file changed callback because no local path was found for '{p}'", e.FullPath);
                return;
            }

            using var lockScope = _lock.EnterScope();
            _timedFileEvents.PushBack(new TimedFileEvent(DateTime.Now + TimeoutDuration, new FileEvent(localPath, null, FileEventType.Changed)));
        }

        private void OnFileRenamedCallback(object sender, RenamedEventArgs e)
        {
            if (e.FullPath.EndsWith(".assetdat"))
                return;

            if (!TryGetLocalPath(e.FullPath, out string? localPath))
            {
                EdLog.Assets.Warning("Failed to handle file renamed callback because no local path was found for '{p}'", e.FullPath);
                return;
            }

            if (!TryGetLocalPath(e.OldFullPath, out string? oldLocalPath))
            {
                EdLog.Assets.Warning("Failed to handle file renamed callback because no local path was found for '{p}'", e.OldFullPath);
                return;
            }

            using var lockScope = _lock.EnterScope();
            _timedFileEvents.PushBack(new TimedFileEvent(DateTime.MinValue, new FileEvent(oldLocalPath, localPath, FileEventType.Renamed)));
        }

        private void OnFileCreatedCallback(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.EndsWith(".assetdat"))
                return;

            if (!TryGetLocalPath(e.FullPath, out string? localPath))
            {
                EdLog.Assets.Warning("Failed to handle created file callback because no local path was found for '{p}'", e.FullPath);
                return;
            }

            if (Directory.Exists(e.FullPath))
            {
                _directories.TryAdd(localPath, null);
                return;
            }

            FileEvent eventData = new FileEvent(localPath, null, FileEventType.Created);
            DateTime now = DateTime.Now;

            using var lockScope = _lock.EnterScope();
            _timedFileEvents.PushBack(new TimedFileEvent(DateTime.Now + TimeoutDuration, eventData));
        }

        private void OnFileDeletedCallback(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.EndsWith(".assetdat"))
                return;

            if (!TryGetLocalPath(e.FullPath, out string? localPath))
            {
                EdLog.Assets.Warning("Failed to handle deleted file callback because no local path was found for '{p}'", e.FullPath);
                return;
            }

            if (_directories.TryRemove(localPath, out _))
            {
                return;
            }

            FileEvent eventData = new FileEvent(localPath, null, FileEventType.Deleted);
            DateTime now = DateTime.Now;

            using var lockScope = _lock.EnterScope();
            _timedFileEvents.PushBack(new TimedFileEvent(now + TimeoutDuration, eventData));
        }

        internal bool TryPopFileEvent(out FileEvent fileEvent)
        {
            if (_timedFileEvents.TryGetFront(out TimedFileEvent timedFileEvent))
            {
                if (timedFileEvent.Timeout < DateTime.Now)
                {
                    fileEvent = timedFileEvent.EventData;

                    _timedFileEvents.PopFront();
                    return true;
                }

                _oldestEvent = timedFileEvent.Timeout;
            }

            fileEvent = default;
            return false;
        }

        internal bool TryEnterLock()
        {
            return _lock.TryEnter(0);
        }

        internal void ExitLock()
        {
            _lock.Exit();
        }

        public override string WorkingDirectory => _workingDirectory;
        public override string NamespaceKey => _namespaceKey;

        public FilesystemStructure Structure => _structure;

        public bool AreFileUpdatesAvailable => !_timedFileEvents.IsEmpty && _oldestEvent < DateTime.Now;

        private readonly record struct TimedFileEvent(DateTime Timeout, FileEvent EventData);

        public static readonly ConsoleVar<TimeSpan> TimeoutDuration = new ConsoleVar<TimeSpan>(TimeSpan.FromSeconds(0.2));
    }

    public readonly record struct FileEvent(string LocalFilePath, string? NewLocalFilePath, FileEventType EventType);

    public enum FileEventType : byte
    {
        Created = 0,
        Deleted,
        Renamed,
        Changed,
        Moved
    }
}
