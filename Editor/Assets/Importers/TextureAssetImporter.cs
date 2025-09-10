using CsToml;
using CsToml.Values;
using Editor.Processors;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

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
            string localInputFile = fullFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length);
            string localOutputFile = outputFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length);

            string configFile = pipeline.Configuration.GetFilePath(localInputFile, "Texture");
            if (!File.Exists(configFile))
            {
                return;
            }

            TextureProcessorArgs args =
                ReadTomlDocument(Toml.ToModel<TomlTable>(File.ReadAllText(configFile), localInputFile, new TomlModelOptions { IncludeFields = true }));

            args.AbsoluteFilepath = fullFilePath;
            args.AbsoluteOutputPath = outputFilePath;

            bool r = new TextureProcessor().Execute(args);

            if (!r)
            {
                EdLog.Assets.Error("Failed to import texture: {local}", fullFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length));
                return;
            }

            Editor.GlobalSingleton.ProjectSubFilesystem.RemapFile(localInputFile, localOutputFile);

            pipeline.ReloadAsset(fullFilePath);
        }

        public static TextureProcessorArgs ReadTomlDocument(TomlTable doc)
        {
            TomlTable root = doc;

            TomlArray swizzleArray = (TomlArray)root["swizzle"];
        
            TextureProcessorArgs args = new TextureProcessorArgs
            {
                ImageType = Enum.Parse<TextureImageType>((string)root["image_type"]),
                ImageMetadata = new TextureProcessorArgs.Metadata(),

                TextureSwizzle = new TextureProcessorArgs.Swizzle(
                    Enum.Parse<TextureSwizzleChannel>((string)swizzleArray[0]!),
                    Enum.Parse<TextureSwizzleChannel>((string)swizzleArray[1]!),
                    Enum.Parse<TextureSwizzleChannel>((string)swizzleArray[2]!),
                    Enum.Parse<TextureSwizzleChannel>((string)swizzleArray[3]!)),
            
                ImageFormat = Enum.Parse<TextureImageFormat>((string)root["image_format"]),
                AlphaSource = Enum.Parse<TextureAlphaSource>((string)root["alpha_source"]),
            
                GammaCorrect = (bool)root["gamma_correct"],
                PremultipliedAlpha = (bool)root["premultiplied_alpha"],
            
                CutoutDither = (bool)root["cutout_dither"],
                CutoutThreshold = (byte)(long)root["cutout_threshold"],
            
                GenerateMipmaps = (bool)root["generate_mipmaps"],
                ScaleAlphaForMipmaps = (bool)root["scale_alpha_for_mipmaps"],
                MaxMipmapCount = (int)(long)root["max_mipmap_count"],
                MinMipmapSize = (int)(long)root["min_mipmap_size"],
                MipmapFilter = Enum.Parse<TextureMipmapFilter>((string)root["mipmap_filter"]),
            
                FlipVertical = (bool)root["flip_vertical"]
            };
            
            switch (args.ImageType)
            {
                case TextureImageType.Normal:
                    {
                        TomlTable node = (TomlTable)root["normal"];
                        ref TextureProcessorNormalArgs normalArgs = ref args.ImageMetadata.NormalArgs;
            
                        normalArgs.Source = Enum.Parse<TextureNormalSource>((string)node["source"]);
                        break;
                    }
                case TextureImageType.Specular:
                    {
                        TomlTable node = (TomlTable)root["specular"];
                        ref TextureProcessorSpecularArgs specularArgs = ref args.ImageMetadata.SpecularArgs;
            
                        specularArgs.Source = Enum.Parse<TextureSpecularSource>((string)node["source"]);
                        break;
                    }
            }
        
            return args;
        }

        public string CustomFileIcon => "Content/Icons/FileTexture.png";

        private const string DefaultTomlContents = @"
flip_vertical = false
image_format = ""BC1""
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
            public TextureImageType ImageType { get; set; }

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
            public bool ScaleAlphaForMipmaps { get; set; }
        }
    }
}
