using Primary.Assets;

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

            string sourcePath = Path.Combine(EditorFilepaths.LibraryAssetsPath, $"{id}_{keyword}.dat");
            return sourcePath;
        }

        /// <summary>Thread-safe</summary>
        public bool DoesFileHaveConfig(string localPath, string keyword)
        {
            AssetId id = _pipeline.Identifier.GetOrRegisterAsset(localPath);

            string sourcePath = Path.Combine(EditorFilepaths.LibraryAssetsPath, $"{id}_{keyword}.dat");
            return File.Exists(sourcePath);
        }
    }
}
