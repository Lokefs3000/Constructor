using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using Primary.Common;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Loaders;
using PrimaryEditor.Processors.Stylesheet;

namespace PrimaryEditor.Assets.Importers
{
    internal sealed class StylesheetImporter : IAssetImporter
    {
        public void ImportFile(AssetPipeline pipeline, AssetId id, Stream inputStream, Stream outputStream, string localPath, string localOutputPath, bool isTrialImport)
        {
            pipeline.FilesystemManager.SetFileRemap(localPath, null);

            string? sourceText = FilesystemManager.ReadAllText(localPath);
            if (sourceText == null)
            {
                EdLog.Assets.Error("[{file}]: Failed to read stylesheet source", localPath);
                throw new AssetImportException();
            }

            string[] includedFiles = [];
            try
            {
                StylesheetProcessor.Execute(localPath, sourceText, outputStream, out includedFiles);
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Stylesheet processor encountered an error");
                throw new AssetImportException();
            }

            if (includedFiles.Length == 0)
                pipeline.Associator.ClearAssociations(id);
            else
            {
                using RentedArray<AssetId> ids = RentedArray<AssetId>.Rent(includedFiles.Length);
                for (int i = 0; i < includedFiles.Length; ++i)
                {
                    ids[i] = pipeline.GetOrRegisterIdForPath(includedFiles[i]);
                }

                pipeline.Associator.MakeAssociations(id, ids.Span, true);
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

            if (stream == null || stream.Length < Unsafe.SizeOf<StylesheetHeader>())
                return false;

            StylesheetHeader header = stream.Read<StylesheetHeader>();

            if (header.FileHeader != StylesheetHeader.Header) return false;
            if (header.FileVersion != StylesheetHeader.Version) return false;

            return true;
        }

        public string UniqueId => "eui_stylesheet";
    }
}
