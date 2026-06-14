using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PrimaryEditor.Startup;

namespace PrimaryEditor.Assets
{
    public sealed class AssetPipeline
    {
        // primary
        private FilesystemManager _filesystemManager;

        // registries
        private ImporterRegistry _importerRegistry;
        private PhysicalFileRegistry _physicalFileRegistry;
        private AssetRegistry _assetRegistry;

        internal AssetPipeline()
        {
            _filesystemManager = new FilesystemManager(this);

            _importerRegistry = new ImporterRegistry();
            _physicalFileRegistry = new PhysicalFileRegistry();
            _assetRegistry = new AssetRegistry();
        }

        internal void ImportAnyChangesLaunch(StartupSplash splash)
        {
            MountRequiredFilesystems();
            LoadDefaultData();

            splash.ActionName = "Checking for asset changes..";

            while (true)
            {
            }

            void MountRequiredFilesystems()
            {
                splash.ActionName = "Mounting filesystems..";

                JsonArray mounts = JsonSerializer.Deserialize<JsonArray>(File.ReadAllText("Filesystems.json"))!;
                foreach (JsonObject obj in mounts!)
                {
                    _filesystemManager.Mount((string)obj["Path"]!, (string)obj["NamespaceKey"]!);
                }
            }

            void LoadDefaultData()
            {
                splash.ActionName = "Loading asset data..";

                _physicalFileRegistry.LoadRegistryFromDisk();
                _assetRegistry.LoadRegistryFromDisk();
            }
        }

        public FilesystemManager FilesystemManager => _filesystemManager;

        public ImporterRegistry ImporterRegistry => _importerRegistry;
        public PhysicalFileRegistry PhysicalFileRegistry => _physicalFileRegistry;
        public AssetRegistry AssetRegistry => _assetRegistry;
    }
}
