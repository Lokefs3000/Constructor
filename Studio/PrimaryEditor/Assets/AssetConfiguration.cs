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

        public string? GetConfigPath(string localPath)
        {
            if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
                return null;

            return fullPath + ".assetdat";
        }

        public string? GetLocalConfigPath(string localPath)
        {
            return localPath + ".assetdat";
        }
    }
}
