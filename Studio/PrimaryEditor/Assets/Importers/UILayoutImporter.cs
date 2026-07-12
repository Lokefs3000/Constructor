using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Loaders;
using PrimaryEditor.Processors.UILayout;

namespace PrimaryEditor.Assets.Importers
{
    internal sealed class UILayoutImporter : IAssetImporter
    {
        public void ImportFile(AssetPipeline pipeline, AssetId id, Stream inputStream, Stream outputStream, string localPath, string localOutputPath, bool isTrialImport)
        {
            pipeline.FilesystemManager.SetFileRemap(localPath, null);

            try
            {
                UILayoutProcessor.Execute(inputStream, outputStream);
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Layout processor encountered an error");
                throw new AssetImportException();
            }

            pipeline.FilesystemManager.SetFileRemap(localPath, localOutputPath);
            pipeline.ReloadAsset(id);
        }

        public void PreloadFile(AssetPipeline pipeline, AssetId id)
        {
            
        }

        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
            using Stream? stream = FilesystemManager.OpenStream(localPath);

            if (stream == null || stream.Length < Unsafe.SizeOf<UILayoutHeader>())
                return false;

            UILayoutHeader header = stream.Read<UILayoutHeader>();

            if (header.FileHeader != UILayoutHeader.Header) return false;
            if (header.FileVersion != UILayoutHeader.Version) return false;

            return true;
        }

        public string UniqueId => "eui_layout";
    }
}
