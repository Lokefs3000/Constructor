using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Assets.Filesystem;
using PrimaryEditor.Assets.Serialization;
using PrimaryEditor.Core;
using PrimaryEditor.Project;
using TerraFX.Interop.Windows;

namespace PrimaryEditor.Assets
{
    public sealed class FilesystemManager : IDisposable, ISubFilesystem
    {
        private readonly AssetPipeline _pipeline;

        private List<BaseFilesystem> _filesystems;

        private ConcurrentDictionary<string, FileRemapData> _fileRemappings;

        private bool _disposedValue;

        internal FilesystemManager(AssetPipeline pipeline)
        {
            _pipeline = pipeline;

            _filesystems = new List<BaseFilesystem>();

            _fileRemappings = new ConcurrentDictionary<string, FileRemapData>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (BaseFilesystem filesystem in _filesystems)
                    {
                        if (filesystem is IDisposable disposable)
                            disposable.Dispose();
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

        internal void LoadRemappingsFromDisk()
        {
            if (File.Exists(s_registryFile))
            {
                RemappingDataJson data;
                try
                {
                    data = JsonSerializer.Deserialize(File.ReadAllText(s_registryFile), RemappingDataJsonContext.Default.RemappingDataJson)!;
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "Failed to read file remappings from disk!");
                    return;
                }

                if (data.Version != PhysicalFileJson.FileVersion)
                {
                    EdLog.Assets.Error("Incorrect remappings data version '{v}'", data.Version);
                    return;
                }

                foreach (RemappingDataEntry entry in data.Remappings)
                {
                    TryFindFilesystemFor(entry.Remap, out BaseFilesystem? filesystem);

                    if (!_fileRemappings.TryAdd(entry.Source, new FileRemapData(entry.Remap, filesystem)))
                    {
                        EdLog.Assets.Warning("Duplicate file remapping '{k}'", entry.Source);
                    }
                }
            }
        }

        internal void SaveRemappingsToDisk()
        {
            RemappingDataJson data = new RemappingDataJson();

            using RentedList<RemappingDataEntry> entries = new RentedList<RemappingDataEntry>();
            foreach (var (key, remap) in _fileRemappings)
            {
                entries.Add(new RemappingDataEntry(key, remap.RemapPath));
            }

            data.Remappings = entries.ToArray();

            try
            {
                File.WriteAllText(s_registryFile, JsonSerializer.Serialize(data, RemappingDataJsonContext.Default.RemappingDataJson));
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Failed to read file remappings from disk!");
                throw;
            }
        }

        internal void MountContent(string directory, string namespaceKey)
        {
            if (directory.Contains('\\'))
                directory = directory.Replace('\\', '/');

            EdLog.Assets.Information("Mounting filesystem at '{p}' with namespace key '{nk}'", directory, namespaceKey);

            ContentFilesystem filesystem = new ContentFilesystem(_pipeline, directory, namespaceKey);
            _filesystems.Add(filesystem);
        }

        internal void MountLibrary()
        {
            EdLog.Assets.Information("Mounting library filesystem");

            LibraryFilesystem filesystem = new LibraryFilesystem();
            _filesystems.Add(filesystem);
        }

        internal void UpdateFileRemap(string localPath, string newLocalPath)
        {
            if (_fileRemappings.TryRemove(localPath, out FileRemapData remapData))
            {
                _fileRemappings.TryAdd(newLocalPath, remapData);
            }
        }

        public void SetFileRemap(string fileToRemap, string? remapLocation)
        {
            if (remapLocation == null)
            {
                _fileRemappings.TryRemove(fileToRemap, out _);
            }
            else
            {
                if (!remapLocation.StartsWith("Library/"))
                {
                    SetFileRemap(fileToRemap, null);
                    return;
                }

                TryFindFilesystemFor(remapLocation, out BaseFilesystem? filesystem);
                _fileRemappings[fileToRemap] = new FileRemapData(remapLocation, filesystem);
            }
        }

        public bool TryFindFilesystemFor(string localPath, [NotNullWhen(true)] out BaseFilesystem? value)
        {
            foreach (BaseFilesystem filesystem in _filesystems)
            {
                if (filesystem.IsPathNamespacedTo(localPath))
                {
                    value = filesystem;
                    return true;
                }
            }

            value = null;
            return false;
        }

        #region Sub filesystem implementation
        public string? ReadAllText(ReadOnlySpan<char> path)
        {
            return ReadAllText(path.ToString());
        }

        public Stream? OpenStream(ReadOnlySpan<char> path)
        {
            return OpenStream(path.ToString());
        }

        public bool Exists(ReadOnlySpan<char> path)
        {
            return Exists(path.ToString());
        }
        #endregion

        public static string? GetLocalPath(string fullPath)
        {
            FilesystemManager self = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
            foreach (BaseFilesystem filesystem in self._filesystems)
            {
                if (filesystem.TryGetLocalPath(fullPath, out string? localPath))
                    return localPath;
            }

            return null;
        }

        public static string? GetFullPath(string localPath)
        {
            FilesystemManager self = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
            foreach (BaseFilesystem filesystem in self._filesystems)
            {
                if (filesystem.TryGetFullPath(localPath, out string? fullPath))
                    return fullPath;
            }

            return null;
        }

        public static bool TryGetLocalPath(string fullPath, [NotNullWhen(true)] out string? localPath)
        {
            localPath = GetLocalPath(fullPath);
            return localPath != null;
        }

        public static bool TryGetFullPath(string localPath, [NotNullWhen(true)] out string? fullPath)
        {
            fullPath = GetFullPath(localPath);
            return fullPath != null;
        }

        public static string? ReadAllText(string localPath)
        {
            FilesystemManager self = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
            if (self._fileRemappings.TryGetValue(localPath, out FileRemapData remapData))
            {
                if (remapData.Filesystem != null)
                    return remapData.Filesystem.ReadAllText(remapData.RemapPath);
                else
                    localPath = remapData.RemapPath;
            }
            
            foreach (BaseFilesystem filesystem in self._filesystems)
            {
                if (!filesystem.IsPathNamespacedTo(localPath))
                    continue;

                string? value = filesystem.ReadAllText(localPath);
                if (value != null)
                    return value;
            }

            return null;
        }

        public static byte[]? ReadAllBytes(string localPath)
        {
            FilesystemManager self = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
            if (self._fileRemappings.TryGetValue(localPath, out FileRemapData remapData))
            {
                if (remapData.Filesystem != null)
                    return remapData.Filesystem.ReadAllBytes(remapData.RemapPath);
                else
                    localPath = remapData.RemapPath;
            }

            foreach (BaseFilesystem filesystem in self._filesystems)
            {
                if (!filesystem.IsPathNamespacedTo(localPath))
                    continue;

                byte[]? value = filesystem.ReadAllBytes(localPath);
                if (value != null)
                    return value;
            }

            return null;
        }

        public static Stream? OpenStream(string localPath)
        {
            FilesystemManager self = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
            if (self._fileRemappings.TryGetValue(localPath, out FileRemapData remapData))
            {
                if (remapData.Filesystem != null)
                    return remapData.Filesystem.OpenStream(remapData.RemapPath);
                else
                    localPath = remapData.RemapPath;
            }

            foreach (BaseFilesystem filesystem in self._filesystems)
            {
                if (!filesystem.IsPathNamespacedTo(localPath))
                    continue;

                Stream? value = filesystem.OpenStream(localPath);
                if (value != null)
                    return value;
            }

            return null;
        }

        public static bool Exists(string localPath)
        {
            FilesystemManager self = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
            foreach (BaseFilesystem filesystem in self._filesystems)
            {
                if (!filesystem.IsPathNamespacedTo(localPath))
                    continue;

                if (filesystem.Exists(localPath))
                    return true;
            }

            return false;
        }

        public ROList<BaseFilesystem> Filesystems => _filesystems;

        private static string s_registryFile => Path.Combine(ProjectData.Instance.Paths.LibrarySavedFolder, "Remappings.json");

        private readonly record struct FileRemapData(string RemapPath, BaseFilesystem? Filesystem);
    }
}
