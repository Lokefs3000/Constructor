using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using PrimaryEditor.Project;

namespace PrimaryEditor.Assets
{
    public sealed class AssetConfiguration
    {
        private AssetPipeline _pipeline;

        internal AssetConfiguration(AssetPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        public string GetConfigFilePath(string localPath, string keyword)
        {
            AssetId id = _pipeline.AssetRegistry.GetOrRegisterIdFor(localPath);

            string sourcePath = Path.Combine(ProjectData.Instance.Paths.LibraryConfigFolder, $"{id:N}_{keyword}.toml");
            return sourcePath;
        }

        public string? GetFilePathOrLocal(string localPath, string keyword, out bool isLocal)
        {
            AssetId id = _pipeline.AssetRegistry.GetOrRegisterIdFor(localPath);

            string sourcePath = Path.Combine(ProjectData.Instance.Paths.LibraryConfigFolder, $"{id:N}_{keyword}.toml");
            if (!File.Exists(sourcePath))
            {
                sourcePath = Path.ChangeExtension(localPath, ".toml");
                if (!FilesystemManager.TryGetFullPath(sourcePath, out string? fullPath) || !File.Exists(fullPath))
                {
                    isLocal = false;
                    return null;
                }

                isLocal = true;
                return sourcePath;
            }
            else
            {
                isLocal = false;
                return sourcePath;
            }
        }

        public bool DoesFileHaveConfig(string localPath, string keyword, bool allowLocalConfig = true)
        {
            if (FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
                localPath = fullPath;
            else
                return false;

            AssetId id = _pipeline.AssetRegistry.GetOrRegisterIdFor(localPath);

            string sourcePath = Path.Combine(ProjectData.Instance.Paths.LibraryConfigFolder, $"{id:N}_{keyword}.toml");
            if (File.Exists(sourcePath))
                return true;

            return allowLocalConfig && File.Exists(Path.ChangeExtension(localPath, ".toml"));
        }

    }
}
