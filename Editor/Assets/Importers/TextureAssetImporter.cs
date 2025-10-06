using Editor.Processors;
using Editor.Storage;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Utility;
using System.Runtime.CompilerServices;
using Tomlyn;
using Tomlyn.Model;
using TextureSwizzleChannel = Editor.Processors.TextureSwizzleChannel;

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

        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile)
        {
            string localInputFile = fullFilePath.Substring(filesystem.AbsolutePath.Length);

            bool hasLocalConfig = false;
            string configFile = pipeline.Configuration.GetFilePath(localInputFile, "Texture");
            if (!File.Exists(configFile))
            {
                configFile = Path.ChangeExtension(fullFilePath, ".toml");
                hasLocalConfig = true;
                if (!File.Exists(configFile))
                    return false;
            }

            TextureProcessorArgs args =
                ReadTomlDocument(Toml.ToModel<TomlTable>(File.ReadAllText(configFile), localInputFile, new TomlModelOptions { IncludeFields = true }));

            args.AbsoluteFilepath = fullFilePath;
            args.AbsoluteOutputPath = outputFilePath;

            bool r = new TextureProcessor().Execute(args);

            if (!r)
            {
                EdLog.Assets.Error("Failed to import texture: {local}", fullFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length));
                return false;
            }

            filesystem.RemapFile(localInputFile, localOutputFile);

            pipeline.ReloadAsset(pipeline.Identifier.GetOrRegisterAsset(localInputFile));
            if (hasLocalConfig)
            {
                pipeline.MakeFileAssociations(localInputFile, configFile);
            }

            Editor.GlobalSingleton.AssetDatabase.AddEntry<TextureAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localInputFile), localInputFile));
            return true;
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);

            if (stream == null)
            {
                return pipeline.Configuration.DoesFileHaveConfig(localFilePath, "Texture") || filesystem.Exists(Path.ChangeExtension(localFilePath, ".toml"));
            }
            if (stream.Length < Unsafe.SizeOf<TextureHeader>())
                return false;

            using BinaryReader br = new BinaryReader(stream);

            TextureHeader header = br.Read<TextureHeader>();

            if (header.FileHeader != TextureHeader.Header) return false;
            if (header.FileVersion != TextureHeader.Version) return false;

            if (header.Width > 16384) return false;
            if (header.Height > 16384) return false;
            if (header.Depth > 2048) return false;

            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            if (!ValidateFile(localFilePath, filesystem, pipeline))
                return;

            Editor.GlobalSingleton.AssetDatabase.AddEntry<TextureAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath));
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

        public string CustomFileIcon => "Editor/Textures/Icons/FileTexture.png";

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
