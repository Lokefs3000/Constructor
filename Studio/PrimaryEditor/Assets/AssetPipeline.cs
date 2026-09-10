using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Primary;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Profiling;
using Primary.Streams;
using PrimaryEditor.Assets.Database;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Filesystem;
using PrimaryEditor.Assets.Importers;
using PrimaryEditor.Assets.Loaders;
using PrimaryEditor.Assets.Serialization;
using PrimaryEditor.Assets.Utility;
using PrimaryEditor.Core;
using PrimaryEditor.Project;
using PrimaryEditor.Startup;
using Tomlyn;

namespace PrimaryEditor.Assets
{
    public sealed class AssetPipeline
    {
        private readonly EditorRuntime? _runtime;

        private readonly bool _printDetailedAssetInfo;

        // primary
        private readonly FilesystemManager _filesystemManager;
        private readonly AssetAssociator _associator;
        private readonly AssetConfiguration _configuration;
        private readonly ImportScheduler _importScheduler;

        // registries
        private readonly ImporterRegistry _importerRegistry;
        private readonly PhysicalFileRegistry _physicalFileRegistry;
        private readonly AssetRegistry _assetRegistry;

        // databases
        private readonly AssetDatabase _database;

        // assets
        private readonly ConcurrentDictionary<FileId, AssetImportData> _importedAssets;

        private readonly Lock _reloadLock;
        private readonly HashSet<AssetId> _assetsToReload;
        private readonly HashSet<AssetId> _pendingImports;

        public AssetPipeline(EditorRuntime? runtime)
        {
            s_instance.Target = this;

            _runtime = runtime;

            _printDetailedAssetInfo = AppArguments.HasArgument("detailed-asset-info");

            _filesystemManager = new FilesystemManager(this);
            _associator = new AssetAssociator();
            _configuration = new AssetConfiguration(this);
            _importScheduler = new ImportScheduler(this);

            _importerRegistry = new ImporterRegistry();
            _physicalFileRegistry = new PhysicalFileRegistry();
            _assetRegistry = new AssetRegistry();

            _database = new AssetDatabase();

            _importedAssets = new ConcurrentDictionary<FileId, AssetImportData>();

            _reloadLock = new Lock();
            _assetsToReload = new HashSet<AssetId>();
            _pendingImports = new HashSet<AssetId>();
        }

        public void ImportAnyChangesLaunch(StartupSplash? splash)
        {
            long setupTimestamp = Stopwatch.GetTimestamp();

            SetupDefaultDirectories();
            MountRequiredFilesystems();
            LoadDefaultData();
            RegisterImporters();

            long assetCheckTimestamp = Stopwatch.GetTimestamp();

            long importWaitTimestamp;
            long saveDataTimestamp;
            if (!AppArguments.HasArgument("--skip-asset-check"))
            {
                splash?.ActionName = "Finding assets..";

                Queue<string> foldersToSearch = new Queue<string>();
                HashSet<FileId> requiredImports = new HashSet<FileId>();
                HashSet<FileId> newAssets = new HashSet<FileId>();

                foreach (BaseFilesystem filesystem in _filesystemManager.Filesystems)
                {
                    if (filesystem is not ContentFilesystem)
                        continue;

                    ContentFilesystem? contentFs = filesystem as ContentFilesystem;

                    foldersToSearch.Clear();
                    foldersToSearch.Enqueue(filesystem.WorkingDirectory);

                    splash?.ActionName = $"Finding assets.. ({filesystem.NamespaceKey})";

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

                                bool hasImporterFor = _importerRegistry.TryGetImporterForPath(localPath, out AssetImporterData importerData);

                                ++searchedFiles;
                                AssetDataConfig? dataConfig;

                                string assetDataPath = entryPathInDir + ".assetdat";
                                if (File.Exists(assetDataPath))
                                {
                                    using Stream? sourceStream = FileUtility.TryWaitOpenNoThrow(assetDataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 5, 60);
                                    if (sourceStream == null)
                                    {
                                        EdLog.Assets.Error("Failed to open asset data file '{f}'! I would recommend that this application be shutdown and the file unlocked before further usage", assetDataPath);
                                        continue;
                                    }

                                    try
                                    {
                                        dataConfig = TomlSerializer.Deserialize<AssetDataConfig>(sourceStream, importerData.TomlOptions ?? s_tomlOptions)!;
                                    }
                                    catch (Exception ex)
                                    {
                                        EdLog.Assets.Error(ex, "Failed to deserialize asset data file '{f}'! Time to close the application and fix whatever is messed up here", assetDataPath);
                                        continue;
                                    }

                                    if (dataConfig.Id.IsInvalid)
                                    {
                                        EdLog.Assets.Error("Invalid id present within the asset data for file '{f}'", localPath);
                                        continue;
                                    }

                                    if (dataConfig.MetaVersion != AssetDataConfig.CurrentVersion)
                                    {
                                        throw new NotSupportedException("Upgrading version not supported yet");
                                    }

                                    ++parsedAssetDatas;
                                }
                                else
                                {
                                    dataConfig = new AssetDataConfig
                                    {
                                        Id = (FileId)Guid.CreateVersion7(),
                                        MetaVersion = AssetDataConfig.CurrentVersion,

                                        SubAssets = [],
                                        UniqueConfig = importerData.DefaultConfig
                                    };

                                    if (!TryWriteAssetDataFor(localPath, dataConfig))
                                    {
                                        continue;
                                    }

                                    ++generatedAssetDatas;
                                    EdLog.Assets.Debug("Created asset data file for '{f}'", localPath);
                                }

                                if (!_importedAssets.TryGetValue(dataConfig.Id, out AssetImportData importData))
                                {
                                    importData = new AssetImportData(dataConfig.Id, null, false, !hasImporterFor);
                                    _importedAssets[dataConfig.Id] = importData;

                                    newAssets.Add(dataConfig.Id);
                                }

                                string? importedPath = _importedAssets.ContainsKey(dataConfig.Id) ? GetLibraryImportPathFor(dataConfig.Id) : null;
                                _assetRegistry.SetupFileWithinRegistryWithId(dataConfig.Id, localPath, importedPath);

                                if (hasImporterFor && importData.ImporterId != null && importerData.Importer.UniqueId != importData.ImporterId)
                                {
                                    EdLog.Assets.Warning("Conflicting importer ids for asset '{p}' ('{c}', '{n}')", localPath, importData.ImporterId, importerData.Importer.UniqueId);
                                }

                                bool shouldTryImport = (importData.IsSkippedOnImport && !_associator.DoesAssetHaveAssociations(dataConfig.Id)) ?
                                    _physicalFileRegistry.IsDataOutOfDate(dataConfig.Id, assetDataPath) :
                                    _physicalFileRegistry.IsFileOrDataOutOfDate(dataConfig.Id, entryPathInDir, assetDataPath)/* || (importData.ImporterId != null && importedPath != null && !IsFileImportedAndValid(importer!, dataHeader.Id, importedPath))*/;

                                if (hasImporterFor && !shouldTryImport && importData.IsImported && !importData.IsSkippedOnImport && importedPath != null)
                                {
                                    try
                                    {
                                        shouldTryImport = !importerData.Importer.ValidateFile(this, dataConfig.Id, importedPath);

                                        if (shouldTryImport && _printDetailedAssetInfo)
                                        {
                                            EdLog.Assets.Information("Asset '{p}' failed asset validation step", localPath);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        EdLog.Assets.Warning(ex, "Error occured trying to validate file '{p}'", localPath);
                                        shouldTryImport = true;
                                    }
                                }

                                if (shouldTryImport)
                                {
                                    if (hasImporterFor)
                                    {
                                        if (requiredImports.Add(dataConfig.Id))
                                        {
                                            ImportDependents(dataConfig.Id);
                                        }
                                    }
                                    else
                                    {
                                        ImportDependents(dataConfig.Id);
                                    }
                                }

                                if (hasImporterFor && (!shouldTryImport || (shouldTryImport && !hasImporterFor)))
                                {
                                    _database.AddEntry(importerData.Importer.AssetDefinitionType, dataConfig.Id);

                                    foreach (SubAssetConfig subAsset in dataConfig.SubAssets)
                                    {
                                        Type? type = Type.GetType(subAsset.AssetType, false);
                                        if (type != null)
                                        {
                                            _database.AddEntry(type, new DatabaseEntry(new AssetId(dataConfig.Id, subAsset.LocalId), subAsset.Name));
                                        }
                                        else
                                        {
                                            EdLog.Assets.Warning("Failed to resolve type '{t}' on asset '{f}'", subAsset.AssetType, localPath);
                                        }
                                    }
                                }

                                // if (_printDetailedAssetInfo && importData.IsSkippedOnImport)
                                // {
                                //     EdLog.Assets.Information("Skipping import for asset '{p}'", localPath);
                                // }
                            }
                        }
                    }

                    EdLog.Assets.Information(@"Asset search done for '{f}' in {s}s
    Searched files/dirs: {sf}/{sd}
    Asset datas parsed/generated: {adp}/{adg}", filesystem.WorkingDirectory, Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds, searchedFiles, searchedDirs, parsedAssetDatas, generatedAssetDatas);
                }

                EdLog.Assets.Information("New assets: {c}", newAssets.Count);
                EdLog.Assets.Information("Scheduling import for {c} total assets", requiredImports.Count);

                splash?.ActionName = $"Scheduling imports.. ({requiredImports.Count})";

                foreach (FileId id in requiredImports)
                {
                    if (_assetRegistry.TryGetLocalPathForId(id, out string? localPath) &&
                        _importerRegistry.TryGetImporterForPath(localPath, out AssetImporterData importerData) &&
                        FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
                    {
                        ScheduleForImport(importerData, id, localPath, fullPath, newAssets.Contains(id));
                    }
                }

                splash?.ActionName = $"Waiting for imports.. ({_importScheduler.ScheduledImports})";
                splash?.ProgressReporter = () => (float)(_importScheduler.FinishedImports / (double)_importScheduler.ScheduledImports);

                importWaitTimestamp = Stopwatch.GetTimestamp();

                _importScheduler.WaitForAllImports();
                _assetsToReload.Clear();

                splash?.ProgressReporter = null;
                saveDataTimestamp = Stopwatch.GetTimestamp();

                void ImportDependents(AssetId id)
                {
                    using Lock.Scope lockScope = _associator.GetAssocationDataWithLockScope(id, out AssociationData associationData, out bool exists);
                    if (exists)
                    {
                        foreach (AssetId dependent in associationData.Dependents)
                        {
                            if (!_associator.IsAssetOnlyActingAsReload(dependent, id) && requiredImports.Add(dependent))
                            {
                                ImportDependents(dependent);
                            }
                        }
                    }
                }
            }
            else
            {
                EdLog.Assets.Information("!!! ASSET CHECK IS DISABLED !!!");

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

            void SetupDefaultDirectories()
            {
                splash?.ActionName = "Ensuring directory structure..";

                if (ProjectData.Instance == null)
                    throw new InvalidOperationException("Project data must have been setup prior to asset pipeline launch");

                string projectRootDir = ProjectData.Instance.Paths.RootFolder;
                Directory.CreateDirectory(Path.Combine(projectRootDir, "Library/Cache"));
                Directory.CreateDirectory(Path.Combine(projectRootDir, "Library/Config"));
                Directory.CreateDirectory(Path.Combine(projectRootDir, "Library/Imported"));
                Directory.CreateDirectory(Path.Combine(projectRootDir, "Library/Log"));
                Directory.CreateDirectory(Path.Combine(projectRootDir, "Library/Saved"));
            }

            void MountRequiredFilesystems()
            {
                splash?.ActionName = "Mounting filesystems..";

                if (ProjectData.Instance == null)
                    throw new InvalidOperationException("Project data must have been setup prior to asset pipeline launch");

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
                splash?.ActionName = "Loading asset data..";

                // _filesystemManager.LoadRemappingsFromDisk();
                _associator.LoadAssocationsFromDisk();

                _physicalFileRegistry.LoadRegistryFromDisk();
                // _assetRegistry.LoadRegistryFromDisk();

                LoadImportedAssetsFromDisk();
            }

            void RegisterImporters()
            {
                splash?.ActionName = "Registering importers..";

                _importerRegistry.RegisterImporter<TextureImporter>(".png", ".jpg", ".jpeg", ".cubemap", ".texcomp");
                _importerRegistry.RegisterImporter<ShaderImporter>(".shader");
                _importerRegistry.RegisterImporter<TextureAtlasImporter>(".atlas");
                _importerRegistry.RegisterImporter<ModelImporter>(".obj", ".fbx", ".gltf", ".glb");
                _importerRegistry.RegisterImporter<MaterialImporter>(".mat");
                _importerRegistry.RegisterImporter<ComputeShaderImporter>(".compute");

                // _importerRegistry.RegisterImporter<UIFontFamilyImporter>(".uifont");
                // _importerRegistry.RegisterImporter<UILayoutImporter>(".layout");
                // _importerRegistry.RegisterImporter<StylesheetImporter>(".style");
            }

            void SaveGeneratedData()
            {
                splash?.ActionName = "Saving asset data..";

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

            Engine? engine = Engine.GlobalSingleton;
            if (engine != null)
            {
                engine.AssetManager.RegisterCustomAsset<UIFontFamilyAsset>(new UIFontFamilyLoader());
                engine.AssetManager.RegisterCustomAsset<UILayoutAsset>(new UILayoutAssetLoader());
                engine.AssetManager.RegisterCustomAsset<StylesheetAsset>(new StylesheetAssetLoader());
            }
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
                    FileId assetId = (FileId)serializer.ReadGuid()!.Value;
                    string? importerId = serializer.ReadString();
                    bool isImported = serializer.ReadBoolean()!.Value;
                    bool isSkippedOnImport = serializer.ReadBoolean()!.Value;

                    serializer.ReadNewLine();

                    _importedAssets[assetId] = new AssetImportData(assetId, importerId, isImported, isSkippedOnImport);
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
        public void HandleUpdates()
        {
            using (new ProfilingScope("UpdateAssets"))
            {
                _importScheduler.UpdateImportStatus();
                _database.FlushPendingUpdates();

                CheckFilesystemUpdates();
                TryImportPending();
                ReloadPending();
            }
        }

        private void CheckFilesystemUpdates()
        {
            bool areAnyUpdatesAvailable = false;
            bool isWithinAnyTimeoutPeriod = false;

            foreach (BaseFilesystem filesystem in _filesystemManager.Filesystems)
            {
                if (filesystem is ContentFilesystem content)
                {
                    if (!areAnyUpdatesAvailable)
                    {
                        areAnyUpdatesAvailable = content.AreFileUpdatesAvailable;
                    }

                    if (areAnyUpdatesAvailable && content.IsWithinTimeoutPeriod)
                    {
                        isWithinAnyTimeoutPeriod = true;
                        break;
                    }
                }
            }

            if (areAnyUpdatesAvailable && !isWithinAnyTimeoutPeriod)
            {
                RentedList<FileEvent> fileEvents = new RentedList<FileEvent>();

                HashSet<string> createdFiles = new HashSet<string>();
                HashSet<string> deletedFiles = new HashSet<string>();
                HashSet<string> changedFiles = new HashSet<string>();

                foreach (BaseFilesystem filesystem in _filesystemManager.Filesystems)
                {
                    if (filesystem is ContentFilesystem content && content.AreFileUpdatesAvailable)
                    {
                        if (content.TryEnterLock())
                        {
                            content.FlushPendingFileEvents(ref fileEvents);
                            content.ExitLock();
                        }

                        if (!fileEvents.IsEmpty)
                        {
                            if (_printDetailedAssetInfo)
                                EdLog.Assets.Information("Processing file events for content filesystem '{p}'", content.NamespaceKey);
                            foreach (FileEvent fileEvent in fileEvents)
                            {
                                if (_printDetailedAssetInfo)
                                    EdLog.Assets.Information("    > {type}: Path:'{p}' NewPath:'{np}' IsDir:{id}", fileEvent.EventType, fileEvent.LocalFilePath, fileEvent.NewLocalFilePath, fileEvent.IsDirectory);

                                switch (fileEvent.EventType)
                                {
                                    case FileEventType.Created:
                                        {
                                            if (fileEvent.IsDirectory)
                                            {
                                                FilesystemDirectory? directory = content.Structure.AddDirectory(fileEvent.LocalFilePath);
                                                if (directory != null)
                                                {
                                                    DiscoverDirectoryFor(content.Structure, directory);
                                                }
                                            }
                                            else
                                            {
                                                createdFiles.Add(fileEvent.LocalFilePath);
                                            }

                                            break;
                                        }
                                    case FileEventType.Deleted:
                                        {
                                            if (fileEvent.IsDirectory)
                                            {
                                                content.Structure.RemoveDirectory(fileEvent.LocalFilePath);
                                            }
                                            else
                                            {
                                                deletedFiles.Add(fileEvent.LocalFilePath);
                                            }

                                            break;
                                        }

                                    case FileEventType.Moved:
                                    case FileEventType.Renamed:
                                        {
                                            if (fileEvent.NewLocalFilePath != null)
                                            {
                                                if (fileEvent.IsDirectory)
                                                {
                                                    content.Structure.RenameDirectory(fileEvent.LocalFilePath, fileEvent.NewLocalFilePath);
                                                }
                                                else if (!fileEvent.LocalFilePath.EndsWith(".assetdat"))
                                                {
                                                    content.Structure.RenameFile(fileEvent.LocalFilePath, fileEvent.NewLocalFilePath);
                                                    if (TryMoveAssetData(fileEvent.LocalFilePath, fileEvent.NewLocalFilePath))
                                                    {
                                                        if (createdFiles.Remove(fileEvent.LocalFilePath))
                                                            createdFiles.Add(fileEvent.NewLocalFilePath);

                                                        if (createdFiles.Remove(fileEvent.LocalFilePath + ".assetdat"))
                                                            createdFiles.Add(fileEvent.NewLocalFilePath + ".assetdat");
                                                    }
                                                }
                                            }

                                            break;
                                        }

                                    case FileEventType.Changed:
                                        {
                                            if (!fileEvent.IsDirectory)
                                            {
                                                changedFiles.Add(fileEvent.LocalFilePath);
                                            }

                                            break;
                                        }
                                }
                            }

                            fileEvents.Clear();
                        }
                    }
                }

                if (createdFiles.Count > 0)
                {
                    foreach (string localPath in createdFiles)
                    {
                        if (!localPath.EndsWith(".assetdat") && FilesystemManager.TryGetFullPath(localPath + ".assetdat", out string? newFullPath))
                        {
                            if (TryCreateAssetDataFor(localPath, out AssetDataConfig? dataConfig, out bool exists))
                            {
                                if (exists)
                                {
                                    if (_assetRegistry.TryGetLocalPathForId(dataConfig.Id, out string? oldLocalPath) &&
                                        FilesystemManager.TryGetFullPath(oldLocalPath, out string? oldFullPath))
                                    {
                                        deletedFiles.Remove(oldLocalPath);
                                        FileUtility.TryMove(oldFullPath, newFullPath + ".assetdat");
                                    }

                                    _assetRegistry.UpdateLocalPath(dataConfig.Id, localPath);
                                    _physicalFileRegistry.StoreFileData(dataConfig.Id, newFullPath, newFullPath + ".assetdat");
                                }
                                else if (_importerRegistry.TryGetImporterForPath(localPath, out _))
                                {
                                    changedFiles.Add(localPath);
                                }
                            }
                        }
                    }
                }

                if (deletedFiles.Count > 0)
                {
                    foreach (string localPath in deletedFiles)
                    {
                        if (localPath.EndsWith(".assetdat"))
                        {
                            ReadOnlySpan<char> orignalLocalPath = localPath.AsSpan(0, localPath.Length - ".assetdat".Length);
                            if (_assetRegistry.TryLookupIdForPath(orignalLocalPath, out FileId id))
                            {
                                _assetRegistry.RemoveAssetFromRegistry(id);
                                _physicalFileRegistry.RemoveFileFromRegistry(id);

                                RemoveImportedAsset(id);
                            }
                        }
                        else if (TryReadAssetDataFor(localPath, out AssetDataConfig? dataConfig))
                        {
                            if (FilesystemManager.TryGetFullPath(localPath + ".assetdat", out string? fullAssetDatPath))
                                FileUtility.TryDelete(fullAssetDatPath);

                            _assetRegistry.RemoveAssetFromRegistry(dataConfig.Id);
                            _physicalFileRegistry.RemoveFileFromRegistry(dataConfig.Id);

                            RemoveImportedAsset(dataConfig.Id);
                        }
                    }
                }

                if (changedFiles.Count > 0)
                {
                    foreach (string localPath in changedFiles)
                    {
                        ReadOnlySpan<char> orignalLocalPath;
                        if (localPath.EndsWith(".assetdat"))
                            orignalLocalPath = localPath.AsSpan(0, localPath.Length - ".assetdat".Length);
                        else
                            orignalLocalPath = localPath;

                        if (_assetRegistry.TryLookupIdForPath(orignalLocalPath, out FileId id))
                        {
                            TryImportChangedAsset(orignalLocalPath.Length == localPath.Length ? localPath : orignalLocalPath.ToString());
                        }
                    }
                }

                fileEvents.Dispose();
            }
        }

        private void DiscoverDirectoryFor(FilesystemStructure structure, FilesystemDirectory directory)
        {
            if (FilesystemManager.TryFindFilesystemFor(directory.LocalPath, out BaseFilesystem? filesystem) &&
                filesystem.TryGetFullPath(directory.LocalPath, out string? fullPath))
            {
                if (_printDetailedAssetInfo)
                    EdLog.Assets.Information("Discovering files for structure in directory '{dir}'", directory.LocalPath);

                foreach (string fileSystemEntry in Directory.EnumerateFileSystemEntries(directory.LocalPath, "*.*", SearchOption.AllDirectories))
                {
                    if (filesystem.TryGetLocalPath(fileSystemEntry, out string? localPath))
                    {
                        if (Directory.Exists(fileSystemEntry))
                            structure.AddDirectory(localPath);
                        else
                            structure.AddFile(localPath);
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

                    if (_importerRegistry.TryGetImporterForPath(localPath, out AssetImporterData importerData))
                    {
                        if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
                        {
                            EdLog.Assets.Warning("Failed to schedule import for '{p}' because the full path could not be found", localPath);
                            continue;
                        }

                        ScheduleForImport(importerData, id, localPath, fullPath, false);
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
                Engine? engine = Engine.GlobalSingleton;
                if (engine != null)
                {
                    using (_reloadLock.EnterScope())
                    {
                        using RentedArray<AssetId> currentAssets = new RentedArray<AssetId>(_assetsToReload.Count);

                        int index = 0;
                        foreach (AssetId id in _assetsToReload)
                        {
                            currentAssets[index++] = id;
                        }

                        _assetsToReload.Clear();
                        EdLog.Assets.Information("Reloading #{c} assets", currentAssets.Count);

                        foreach (AssetId id in currentAssets)
                        {
                            engine.AssetManager.ForceReloadAsset(id);

                            using (_associator.GetAssocationDataWithLockScope(id, out AssociationData associationData, out bool exists))
                            {
                                if (exists)
                                {
                                    foreach (AssetId dependent in associationData.Dependents)
                                    {
                                        if (_associator.IsAssetOnlyActingAsReload(id, dependent))
                                            _assetsToReload.Add(dependent);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    _assetsToReload.Clear();
                }
            }
        }

        private bool TryCreateAssetDataFor(string localPath, [NotNullWhen(true)] out AssetDataConfig? dataConfig, out bool exists)
        {
            dataConfig = null;
            exists = false;

            if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
            {
                EdLog.Assets.Warning("Failed to get full path for '{p}' to generate asset data file", localPath);
                return false;
            }

            _importerRegistry.TryGetImporterForPath(localPath, out AssetImporterData importerData);

            string assetDataPath = localPath.EndsWith(".assetdat") ? fullPath : fullPath + ".assetdat";
            if (File.Exists(assetDataPath))
            {
                try
                {
                    using Stream? sourceStream = FileUtility.TryWaitOpenNoThrow(assetDataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 5, 60);
                    if (sourceStream == null)
                    {
                        EdLog.Assets.Error("Failed to open asset data file '{f}'", assetDataPath);
                        throw new AssetIgnoredException();
                    }

                    try
                    {
                        dataConfig = TomlSerializer.Deserialize<AssetDataConfig>(sourceStream, importerData.TomlOptions ?? s_tomlOptions)!;
                    }
                    catch (Exception ex)
                    {
                        EdLog.Assets.Error(ex, "Failed to deserialize asset data file '{f}'", assetDataPath);
                        throw new AssetIgnoredException();
                    }

                    if (!_assetRegistry.IsIdValid(dataConfig.Id))
                    {
                        EdLog.Assets.Error("Id in asset data '{f}' is not valid", assetDataPath);
                        throw new AssetIgnoredException();
                    }

                    if (dataConfig.MetaVersion != AssetDataConfig.CurrentVersion)
                    {
                        throw new NotSupportedException("Upgrading version not supported yet");
                    }

                    exists = true;
                    return true;
                }
                catch (AssetIgnoredException)
                {
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "Exception raised trying to read asset data file '{p}'", assetDataPath);
                }
            }

            dataConfig = new AssetDataConfig
            {
                Id = _assetRegistry.SetupFileWithinRegistry(localPath, null),
                MetaVersion = AssetDataConfig.CurrentVersion,

                SubAssets = [],
                UniqueConfig = importerData.DefaultConfig
            };

            if (!TryWriteAssetDataFor(localPath, dataConfig))
            {
                EdLog.Assets.Error("Failed to write asset data for local path '{f}'", localPath);
                return false;
            }

            _physicalFileRegistry.StoreFileData(dataConfig.Id, fullPath, assetDataPath);

            if (_printDetailedAssetInfo)
                EdLog.Assets.Information("Created asset data file for '{f}' ({id})", localPath, dataConfig.Id);

            return true;
        }

        private bool TryReadAssetDataFor(string localPath, [NotNullWhen(true)] out AssetDataConfig? dataConfig)
        {
            dataConfig = default;

            if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
            {
                EdLog.Assets.Warning("Failed to get full path for '{p}' to generate asset data file", localPath);
                return false;
            }

            _importerRegistry.TryGetImporterForPath(localPath, out AssetImporterData importerData);

            string assetDataPath = localPath.EndsWith(".assetdat") ? fullPath : fullPath + ".assetdat";
            if (File.Exists(assetDataPath))
            {
                try
                {
                    using Stream? sourceStream = FileUtility.TryWaitOpenNoThrow(assetDataPath, FileMode.Open, FileAccess.Read, FileShare.Read, 5, 60);
                    if (sourceStream == null)
                    {
                        EdLog.Assets.Error("Failed to open asset data file '{f}'", assetDataPath);
                        throw new AssetIgnoredException();
                    }

                    try
                    {
                        dataConfig = TomlSerializer.Deserialize<AssetDataConfig>(sourceStream, importerData.TomlOptions ?? s_tomlOptions)!;
                    }
                    catch (Exception ex)
                    {
                        EdLog.Assets.Error(ex, "Failed to deserialize asset data file '{f}'", assetDataPath);
                        return false;
                    }

                    if (!_assetRegistry.IsIdValid(dataConfig.Id))
                    {
                        EdLog.Assets.Error("Id in asset data '{f}' is not valid", assetDataPath);
                        throw new AssetIgnoredException();
                    }

                    if (dataConfig.MetaVersion != AssetDataConfig.CurrentVersion)
                    {
                        throw new NotSupportedException("Upgrading version not supported yet");
                    }

                    return true;
                }
                catch (AssetIgnoredException)
                {
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "Exception raised trying to read asset data file '{p}'", assetDataPath);
                }
            }

            return false;
        }

        private bool TryWriteAssetDataFor(string localPath, AssetDataConfig dataConfig)
        {
            if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
            {
                EdLog.Assets.Warning("Failed to get full path for '{p}' to generate asset data file", localPath);
                return false;
            }

            _importerRegistry.TryGetImporterForPath(localPath, out AssetImporterData importerData);

            try
            {
                using Stream stream = FileUtility.TryWaitOpen(fullPath + ".assetdat", FileMode.Create, FileAccess.Write, FileShare.None);
                TomlSerializer.Serialize(stream, dataConfig, importerData.TomlOptions ?? s_tomlOptions);
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Failed to serialize asset data to disk for '{f}'", localPath);
                return false;
            }

            return true;
        }

        private bool TryMoveAssetData(string oldLocalPath, string newLocalPath)
        {
            if (!FilesystemManager.TryGetFullPath(oldLocalPath, out string? fullPath))
            {
                EdLog.Assets.Warning("Failed to get full path for '{p}' to rename asset data file", oldLocalPath);
                return false;
            }

            if (!FilesystemManager.TryGetFullPath(newLocalPath, out string? newFullPath))
            {
                EdLog.Assets.Warning("Failed to get full path for '{p}' to rename asset data file", newLocalPath);
                return false;
            }

            string assetDataPath = Path.Combine(fullPath);
            string newAssetDataPath = newFullPath + ".assetdat";

            if (_assetRegistry.TryLookupIdForPath(oldLocalPath, out FileId id) && FileUtility.TryMove(assetDataPath, newAssetDataPath))
            {
                _assetRegistry.UpdateLocalPath(id, newLocalPath);
                _physicalFileRegistry.StoreFileData(id, newFullPath, newAssetDataPath);

                return true;
            }
            else
            {
                EdLog.Assets.Warning("Failed to move asset data path from '{f}' to '{t}'", assetDataPath, newAssetDataPath);
            }

            return false;
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
            if (_importerRegistry.TryGetImporterForPath(localPath, out AssetImporterData importerData))
            {
                _pendingImports.Add(id);
            }

            using Lock.Scope lockScope = _associator.GetAssocationDataWithLockScope(id, out AssociationData associationData, out bool exists);
            if (exists)
            {
                foreach (AssetId dependent in associationData.Dependents)
                {
                    if (!_associator.IsAssetOnlyActingAsReload(id, dependent) && _assetRegistry.TryGetLocalPathForId(dependent, out string? dependentLocalPath))
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

        private void ScheduleForImport(AssetImporterData importerData, AssetId id, string localPath, string fullPath, bool isTrialImport)
        {
            Action action = () =>
            {
                DateTime startTime = DateTime.Now;

                if (!TryReadAssetDataFor(localPath, out AssetDataConfig? dataConfig))
                {
                    EdLog.Assets.Error("[{file}]: Failed to read asset data config", localPath);
                    return;
                }

                using FileStream? inputStream = FileUtility.TryWaitOpenNoThrow(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (inputStream == null)
                {
                    EdLog.Assets.Error("[{file}]: Failed to open input stream", localPath);
                    return;
                }

                string localOutputPath;

                {
                    GuidStringArray stringArray = default;
                    id.FileId.TryFormat(stringArray.Span, out int _);

                    string prefixDir = Path.Combine(ProjectData.Instance!.Paths.LibraryImportedFolder, stringArray.Span[30..].ToString());
                    if (!Directory.Exists(prefixDir))
                        Directory.CreateDirectory(prefixDir);

                    localOutputPath = GetLibraryImportPathFor(stringArray.Span);
                }

                string outputFilePath = Path.Combine(ProjectData.Instance.Paths.RootFolder, localOutputPath);

                PooledMemoryStream outputStream = new PooledMemoryStream();

                try
                {
                    ImportContext context = new ImportContext(this, id, localPath, localOutputPath, isTrialImport, inputStream, dataConfig, outputStream);
                    importerData.Importer.ImportFile(in context);

                    _associator.ClearAssociations(id);
                    if (context.Dependencies.Count > 0)
                        _associator.MakeAssociations(id, context.Dependencies);
                    if (context.ReloadConnections.Count > 0)
                        _associator.MakeAssociations(id, context.ReloadConnections, actAsReloadFlag: true);

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

                    bool doSubAssetsStillMatch = dataConfig.SubAssets.Length == context.SubAssets.Count;
                    if (doSubAssetsStillMatch)
                    {
                        context.SortSubAssets();
                        dataConfig.SubAssets.Sort(static (x, y) => x.LocalId.CompareTo(y.LocalId));

                        for (int i = 0; i < dataConfig.SubAssets.Length; ++i)
                        {
                            SubAssetConfig subAsset = dataConfig.SubAssets[i];
                            (Type type, int localId, string? name) = context.SubAssets[i];

                            if (subAsset.AssetType != type.AssemblyQualifiedName || subAsset.LocalId != localId || subAsset.Name != name)
                            {
                                doSubAssetsStillMatch = false;
                                break;
                            }
                        }
                    }

                    (Type Type, int LocalId, string? Name)[]? removedAssets = null;
                    if (!doSubAssetsStillMatch)
                    {
                        RegenerateDataConfigNewSubAssets(localPath, dataConfig, context.SubAssets.AsSpan(), out removedAssets);
                    }

                    ReportImportAsFinished(id, importerData, removedAssets, context);
                }
                catch (AssetIgnoredException)
                {
                    if (File.Exists(outputFilePath))
                        File.Delete(outputFilePath);

                    outputStream.SetLength(0);

                    ReportImportAsIgnored(id, importerData);
                    return;
                }
                catch (Exception ex)
                {
                    if (ex is not AssetImportException)
                    {
                        EdLog.Assets.Error(ex, "[{file}]: An unexpected error occured while trying to import", localPath);
                    }
                    else
                    {
                        EdLog.Assets.Error(ex, "[{file}]: An exception occured while trying to import", localPath);
                    }

                    if (File.Exists(outputFilePath))
                        File.Delete(outputFilePath);

                    ReportImportAsFailed(id, importerData, dataConfig);
                    return;
                }

                outputStream.Dispose();

                EdLog.Assets.Debug("Import '{i}' finished in {sc}s", localPath, (DateTime.Now - startTime).TotalSeconds);
            };

            //EdLog.Assets.Debug("Scheduling import for asset '{a}'", localPath);
            _importScheduler.ScheduleImport(action, id);
        }

        private void RegenerateDataConfigNewSubAssets(string localPath, AssetDataConfig dataConfig, ReadOnlySpan<(Type Type, int LocalId, string? Name)> currentSubAssets, out (Type Type, int LocalId, string? Name)[]? removedAssets)
        {
            removedAssets = null;

            if (currentSubAssets.IsEmpty)
            {
                using RentedList<(Type Type, int LocalId, string? Name)> list = new RentedList<(Type Type, int LocalId, string? Name)>();
                for (int i = 0; i < dataConfig.SubAssets.Length; ++i)
                {
                    SubAssetConfig subAsset = dataConfig.SubAssets[i];

                    Type? type = Type.GetType(subAsset.AssetType);
                    if (type != null)
                    {
                        list.Add((type, subAsset.LocalId, subAsset.Name));
                    }
                }

                if (!list.IsEmpty)
                    removedAssets = [.. list];

                dataConfig.SubAssets = [];
            }
            else
            {
                dataConfig.SubAssets = dataConfig.SubAssets.Length == currentSubAssets.Length ? dataConfig.SubAssets : new SubAssetConfig[currentSubAssets.Length];

                using RentedList<(Type Type, int LocalId, string? Name)> list = new RentedList<(Type Type, int LocalId, string? Name)>();
                for (int i = 0; i < dataConfig.SubAssets.Length; ++i)
                {
                    ref SubAssetConfig subAsset = ref dataConfig.SubAssets[i];
                    (Type Type, int LocalId, string? Name) currentSubAsset = currentSubAssets[i];

                    if (subAsset == null || (subAsset.AssetType != currentSubAsset.Type.AssemblyQualifiedName || subAsset.LocalId != currentSubAsset.LocalId || subAsset.Name != currentSubAsset.Name))
                    {
                        if (subAsset != null)
                        {
                            Type? type = Type.GetType(subAsset.AssetType);
                            if (type != null)
                            {
                                list.Add((type, subAsset.LocalId, subAsset.Name));
                            }

                        }
                        else
                        {
                            subAsset = new SubAssetConfig();
                        }

                        subAsset.AssetType = currentSubAsset.Type.AssemblyQualifiedName!;
                        subAsset.LocalId = currentSubAsset.LocalId;
                        subAsset.Name = currentSubAsset.Name ?? string.Empty;
                    }
                }

                if (!list.IsEmpty)
                    removedAssets = [.. list];
            }

            TryWriteAssetDataFor(localPath, dataConfig);
        }

        internal void ReportImportAsFinished(AssetId id, AssetImporterData importerData, (Type Type, int LocalId, string? Name)[]? removedAssets, ImportContext context)
        {
            ReloadAsset(id);

            // foreach ((_, int localId, _) in context.SubAssets)
            // {
            //     ReloadAsset(id.WithLocalId(localId));
            // }

            if (!_assetRegistry.TryGetAssetPathForId(id, out string? localPath))
                return;

            if (!_importedAssets.TryGetValue(id, out AssetImportData importData) || importData.ImporterId != importerData.Importer.UniqueId || !importData.IsImported || importData.IsSkippedOnImport)
            {
                importData.ImporterId = importerData.Importer.UniqueId;
                importData.IsImported = true;
                importData.IsSkippedOnImport = false;

                _importedAssets[id] = importData;
            }

            _database.AddEntry(importerData.Importer.AssetDefinitionType, id);

            if (removedAssets != null)
            {
                foreach ((Type type, int localId, string? name) in removedAssets)
                {
                    _database.RemoveEntry(type, new DatabaseEntry(new AssetId(context.Id, localId), name));
                }
            }

            foreach ((Type type, int localId, string? name) in context.SubAssets)
            {
                _database.AddEntry(type, new DatabaseEntry(new AssetId(context.Id, localId), name));
            }
        }

        internal void ReportImportAsFailed(AssetId id, AssetImporterData importerData, AssetDataConfig dataConfig)
        {
            if (_importedAssets.TryGetValue(id, out AssetImportData importData))
            {
                importData.IsImported = false;
                _importedAssets[id] = importData;

                _assetRegistry.RemoveAssetPath(id);

                FileUtility.TryDelete(GetLibraryImportPathFor(id));
                _database.RemoveEntry(importerData.Importer.AssetDefinitionType, id);

                foreach (SubAssetConfig subAsset in dataConfig.SubAssets)
                {
                    Type? type = Type.GetType(subAsset.AssetType, false);
                    if (type != null)
                    {
                        _database.RemoveEntry(type, new AssetId(dataConfig.Id, subAsset.LocalId));
                    }
                }
            }
        }

        internal void ReportImportAsIgnored(AssetId id, AssetImporterData importerData)
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
        internal FileId GetOrRegisterIdForPath(string localPath)
        {
            // assetdat files cannot have an id
            if (localPath.EndsWith(".assetdat"))
                return AssetId.Invalid;

            if (!_assetRegistry.TryLookupIdForPath(localPath, out FileId id))
            {
                // This in return also ends up doing a check for if the path is local
                if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
                    return FileId.Invalid;

                // No logic in creating an id for a file that doesn't exist
                if (!File.Exists(fullPath))
                    return FileId.Invalid;

                string dataFilePath = fullPath + ".assetdat";

                FileId assetId = _assetRegistry.SetupFileWithinRegistry(localPath, null);
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

        public AssetDatabase Database => _database;

        private static string GetLibraryImportPathFor(AssetId id)
        {
            Span<char> formatBuffer = stackalloc char[32];
            id.FileId.TryFormat(formatBuffer, out int _);
            return GetLibraryImportPathFor(formatBuffer);
        }

        private static string GetLibraryImportPathFor(ReadOnlySpan<char> id)
        {
            Debug.Assert(id.Length == 32);
            return $"Library/Imported/{id[30..]}/{id[..30]}";
        }

        private static string s_importedDataPath => Path.Combine(ProjectData.Instance!.Paths.LibrarySavedFolder, "ImportedFiles.dat");
        private static readonly JsonSerializerOptions s_options = new JsonSerializerOptions
        {
            WriteIndented = EditorRuntime.IsDebugBuild,
            IndentSize = 4,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 4,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = [
                new EarlyFileIdTomlConverter(),
                new EarlyAssetIdTomlConverter()
                ]
        };

        private static readonly WeakReference s_instance = new WeakReference(null);

        public static AssetPipeline? Instance => Unsafe.As<AssetPipeline>(s_instance.Target);

        private record struct AssetImportData(AssetId Id, string? ImporterId, bool IsImported, bool IsSkippedOnImport);

        [InlineArray(32)]
        private struct GuidStringArray
        {
            public char Index0;

            public Span<char> Span => MemoryMarshal.CreateSpan(ref Index0, 32);
        }

        private const int FileCurrentVersion = 1;
    }
}
