using Primary.Assets.Types;

namespace Editor.Assets
{
    public sealed class AssetConfiguration
    {
        private AssetPipeline _pipeline;

        internal AssetConfiguration(AssetPipeline pipeline)
        {
            _pipeline = pipeline;

            if (!Directory.Exists(EditorFilepaths.LibraryAssetsPath))
                Directory.CreateDirectory(EditorFilepaths.LibraryAssetsPath);
        }

        /// <summary>Thread-safe</summary>
        public string GetFilePath(string localPath, string keyword)
        {
            AssetId id = _pipeline.Identifier.GetOrRegisterAsset(localPath);

            string sourcePath = Path.Combine(EditorFilepaths.LibraryAssetsPath, $"{id}_{keyword}.toml");
            return sourcePath;
        }

        /// <summary>Thread-safe</summary>
        public string? GetFilePathOrLocal(string localPath, string keyword, out bool isLocal)
        {
            AssetId id = _pipeline.Identifier.GetOrRegisterAsset(localPath);

            string sourcePath = Path.Combine(EditorFilepaths.LibraryAssetsPath, $"{id.ToString("N")}_{keyword}.toml");
            if (!File.Exists(sourcePath))
            {
                sourcePath = Path.ChangeExtension(localPath, ".toml");
                if (!AssetPipeline.TryGetFullPathFromLocal(sourcePath, out string? fullPath) || !File.Exists(fullPath))
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

        /// <summary>Thread-safe</summary>
        public bool DoesFileHaveConfig(string localPath, string keyword, bool allowLocalConfig = true)
        {
            AssetId id = _pipeline.Identifier.GetOrRegisterAsset(localPath);

            string sourcePath = Path.Combine(EditorFilepaths.LibraryAssetsPath, $"{id}_{keyword}.toml");
            if (File.Exists(sourcePath))
                return true;

            return allowLocalConfig && File.Exists(Path.ChangeExtension(localPath, ".toml"));
        }
    }
}
