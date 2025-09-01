using CsToml;
using Editor.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;

namespace Editor.Assets.Importers
{
    internal class TextureAssetImporter : IAssetImporter
    {
        private TextureProcessor _processor;

        public TextureAssetImporter()
        {
            _processor = new TextureProcessor();
        }

        public void Dispose()
        {

        }

        public void Import(AssetPipeline pipeline, string fullFilePath, string outputFilePath)
        {
            string tomlFile = Path.ChangeExtension(fullFilePath, ".toml");
            if (!File.Exists(tomlFile))
            {
                return;
            }

            TextureConfig cfg = Toml.ToModel<TextureConfig>(File.ReadAllText(tomlFile));

            bool r = _processor.Execute(new TextureProcessorArgs
            {
                AbsoluteFilepath = fullFilePath,
                AbsoluteOutputPath = outputFilePath,

                FlipVertical = cfg.FlipVertical,
                ImageFormat = cfg.ImageFormat,
                CutoutDither = cfg.CutoutDither,
                CutoutThreshold = cfg.CutoutThreshold,
                GammaCorrect = cfg.GammaCorrect,
                PremultipliedAlpha = cfg.PremultipliedAlpha,
                MipmapFilter = cfg.MipmapFilter,
                MaxMipmapCount = cfg.MaxMipmapCount,
                MinMipmapSize = cfg.MinMipmapSize,
                GenerateMipmaps = cfg.GenerateMipmaps,
                ImageType = cfg.ImageType,
                ScaleAlphaForMipmaps = cfg.ScaleAlphaForMipmaps,
            });

            if (!r)
            {
                EdLog.Assets.Error("Failed to import texture: {local}", fullFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length));
                return;
            }

            string localInputFile = fullFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length);
            string localOutputFile = outputFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length);

            Editor.GlobalSingleton.ProjectSubFilesystem.RemapFile(localInputFile, localOutputFile);

            pipeline.MakeFileAssociations(fullFilePath, tomlFile);
            pipeline.ReloadAsset(fullFilePath);
        }

        public string CustomFileIcon => "Content/Icons/FileTexture.png";

        private class TextureConfig
        {
            public bool FlipVertical { get; set; }
            public TextureImageFormat ImageFormat { get; set; }
            public bool CutoutDither { get; set; }
            public byte CutoutThreshold { get; set; }
            public bool GammaCorrect { get; set; }
            public bool PremultipliedAlpha { get; set; }
            public TextureMipmapFilter MipmapFilter { get; set; }
            public int MaxMipmapCount { get; set; }
            public int MinMipmapSize { get; set; }
            public bool GenerateMipmaps { get; set; }
            public TextureImageType ImageType { get; set; }
            public bool ScaleAlphaForMipmaps { get; set; }
        }
    }
}
