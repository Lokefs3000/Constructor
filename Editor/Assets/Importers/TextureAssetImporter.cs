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
        public TextureAssetImporter()
        {
            TextureProcessor.Logger = EdLog.Assets;
        }

        public void Dispose()
        {

        }

        public void Import(AssetPipeline pipeline, string fullFilePath, string outputFilePath)
        {
            string tomlFile = Path.ChangeExtension(fullFilePath, ".toml");
            if (!File.Exists(tomlFile))
            {
                File.WriteAllText(tomlFile, DefaultTomlContents);
            }

            TextureConfig cfg = Toml.ToModel<TextureConfig>(File.ReadAllText(tomlFile));
            if (!cfg.ImportFile)
            {
                return;
            }

            bool r = new TextureProcessor().Execute(new TextureProcessorArgs
            {
                AbsoluteFilepath = fullFilePath,
                AbsoluteOutputPath = outputFilePath,

                FlipVertical = !cfg.FlipVertical, //directx
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

        private const string DefaultTomlContents = @"
flip_vertical = false
image_format = ""BC3""
cutout_dither = false
cutout_threshold = 127
gamma_correct = false
premultiplied_alpha = false
mipmap_filter = ""Box""
max_mipmap_count = 2147483647
min_mipmap_size = 1
generate_mipmaps = false
image_type = ""Colormap""
scale_alpha_for_mipmaps = false
";

        private class TextureConfig
        {
            public bool ImportFile { get; set; } = true;

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
