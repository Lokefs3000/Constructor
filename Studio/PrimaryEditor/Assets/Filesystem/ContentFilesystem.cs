using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using Primary.Console;

namespace PrimaryEditor.Assets.Filesystem
{
    public sealed class ContentFilesystem : IDisposable
    {
        private readonly AssetPipeline _pipeline;

        private readonly string _workingDirectory;
        private readonly string _namespaceKey;

        private readonly FileSystemWatcher _watcher;

        private DynamicCircularBuffer<TimedFileEvent> _timedFileEvents;

        private bool _disposedValue;

        internal ContentFilesystem(AssetPipeline pipeline, string workingDirectory, string namespaceKey)
        {
            _pipeline = pipeline;

            _workingDirectory = workingDirectory;
            _namespaceKey = namespaceKey;

            _watcher = new FileSystemWatcher(workingDirectory)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                InternalBufferSize = ushort.MaxValue,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            };

            _timedFileEvents = new DynamicCircularBuffer<TimedFileEvent>(16);

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

        public string? GetLocalPath(string fullPath)
        {
            // full path is already local
            if (fullPath.StartsWith(_namespaceKey))
                return fullPath.Contains('\\') ? fullPath.Replace('\\', '/') : fullPath;

            if (fullPath.Length <= _workingDirectory.Length)
                return null;

            if (fullPath.Contains('\\'))
                fullPath = fullPath.Replace('\\', '/');

            return $"{_namespaceKey}/{fullPath[(_workingDirectory.Length + 1)..]}";
        }

        public string? GetFullPath(string fullPath)
        {
            if (Path.IsPathFullyQualified(fullPath))
                return fullPath;

            if (!fullPath.StartsWith(_namespaceKey))
                return null;

            return Path.Combine(_workingDirectory, fullPath[_namespaceKey.Length..]);
        }

        private bool IsNewFileActuallyUnique(string localFilePath, string newLocalFilePath)
        {
            AssetId sourceId = _pipeline.AssetRegistry.RetriveIdForPath(localFilePath);
            if (!sourceId.IsInvalid)
            {
                if (_pipeline.PhysicalFileRegistry.TryGetFileInfo(sourceId, out PhysicalFileInfo info))
                {
                    string newFullPath = GetFullPath(newLocalFilePath)!;
                    using Stream? stream = FileUtility.TryWaitOpenNoThrow(newFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, maxTries: 4, timeoutMs: 50);
                    if (stream == null)
                    {
                        // show popup to try and validate what happened
                        throw new Exception();
                    }

                    if (info.FileSize != stream.Length)
                        return true;

                    FileChecksum checksum = _pipeline.PhysicalFileRegistry.CreateChecksumFrom(stream);
                    if (!checksum.Equals(info.Checksum))
                    {
                        _pipeline.PhysicalFileRegistry.TryRegisterFile(sourceId, File.GetLastWriteTime(newFullPath), stream.Length, checksum);
                        return true;
                    }

                    return false;
                }
            }

            return true;
        }

        private void OnFileChangedCallback(object sender, FileSystemEventArgs e)
        {

        }

        private void OnFileRenamedCallback(object sender, FileSystemEventArgs e)
        {

        }

        private void OnFileCreatedCallback(object sender, FileSystemEventArgs e)
        {
            FileEvent eventData = new FileEvent(GetLocalPath(e.FullPath)!, null, FileEventType.Created);
            for (int i = 0; i < _timedFileEvents.Count; i++)
            {
                TimedFileEvent fileEvent = _timedFileEvents[i];
                if (fileEvent.EventData.EventType == FileEventType.Created)
                {
                    if (!IsNewFileActuallyUnique(eventData.LocalFilePath, fileEvent.EventData.LocalFilePath))
                    {
                        _timedFileEvents[i] = new TimedFileEvent(fileEvent.Timeout, new FileEvent(fileEvent.EventData.LocalFilePath, eventData.LocalFilePath, FileEventType.Moved));
                        return;
                    }
                }
            }

            _timedFileEvents.PushBack(new TimedFileEvent(DateTime.Now + TimeoutDuration, eventData));
        }

        private void OnFileDeletedCallback(object sender, FileSystemEventArgs e)
        {
            FileEvent eventData = new FileEvent(GetLocalPath(e.FullPath)!, null, FileEventType.Deleted);
            for (int i = 0; i < _timedFileEvents.Count; i++)
            {
                TimedFileEvent fileEvent = _timedFileEvents[i];
                if (fileEvent.EventData.EventType == FileEventType.Created)
                {
                    if (!IsNewFileActuallyUnique(eventData.LocalFilePath, fileEvent.EventData.LocalFilePath))
                    {
                        _timedFileEvents[i] = new TimedFileEvent(fileEvent.Timeout, new FileEvent(eventData.LocalFilePath, fileEvent.EventData.LocalFilePath, FileEventType.Moved));
                        return;
                    }
                }
            }

            _timedFileEvents.PushBack(new TimedFileEvent(DateTime.Now + TimeoutDuration, eventData));
        }

        private readonly record struct TimedFileEvent(DateTime Timeout, FileEvent EventData);

        public static readonly CVar<TimeSpan> TimeoutDuration = new CVar<TimeSpan>(TimeSpan.FromSeconds(0.1));
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
