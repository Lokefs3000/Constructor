using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Primary;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using Primary.Streams;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Filesystem;
using PrimaryEditor.Assets.Importers;
using PrimaryEditor.Assets.Loaders;
using PrimaryEditor.Assets.Serialization;
using PrimaryEditor.Core;
using PrimaryEditor.Project;
using PrimaryEditor.Startup;
using Tomlyn;
using Tomlyn.Model;

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
        private ConcurrentDictionary<AssetId, AssetImportData> _importedAssets;

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

            _importedAssets = new ConcurrentDictionary<AssetId, AssetImportData>();

            _reloadLock = new Lock();
            _assetsToReload = new HashSet<AssetId>();
            _pendingImports = new HashSet<AssetId>();
        }

        internal void ImportAnyChangesLaunch(StartupSplash splash)
        {
            long setupTimestamp = Stopwatch.GetTimestamp();

            MountRequiredFilesystems();
            LoadDefaultData();
            RegisterImporters();

            long assetCheckTimestamp = Stopwatch.GetTimestamp();

            long importWaitTimestamp;
            long saveDataTimestamp;
            if (!AppArguments.HasArgument("skip-asset-check"))
            {
                splash.ActionName = "Finding assets..";

                Queue<string> foldersToSearch = new Queue<string>();
                HashSet<AssetId> requiredImports = new HashSet<AssetId>();
                HashSet<AssetId> newAssets = new HashSet<AssetId>();

                foreach (BaseFilesystem filesystem in _filesystemManager.Filesystems)
                {
                    if (filesystem is not ContentFilesystem)
                        continue;

                    ContentFilesystem? contentFs = filesystem as ContentFilesystem;

                    foldersToSearch.Clear();
                    foldersToSearch.Enqueue(filesystem.WorkingDirectory);

                    splash.ActionName = $"Finding assets.. ({filesystem.NamespaceKey})";

                    long startTimestamp = Stopwatch.GetTimestamp();

                    int searchedFiles = 0;
                    int searchedDirs = 0;

                    int parsedAssetDatas = 0;
                    int generatedAssetDatas = 0;

                    while (foldersToSearch.TryDequeue(out string? directoryPath))
                    {
                        foreach (string entryPathInDir in Directory.EnumerateFileSystemEntries(directoryPath, "*.*", SearchOption.TopDirectoryOnly))
                        {
                            if (entryPathInDir.EndsWith(".assetdat"))
                                continue;

                            if (Directory.Exists(entryPathInDir))
                            {
                                ++searchedDirs;
                                foldersToSearch.Enqueue(entryPathInDir);

                                if (contentFs != null && filesystem.TryGetLocalPath(entryPathInDir, out string? localPath))
                                {
                                    contentFs.Structure.AddDirectory(localPath);
                                }
                            }
                            else if (filesystem.TryGetLocalPath(entryPathInDir, out string? localPath))
                            {
                                contentFs?.Structure.AddFile(localPath);

                                ++searchedFiles;
                                AssetDataHeader dataHeader;

                                string assetDataPath = entryPathInDir + ".assetdat";
                                if (File.Exists(assetDataPath))
                                {
                                    using Stream? sourceStream = FileUtility.TryWaitOpenNoThrow(assetDataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 5, 60);
                                    if (sourceStream == null)
                                    {
                                        EdLog.Assets.Error("Failed to open asset data file '{f}'! I would recommend that this application be shutdown and the file unlocked before further usage", assetDataPath);
                                        continue;
                                    }

                                    if (!TomlSerializer.TryDeserialize(sourceStream, AssetDataHeaderTomlContext.Default, out dataHeader))
                                    {
                                        EdLog.Assets.Error("Failed to deserialize asset data file '{f}'! Time to close the application and fix whatever is messed up here", assetDataPath);
                                        continue;
                                    }

                                    if (dataHeader.Id.IsInvalid)
                                    {
                                        EdLog.Assets.Error("Invalid id present within the asset data for file '{f}'", localPath);
                                        continue;
                                    }

                                    if (dataHeader.Version != AssetDataHeader.TargetVersion)
                                    {
                                        throw new NotSupportedException("Upgrading version not supported yet");
                                    }

                                    ++parsedAssetDatas;
                                }
                                else
                                {
                                    dataHeader = new AssetDataHeader((AssetId)Guid.CreateVersion7(), false);

                                    FileUtility.TryWriteAllText(assetDataPath, @$"asset_id = ""{dataHeader.Id}""
meta_version = {AssetDataHeader.TargetVersion}", 5, 60);

                                    ++generatedAssetDatas;
                                    EdLog.Assets.Debug("Created asset data file for '{f}'", localPath);
                                }

                                bool hasImporterFor = _importerRegistry.TryGetImporterForPath(localPath, out IAssetImporter? importer);

                                if (!_importedAssets.TryGetValue(dataHeader.Id, out AssetImportData importData))
                                {
                                    importData = new AssetImportData(null, false, !hasImporterFor);
                                    _importedAssets[dataHeader.Id] = importData;

                                    newAssets.Add(dataHeader.Id);
                                }

                                string? importedPath = _importedAssets.ContainsKey(dataHeader.Id) ? GetLibraryImportPathFor(dataHeader.Id) : null;
                                _assetRegistry.SetupFileWithinRegistryWithId(dataHeader.Id, localPath, importedPath);

                                if (hasImporterFor)
                                {
                                    if (importData.ImporterId != null && importer!.UniqueId != importData.ImporterId)
                                    {
                                        EdLog.Assets.Warning("Conflicting importer ids for asset '{p}' ('{c}', '{n}')", localPath, importData.ImporterId, importer!.UniqueId);
                                    }

                                    bool shouldTryImport = importData.IsSkippedOnImport ?
                                        _physicalFileRegistry.IsDataOutOfDate(dataHeader.Id, assetDataPath) :
                                        _physicalFileRegistry.IsFileOrDataOutOfDate(dataHeader.Id, entryPathInDir, assetDataPath)/* || (importData.ImporterId != null && importedPath != null && !IsFileImportedAndValid(importer!, dataHeader.Id, importedPath))*/;

                                    if (shouldTryImport)
                                    {
                                        if (requiredImports.Add(dataHeader.Id))
                                        {
                                            ImportDependents(dataHeader.Id);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    EdLog.Assets.Information(@"Asset search done for '{f}' in {s}s
    Searched files/dirs: {sf}/{sd}
    Asset datas parsed/generated: {adp}/{adg}", filesystem.WorkingDirectory, Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds, searchedFiles, searchedDirs, parsedAssetDatas, generatedAssetDatas);
                }

                EdLog.Assets.Information("New assets: {c}", newAssets.Count);
                EdLog.Assets.Information("Scheduling import for {c} total assets", requiredImports.Count);

                splash.ActionName = $"Scheduling imports.. ({requiredImports.Count})";

                foreach (AssetId id in requiredImports)
                {
                    if (_assetRegistry.TryGetLocalPathForId(id, out string? localPath) &&
                        _importerRegistry.TryGetImporterForPath(localPath, out IAssetImporter? importer) &&
                        FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
                    {
                        ScheduleForImport(importer, id, localPath, fullPath, newAssets.Contains(id));
                    }
                }

                splash.ActionName = $"Waiting for imports.. ({_importScheduler.ScheduledImports})";
                splash.ProgressReporter = () => (float)(_importScheduler.FinishedImports / (double)_importScheduler.ScheduledImports);

                importWaitTimestamp = Stopwatch.GetTimestamp();

                _importScheduler.WaitForAllImports();
                _assetsToReload.Clear();

                splash.ProgressReporter = null;
                saveDataTimestamp = Stopwatch.GetTimestamp();

                void ImportDependents(AssetId id)
                {
                    using Lock.Scope lockScope = _associator.GetAssocationDataWithLockScope(id, out AssociationData associationData, out bool exists);
                    if (exists)
                    {
                        foreach (AssetId dependent in associationData.Dependents)
                        {
                            if (requiredImports.Add(dependent))
                            {
                                ImportDependents(dependent);
                            }
                        }
                    }
                }
            }
            else
            {
                EdLog.Assets.Information("!!! ASSET CHECK IS DIABLED !!!");

                importWaitTimestamp = assetCheckTimestamp;
                saveDataTimestamp = assetCheckTimestamp;
            }

            SaveGeneratedData();

            long endTimestamp = Stopwatch.GetTimestamp();

            EdLog.Assets.Information(@"Asset setup timings:
    Setup:          {a}s
    Asset check:    {b}s
    Import wait:    {c}s
    Save data:      {d}s
    Total:          {e}s", Stopwatch.GetElapsedTime(setupTimestamp, assetCheckTimestamp).TotalSeconds, Stopwatch.GetElapsedTime(assetCheckTimestamp, importWaitTimestamp).TotalSeconds, Stopwatch.GetElapsedTime(importWaitTimestamp, saveDataTimestamp).TotalSeconds, Stopwatch.GetElapsedTime(saveDataTimestamp, endTimestamp).TotalSeconds, Stopwatch.GetElapsedTime(setupTimestamp).TotalSeconds);

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

                // _filesystemManager.LoadRemappingsFromDisk();
                _associator.LoadAssocationsFromDisk();

                _physicalFileRegistry.LoadRegistryFromDisk();
                // _assetRegistry.LoadRegistryFromDisk();

                LoadImportedAssetsFromDisk();
            }

            void RegisterImporters()
            {
                splash.ActionName = "Registering importers..";

                _importerRegistry.RegisterImporter<TextureImporter>(".png", ".jpg", ".jpeg", ".cubemap", ".texcomp");
                _importerRegistry.RegisterImporter<ShaderImporter>(".shader");
                _importerRegistry.RegisterImporter<UIFontFamilyImporter>(".uifont");
                _importerRegistry.RegisterImporter<UILayoutImporter>(".layout");
                _importerRegistry.RegisterImporter<StylesheetImporter>(".style");
                _importerRegistry.RegisterImporter<TextureAtlasImporter>(".atlas");
            }

            void SaveGeneratedData()
            {
                splash.ActionName = "Saving asset data..";

                // _filesystemManager.SaveRemappingsToDisk();
                _associator.SaveAssociationsToDisk();

                _physicalFileRegistry.SaveRegistryToDisk();
                // _assetRegistry.SaveRegistryToDisk();

                SaveImportedAssetsToDisk();
            }
        }

        internal void RegisterCustomAssets(StartupSplash splash)
        {
            splash.ActionName = "Registering assets..";

            _runtime.AssetManager.RegisterCustomAsset<UIFontFamilyAsset>(new UIFontFamilyLoader());
            _runtime.AssetManager.RegisterCustomAsset<UILayoutAsset>(new UILayoutAssetLoader());
            _runtime.AssetManager.RegisterCustomAsset<StylesheetAsset>(new StylesheetAssetLoader());
        }

        private void LoadImportedAssetsFromDisk()
        {
            if (File.Exists(s_importedDataPath))
            {
                using Stream? inputStream = FileUtility.TryWaitOpenNoThrow(s_importedDataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (inputStream == null)
                {
                    EdLog.Assets.Error("Failed to open file stream '{f}' for imported asset data", s_importedDataPath);
                    FileUtility.TryDelete(s_importedDataPath);
                    return;
                }

                using DataReader serializer = new DataReader(inputStream);

                if (serializer.ReadVersionHeader() != FileCurrentVersion)
                    throw new Exception("Invalid version in imported asset data");

                while (!serializer.IsAtEndOfStream)
                {
                    AssetId assetId = (AssetId)serializer.ReadGuid()!.Value;
                    string? importerId = serializer.ReadString();
                    bool isImported = serializer.ReadBoolean()!.Value;
                    bool isSkippedOnImport = serializer.ReadBoolean()!.Value;

                    serializer.ReadNewLine();

                    _importedAssets[assetId] = new AssetImportData(importerId, isImported, isSkippedOnImport);
                }
            }
        }

        private void SaveImportedAssetsToDisk()
        {
            using Stream? outputStream = FileUtility.TryWaitOpenNoThrow(s_importedDataPath, FileMode.Create, FileAccess.Write, FileShare.None);
            if (outputStream == null)
            {
                EdLog.Assets.Error("Failed to open file stream '{f}' for writing imported asset data", s_importedDataPath);
                FileUtility.TryDelete(s_importedDataPath);
                return;
            }

            using DataWriter serializer = new DataWriter(outputStream);

            serializer.WriteVersionHeader(FileCurrentVersion);

            foreach (var (id, importData) in _importedAssets)
            {
                serializer.WriteValue(id);

                if (importData.ImporterId != null)
                    serializer.WriteValue(importData.ImporterId);
                else
                    serializer.WriteNull();

                serializer.WriteValue(importData.IsImported);
                serializer.WriteValue(importData.IsSkippedOnImport);

                serializer.FinishLine();
            }
        }

        private bool IsFileImportedAndValid(IAssetImporter importer, AssetId id, string filePath)
        {
            try
            {
                return importer.ValidateFile(this, id, filePath);
            }
            catch (Exception)
            {
                return false;
            }
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
                                case FileEventType.Deleted:
                                    break;
                                case FileEventType.Renamed:
                                    {
                                        TryRenameAssetData(fileEvent.LocalFilePath, fileEvent.NewLocalFilePath!);
                                        _assetRegistry.UpdateLocalPath(fileEvent.LocalFilePath, fileEvent.NewLocalFilePath!);
                                        break;
                                    }
                                case FileEventType.Created:
                                    {
                                        TryCreateAssetDataFor(fileEvent.LocalFilePath, out bool alreadyExists);
                                        if (alreadyExists)
                                            TryImportChangedAsset(fileEvent.LocalFilePath);
                                        break;
                                    }
                                case FileEventType.Changed:
                                    {
                                        string localPath = fileEvent.LocalFilePath;
                                        if (localPath.EndsWith(".assetdat"))
                                        {
                                            localPath = localPath[..(localPath.Length - ".assetdat".Length)];
                                            if (!FilesystemManager.Exists(localPath))
                                                break;
                                        }

                                        TryImportChangedAsset(localPath);
                                        break;
                                    }
                                case FileEventType.Moved:
                                    {
                                        TryMoveAssetData(fileEvent.LocalFilePath, fileEvent.NewLocalFilePath!);
                                        _assetRegistry.UpdateLocalPath(fileEvent.LocalFilePath, fileEvent.NewLocalFilePath!);
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

                    if (!_assetRegistry.TryGetLocalPathForId(id, out string? localPath))
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

                        ScheduleForImport(importer, id, localPath, fullPath, false);
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

        private void TryCreateAssetDataFor(string localPath, out bool alreadyExists)
        {
            alreadyExists = false;

            if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
            {
                EdLog.Assets.Warning("Failed to get full path for '{p}' to generate asset data file", localPath);
                return;
            }

            string assetDataPath = fullPath + ".assetdat";

            // If the file already exists the asset has already been prepared for use
            if (File.Exists(assetDataPath))
            {
                alreadyExists = true;
                return;
            }

            AssetId id = _assetRegistry.SetupFileWithinRegistry(localPath, null);

            FileUtility.TryWriteAllText(assetDataPath, @$"asset_id = ""{id}""
meta_version = {AssetDataHeader.TargetVersion}", 5, 60);

            _physicalFileRegistry.StoreFileData(id, fullPath, assetDataPath);

            EdLog.Assets.Debug("Created asset data file for '{f}' ({id})", localPath, id);
        }

        private void TryRenameAssetData(string oldLocalPath, string newLocalPath)
        {
            if (!FilesystemManager.TryGetFullPath(oldLocalPath, out string? fullPath))
            {
                EdLog.Assets.Warning("Failed to get full path for '{p}' to rename asset data file", oldLocalPath);
                return;
            }

            string assetDataPath = Path.Combine(fullPath);
            if (File.Exists(assetDataPath))
            {
                string newFileName = Path.GetFileNameWithoutExtension(newLocalPath);
                string newAssetDataPath = Path.Combine(Path.GetDirectoryName(assetDataPath)!, newFileName + ".assetdat");

                try
                {
                    File.Move(assetDataPath, newAssetDataPath);
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Warning(ex, "Failed to move asset data path from '{f}' to '{t}'", assetDataPath, newAssetDataPath);
                }
            }
        }

        private void TryMoveAssetData(string oldLocalPath, string newLocalPath)
        {
            if (!FilesystemManager.TryGetFullPath(oldLocalPath, out string? fullPath))
            {
                EdLog.Assets.Warning("Failed to get full path for '{p}' to rename asset data file", oldLocalPath);
                return;
            }

            if (!FilesystemManager.TryGetFullPath(newLocalPath, out string? newFullPath))
            {
                EdLog.Assets.Warning("Failed to get full path for '{p}' to rename asset data file", newLocalPath);
                return;
            }

            string assetDataPath = Path.Combine(fullPath);
            if (File.Exists(assetDataPath))
            {
                string newAssetDataPath = newFullPath + ".assetdat";

                try
                {
                    File.Move(assetDataPath, newAssetDataPath);
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Warning(ex, "Failed to move asset data path from '{f}' to '{t}'", assetDataPath, newAssetDataPath);
                }
            }
        }
        #endregion
        #region Import
        private void TryImportChangedAsset(string localPath)
        {
            if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
            {
                EdLog.Assets.Error("Failed to get full path for local path '{l}'", localPath);
                return;
            }

            AssetId id = GetOrRegisterIdForPath(localPath);
            if (_importerRegistry.TryGetImporterForPath(localPath, out IAssetImporter? importer))
            {
                _pendingImports.Add(id);
            }

            using Lock.Scope lockScope = _associator.GetAssocationDataWithLockScope(id, out AssociationData associationData, out bool exists);
            if (exists)
            {
                foreach (AssetId dependent in associationData.Dependents)
                {
                    if (_assetRegistry.TryGetLocalPathForId(dependent, out string? dependentLocalPath))
                        TryImportChangedAsset(dependentLocalPath);
                }
            }
        }

        private void RemoveImportedAsset(AssetId id)
        {
            if (_importedAssets.TryGetValue(id, out AssetImportData importData))
            {
                importData.IsImported = false;
                _importedAssets[id] = importData;

                _assetRegistry.RemoveAssetPath(id);
                _associator.ClearAssociations(id);

                FileUtility.TryDelete(GetLibraryImportPathFor(id));
            }
        }

        private void ScheduleForImport(IAssetImporter importer, AssetId id, string localPath, string fullPath, bool isTrialImport)
        {
            Action action = () =>
            {
                DateTime startTime = DateTime.Now;

                using FileStream? inputStream = FileUtility.TryWaitOpenNoThrow(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (inputStream == null)
                {
                    EdLog.Assets.Error("[{file}]: Failed to open input stream", localPath);
                    return;
                }

                string localOutputPath;

                {
                    GuidStringArray stringArray = default;
                    id.TryFormat(stringArray.Span, out int _);

                    string prefixDir = Path.Combine(ProjectData.Instance.Paths.LibraryImportedFolder, stringArray.Span[..2].ToString());
                    if (!Directory.Exists(prefixDir))
                        Directory.CreateDirectory(prefixDir);

                    localOutputPath = GetLibraryImportPathFor(stringArray.Span);
                }

                string outputFilePath = Path.Combine(ProjectData.Instance.Paths.RootFolder, localOutputPath);

                PooledMemoryStream outputStream = new PooledMemoryStream();

                try
                {
                    importer.ImportFile(this, id, inputStream, outputStream, localPath, localOutputPath, isTrialImport);

                    if (!outputStream.IsEmpty)
                    {
                        using FileStream? fileStream = FileUtility.TryWaitOpenNoThrow(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        if (fileStream == null)
                        {
                            EdLog.Assets.Error("[{file}]: Failed to open stream to flush imported data to disk", localPath);
                            throw new AssetLoadException();
                        }

                        outputStream.CopyTo(fileStream);
                        fileStream.Flush(true);
                    }

                    ReportImportAsFinished(id, importer);
                }
                catch (AssetIgnoredException)
                {
                    if (File.Exists(outputFilePath))
                        File.Delete(outputFilePath);

                    outputStream.SetLength(0);

                    ReportImportAsIgnored(id, importer);
                    return;
                }
                catch (Exception ex)
                {
                    if (ex is not AssetImportException)
                    {
                        EdLog.Assets.Error(ex, "[{file}]: An unexpected error occured while trying to import", localPath);
                    }

                    if (File.Exists(outputFilePath))
                        File.Delete(outputFilePath);

                    ReportImportAsFailed(id, importer);
                    return;
                }

                outputStream.Dispose();

                EdLog.Assets.Debug("Import '{i}' finished in {sc}s", localPath, (DateTime.Now - startTime).TotalSeconds);
            };

            //EdLog.Assets.Debug("Scheduling import for asset '{a}'", localPath);
            _importScheduler.ScheduleImport(action, id);
        }

        internal void ReportImportAsFinished(AssetId id, IAssetImporter importer)
        {
            if (!_assetRegistry.TryGetAssetPathForId(id, out string? localPath))
                return;

            if (!_importedAssets.TryGetValue(id, out AssetImportData importData) || importData.ImporterId != importer.UniqueId || !importData.IsImported || importData.IsSkippedOnImport)
            {
                importData.ImporterId = importer.UniqueId;
                importData.IsImported = true;
                importData.IsSkippedOnImport = false;

                _importedAssets[id] = importData;
            }
        }

        internal void ReportImportAsFailed(AssetId id, IAssetImporter importer)
        {
            if (_importedAssets.TryGetValue(id, out AssetImportData importData))
            {
                importData.IsImported = false;
                _importedAssets[id] = importData;

                _assetRegistry.RemoveAssetPath(id);

                FileUtility.TryDelete(GetLibraryImportPathFor(id));
            }
        }

        internal void ReportImportAsIgnored(AssetId id, IAssetImporter importer)
        {
            if (!_assetRegistry.TryGetAssetPathForId(id, out string? localPath))
                return;

            if (_importedAssets.TryGetValue(id, out AssetImportData importData) && !importData.IsSkippedOnImport)
            {
                importData.IsSkippedOnImport = true;
                _importedAssets[id] = importData;
            }
        }
        #endregion
        #region Ids
        /// <summary>Gets the id for a file within the filesystem or registers it if it has no previously been registered.</summary>
        /// <param name="localPath">The file within the filesystem to get an id for.</param>
        /// <returns>The files id or <seealso cref="AssetId.Invalid"/> if the file is invalid.</returns>
        /// <remarks>This method will always fail when trying to register a file with an '.assetdat' extension since that file actually holds it's parent files id.</remarks>
        internal AssetId GetOrRegisterIdForPath(string localPath)
        {
            // assetdat files cannot have an id
            if (localPath.EndsWith(".assetdat"))
                return AssetId.Invalid;

            if (!_assetRegistry.TryLookupIdForPath(localPath, out AssetId id))
            {
                // This in return also ends up doing a check for if the path is local
                if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
                    return AssetId.Invalid;

                // No logic in creating an id for a file that doesn't exist
                if (!File.Exists(fullPath))
                    return AssetId.Invalid;

                string dataFilePath = fullPath + ".assetdat";

                AssetId assetId = _assetRegistry.SetupFileWithinRegistry(localPath, null);
                _physicalFileRegistry.StoreFileData(id, fullPath, File.Exists(dataFilePath) ? dataFilePath : null);
            }

            return id;
        }
        #endregion

        public FilesystemManager FilesystemManager => _filesystemManager;
        public AssetAssociator Associator => _associator;
        public AssetConfiguration Configuration => _configuration;

        public ImporterRegistry ImporterRegistry => _importerRegistry;
        public PhysicalFileRegistry PhysicalFileRegistry => _physicalFileRegistry;
        public AssetRegistry AssetRegistry => _assetRegistry;

        private static string GetLibraryImportPathFor(AssetId id)
        {
            Span<char> formatBuffer = stackalloc char[32];
            id.TryFormat(formatBuffer, out int _);
            return GetLibraryImportPathFor(formatBuffer);
        }

        private static string GetLibraryImportPathFor(ReadOnlySpan<char> id)
        {
            Debug.Assert(id.Length == 32);
            return $"Library/Imported/{id[..2]}/{id[2..]}";
        }

        private static string s_importedDataPath => Path.Combine(ProjectData.Instance.Paths.LibrarySavedFolder, "ImportedFiles.dat");
        private static readonly JsonSerializerOptions s_options = new JsonSerializerOptions
        {
            WriteIndented = EditorRuntime.IsDebugBuild,
            IndentSize = 4,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static readonly TomlSerializerOptions AssetDataSerializerOptions = new TomlSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 4,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        private record struct AssetImportData(string? ImporterId, bool IsImported, bool IsSkippedOnImport);

        [InlineArray(32)]
        private struct GuidStringArray
        {
            public char Index0;

            public Span<char> Span => MemoryMarshal.CreateSpan(ref Index0, 32);
        }

        private const int FileCurrentVersion = 1;
    }
}
