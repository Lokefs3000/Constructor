using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Assets.Filesystem;
using PrimaryEditor.Core;

namespace PrimaryEditor.Assets
{
    public sealed class FilesystemManager : IDisposable
    {
        private readonly AssetPipeline _pipeline;

        private List<ContentFilesystem> _filesystems;

        private bool _disposedValue;

        internal FilesystemManager(AssetPipeline pipeline)
        {
            _pipeline = pipeline;

            _filesystems = new List<ContentFilesystem>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (ContentFilesystem filesystem in _filesystems)
                    {
                        filesystem.Dispose();
                    }

                    _filesystems.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Mount(string directory, string namespaceKey)
        {
            if (directory.Contains('\\'))
                directory = directory.Replace('\\', '/');

            EdLog.Assets.Information("Mounting filesystem at '{p}' with namespace key '{nk}'", directory, namespaceKey);

            ContentFilesystem filesystem = new ContentFilesystem(_pipeline, directory, namespaceKey);
            _filesystems.Add(filesystem);
        }

        public static string? GetLocalPath(string fullPath)
        {
            FilesystemManager self = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
            foreach (ContentFilesystem filesystem in self._filesystems)
            {
                string? localPath = filesystem.GetLocalPath(fullPath);
                if (localPath != null)
                    return localPath;
            }

            return null;
        }

        public static string? GetFullPath(string localPath)
        {
            FilesystemManager self = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
            foreach (ContentFilesystem filesystem in self._filesystems)
            {
                string? fullPath = filesystem.GetFullPath(localPath);
                if (fullPath != null)
                    return fullPath;
            }

            return null;
        }
    }
}
