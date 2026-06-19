using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Primary;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Filesystem;
using PrimaryEditor.Assets.Importers;
using PrimaryEditor.Assets.Utility;
using PrimaryEditor.Core;
using PrimaryEditor.Project;
using PrimaryEditor.Startup;
using Sigil;
using TerraFX.Interop.WinRT;

namespace PrimaryEditor.Assets
{
    public sealed class AssetPipeline
    {
        private EditorRuntime _runtime;

        // primary
        private FilesystemManager _filesystemManager;
        private AssetAssociator _associator;
        private AssetConfiguration _configuration;
        private ImportScheduler _importScheduler;

        // registries
        private ImporterRegistry _importerRegistry;
        private PhysicalFileRegistry _physicalFileRegistry;
        private AssetRegistry _assetRegistry;

        // assets
        private ConcurrentDictionary<AssetId, object?> _importedAssets;

        private Lock _reloadLock;
        private HashSet<AssetId> _assetsToReload;
        private HashSet<AssetId> _pendingImports;

        internal AssetPipeline(EditorRuntime runtime)
        {
            _runtime = runtime;

            _filesystemManager = new FilesystemManager(this);
            _associator = new AssetAssociator();
            _configuration = new AssetConfiguration(this);
            _importScheduler = new ImportScheduler(this);

            _importerRegistry = new ImporterRegistry();
            _physicalFileRegistry = new PhysicalFileRegistry();
            _assetRegistry = new AssetRegistry();

            _importedAssets = new ConcurrentDictionary<AssetId, object?>();

            _reloadLock = new Lock();
            _assetsToReload = new HashSet<AssetId>();
            _pendingImports = new HashSet<AssetId>();
        }

        internal void ImportAnyChangesLaunch(StartupSplash splash)
        {
            MountRequiredFilesystems();
            LoadDefaultData();
            RegisterImporters();

            splash.ActionName = "Checking for asset changes..";

            ConcurrentDictionary<AssetId, object?> alreadyImporting = new ConcurrentDictionary<AssetId, object?>();
            foreach (BaseFilesystem filesystem in _filesystemManager.Filesystems)
            {
                if (filesystem is not ContentFilesystem)
                    continue;

                DateTime startTime = DateTime.Now;

                string[] files = Directory.GetFiles(filesystem.WorkingDirectory, "*.*", SearchOption.AllDirectories);
                Parallel.ForEach(files, (file) =>
                {
                    if (filesystem.TryGetLocalPath(file, out string? localPath))
                    {
                        bool isFileOutOfDate = false;

                        AssetId id = _assetRegistry.GetOrRegisterIdFor(localPath);
                        if (_physicalFileRegistry.TryGetFileInfo(id, out PhysicalFileInfo fileInfo))
                        {
                            DateTime lastWriteTime = fileInfo.LastModifiedTime;
                            if (lastWriteTime < File.GetLastWriteTime(file))
                            {
                                _physicalFileRegistry.TryUpdateFileFromPath(id, file);
                                isFileOutOfDate = true;
                            }
                        }
                        else
                        {
                            _physicalFileRegistry.TryRegisterFileFromPath(id, file);
                            isFileOutOfDate = true;
                        }

                        int index = localPath.LastIndexOf('.');
                        if (index != -1)
                        {
                            if (_importerRegistry.TryGetImporterFor(localPath.AsSpan()[index..], out IAssetImporter? importer))
                            {
                                if (isFileOutOfDate || !IsFileImportedAndValid(importer, localPath))
                                {
                                    ScheduleForImport(importer, id, localPath, file);
                                    alreadyImporting.TryAdd(id, null);
                                }
                            }
                        }

                        if (isFileOutOfDate)
                        {
                            TryImportDependents(id);

                            void TryImportDependents(AssetId id)
                            {
                                using Lock.Scope scope = _associator.GetAssocationDataWithLockScope(id, out AssociationData associationData, out bool exists);
                                if (exists)
                                {
                                    foreach (AssetId dependency in associationData.Dependents)
                                    {
                                        if (alreadyImporting.TryAdd(dependency, null))
                                        {
                                            localPath = _assetRegistry.RetrievePathForId(dependency);
                                            if (localPath != null)
                                            {
                                                int index = localPath.LastIndexOf('.');
                                                if (index != -1)
                                                {
                                                    if (_importerRegistry.TryGetImporterFor(localPath.AsSpan()[index..], out IAssetImporter? importer))
                                                    {
                                                        if (isFileOutOfDate || !IsFileImportedAndValid(importer, localPath))
                                                        {
                                                            ScheduleForImport(importer, id, localPath, file);
                                                            alreadyImporting.TryAdd(id, null);
                                                        }
                                                    }
                                                }
                                            }

                                            TryImportDependents(dependency);
                                        }
                                    }
                                }
                            }
                        }
                    }
                });

                EdLog.Assets.Information("Took {secs}s to check for changes in filesystem '{fs}'", (DateTime.Now - startTime).TotalSeconds, filesystem.NamespaceKey);
            }

            splash.ActionName = $"Waiting for imports.. ({_importScheduler.ScheduledImports})";
            splash.ProgressReporter = () => _importScheduler.FinishedImports / (float)_importScheduler.ScheduledImports;

            _importScheduler.WaitForAllImports();
            _assetsToReload.Clear();

            splash.ProgressReporter = null;

            SaveGeneratedData();

            void MountRequiredFilesystems()
            {
                splash.ActionName = "Mounting filesystems..";

                _filesystemManager.MountLibrary();
                _filesystemManager.MountContent(ProjectData.Instance.Paths.ContentFolder, "Content");

                JsonArray mounts = JsonSerializer.Deserialize<JsonArray>(File.ReadAllText("Filesystems.json"))!;
                foreach (JsonObject obj in mounts!)
                {
                    _filesystemManager.MountContent((string)obj["Path"]!, (string)obj["NamespaceKey"]!);
                }
            }

            void LoadDefaultData()
            {
                splash.ActionName = "Loading asset data..";

                _filesystemManager.LoadRemappingsFromDisk();
                _associator.LoadAssocationsFromDisk();

                _physicalFileRegistry.LoadRegistryFromDisk();
                _assetRegistry.LoadRegistryFromDisk();

                LoadImportedAssetsFromDisk();
            }

            void RegisterImporters()
            {
                splash.ActionName = "Registering importers..";

                _importerRegistry.RegisterImporter<TextureImporter>(".png", ".jpg", ".jpeg", ".cubemap", ".texcomp");
                _importerRegistry.RegisterImporter<ShaderImporter>(".shader");
            }

            void SaveGeneratedData()
            {
                splash.ActionName = "Saving asset data..";

                _filesystemManager.SaveRemappingsToDisk();
                _associator.SaveAssociationsToDisk();

                _physicalFileRegistry.SaveRegistryToDisk();
                _assetRegistry.SaveRegistryToDisk();

                SaveImportedAssetsToDisk();
            }
        }

        private void LoadImportedAssetsFromDisk()
        {
            if (File.Exists(s_importedDataPath))
            {
                AssetId[] ids = JsonSerializer.Deserialize<AssetId[]>(File.ReadAllText(s_importedDataPath))!;
                foreach (AssetId id in ids)
                {
                    _importedAssets.TryAdd(id, null);
                }
            }
        }

        private void SaveImportedAssetsToDisk()
        {
            File.WriteAllText(s_importedDataPath, JsonSerializer.Serialize(_importedAssets.Keys.ToArray(), s_options));
        }

        private bool IsFileImportedAndValid(IAssetImporter importer, ReadOnlySpan<char> filePath)
        {
            if (_assetRegistry.TryGetIdFromPath(filePath, out AssetId id))
            {
                if (!_importedAssets.ContainsKey(id))
                    return false;

                try
                {
                    return importer.ValidateFile(this, id, filePath.ToString());
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }

        public void ReloadAsset(AssetId id)
        {
            using (_reloadLock.EnterScope())
            {
                _assetsToReload.Add(id);
            }
        }

        #region Updates
        internal void HandleUpdates()
        {
            _importScheduler.UpdateImportStatus();

            CheckFilesystemUpdates();
            TryImportPending();
            ReloadPending();
        }

        private void CheckFilesystemUpdates()
        {
            foreach (BaseFilesystem filesystem in _filesystemManager.Filesystems)
            {
                if (filesystem is ContentFilesystem content && content.AreFileUpdatesAvailable)
                {
                    if (content.TryEnterLock())
                    {
                        while (content.TryPopFileEvent(out FileEvent fileEvent))
                        {
                            switch (fileEvent.EventType)
                            {
                                case FileEventType.Created:
                                    break;
                                case FileEventType.Deleted:
                                    break;
                                case FileEventType.Renamed:
                                    break;
                                case FileEventType.Changed:
                                    {
                                        TryImportChangedAsset(fileEvent.LocalFilePath);
                                        break;
                                    }
                                case FileEventType.Moved:
                                    {
                                        _assetRegistry.MoveFileWithinRegistry(fileEvent.LocalFilePath, fileEvent.NewLocalFilePath!);
                                        _filesystemManager.UpdateFileRemap(fileEvent.LocalFilePath, fileEvent.NewLocalFilePath!);
                                        break;
                                    }
                            }

                            EdLog.Assets.Information("{ty}: localPath:'{p}' newLocalPath:'{n}'", fileEvent.EventType, fileEvent.LocalFilePath, fileEvent.NewLocalFilePath);
                        }

                        content.ExitLock();
                    }
                }
            }
        }

        private void TryImportPending()
        {
            if (_pendingImports.Count == 0)
                return;

            using RentedList<AssetId> ids = new RentedList<AssetId>();
            foreach (AssetId id in _pendingImports)
            {
                if (!_importScheduler.IsImportRunningFor(id))
                {
                    ids.Add(id);

                    string? localPath = _assetRegistry.RetrievePathForId(id);
                    if (localPath == null)
                    {
                        EdLog.Assets.Warning("Failed to schedule import for '{id}' because the local path could not be found", id);
                        continue;
                    }

                    if (_importerRegistry.TryGetImporterForPath(localPath, out IAssetImporter? importer))
                    {
                        if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
                        {
                            EdLog.Assets.Warning("Failed to schedule import for '{p}' because the full path could not be found", localPath);
                            continue;
                        }

                        ScheduleForImport(importer, id, localPath, fullPath);
                    }
                    else
                    {
                        EdLog.Assets.Warning("Failed to schedule import for '{p}' because no suitable importer was found", localPath);
                        continue;
                    }
                }
            }

            if (!ids.IsEmpty)
            {
                foreach (AssetId id in ids)
                {
                    _pendingImports.Remove(id);
                }
            }
        }

        private void ReloadPending()
        {
            if (_assetsToReload.Count > 0)
            {
                using (_reloadLock.EnterScope())
                {
                    foreach (AssetId id in _assetsToReload)
                    {
                        _runtime.AssetManager.ForceReloadAsset(id);
                    }

                    _assetsToReload.Clear();
                }
            }
        }
        #endregion
        #region Import
        private void TryImportChangedAsset(string localPath)
        {
            if (_importerRegistry.TryGetImporterForPath(localPath, out IAssetImporter? importer))
            {
                if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
                {
                    EdLog.Assets.Error("Failed to get full path for local path '{l}'", localPath);
                    return;
                }

                AssetId id = _assetRegistry.GetOrRegisterIdFor(localPath);
                TryImportWithDependents(id, localPath, fullPath);

                void TryImportWithDependents(AssetId id, string? localPath, string? fullPath)
                {
                    localPath ??= _assetRegistry.RetrievePathForId(id);
                    if (localPath == null)
                        return;

                    fullPath ??= FilesystemManager.GetFullPath(localPath);
                    if (fullPath == null)
                        return;

                    if (_importScheduler.IsImportRunningFor(id))
                    {
                        _pendingImports.Add(id);
                    }
                    else
                    {
                        _pendingImports.Remove(id);
                        ScheduleForImport(importer!, id, localPath, fullPath);
                    }

                    using Lock.Scope lockScope = _associator.GetAssocationDataWithLockScope(id, out AssociationData associationData, out bool exists);
                    if (exists)
                    {
                        foreach (AssetId dependent in associationData.Dependents)
                        {
                            TryImportWithDependents(dependent, null, null);
                        }
                    }
                }
            }
        }

        private void ScheduleForImport(IAssetImporter importer, AssetId id, string localPath, string fullPath)
        {
            Action action = () =>
            {
                DateTime startTime = DateTime.Now;

                using Stream? inputStream = FileUtility.TryWaitOpenNoThrow(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (inputStream == null)
                {
                    EdLog.Assets.Error("[{file}]: Failed to open input stream", localPath);
                    return;
                }

                string outputFilePath = Path.Combine(ProjectData.Instance.Paths.LibraryImportedFolder, id.ToString());
                string localOutputPath = $"Library/Imported/{id:N}";

                using Stream outputStream = new LazyFileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

                try
                {
                    importer.ImportFile(this, id, inputStream, outputStream, localPath, localOutputPath);
                    outputStream.Dispose();

                    ReportImportAsFinished(id);
                }
                catch (Exception ex)
                {
                    outputStream.Dispose();

                    if (ex is not AssetImportException)
                    {
                        EdLog.Assets.Error(ex, "[{file}]: An unexpected error occured while trying to import", localPath);
                    }

                    if (File.Exists(outputFilePath))
                        File.Delete(outputFilePath);

                    ReportImportAsFailed(id);
                }

                EdLog.Assets.Debug("Import '{i}' finished in {sc}s", localPath, (DateTime.Now - startTime).TotalSeconds);
            };

            //EdLog.Assets.Debug("Scheduling import for asset '{a}'", localPath);
            _importScheduler.ScheduleImport(action, id);
        }

        internal void ReportImportAsFinished(AssetId id)
        {
            _importedAssets.TryAdd(id, null);
        }

        internal void ReportImportAsFailed(AssetId id)
        {
            _importedAssets.TryRemove(id, out _);
        }
        #endregion

        public FilesystemManager FilesystemManager => _filesystemManager;
        public AssetAssociator Associator => _associator;
        public AssetConfiguration Configuration => _configuration;

        public ImporterRegistry ImporterRegistry => _importerRegistry;
        public PhysicalFileRegistry PhysicalFileRegistry => _physicalFileRegistry;
        public AssetRegistry AssetRegistry => _assetRegistry;

        private static string s_importedDataPath => Path.Combine(ProjectData.Instance.Paths.LibrarySavedFolder, "Imported.json");
        private static readonly JsonSerializerOptions s_options = new JsonSerializerOptions
        {
            WriteIndented = EditorRuntime.IsDebugBuild,
            IndentSize = 4,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
