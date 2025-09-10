using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Assets
{
    public sealed class AssetConfiguration
    {
        private AssetPipeline _pipeline;

        internal AssetConfiguration(AssetPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        /// <summary>Thread-safe</summary>
        public string GetFilePath(string localPath, string keyword)
        {
            AssetId id = _pipeline.Identifier.GetOrRegisterAsset(localPath);

            string sourcePath = Path.Combine(EditorFilepaths.LibraryAssetsPath, $"{id.Id}_{keyword}.dat");
            return sourcePath;
        }
    }
}
