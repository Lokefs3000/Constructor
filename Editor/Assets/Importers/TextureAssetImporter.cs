using Editor.Processors;
using Editor.Storage;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Common;
using Primary.Utility;
using System.Diagnostics;
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

            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localInputFile);
            pipeline.Associator.ClearAssocations(id);

            bool hasLocalConfig = false;
            string configFile = pipeline.Configuration.GetFilePath(localInputFile, "Texture");
            if (!File.Exists(configFile))
            {
                configFile = Path.ChangeExtension(fullFilePath, ".toml");
                hasLocalConfig = true;
                if (!File.Exists(configFile))
                    return false;

                AssetId configId = pipeline.Identifier.GetOrRegisterAsset(Path.ChangeExtension(localInputFile, ".toml"));
                pipeline.Associator.MakeAssocations(id, new ReadOnlySpan<AssetId>(in configId));
            }

            string[]? fileAssociations = Array.Empty<string>();

            TextureProcessorArgs args =
                ReadTomlDocument(Toml.ToModel<TomlTable>(File.ReadAllText(configFile), localInputFile));

            TextureCompositeArgs? compositeArgs = null;
            TextureCubemapArgs? cubemapArgs = null;

            if (fullFilePath.EndsWith(".texcomp"))
            {
                compositeArgs = ReadCompositeDocument(Toml.ToModel<TomlTable>(File.ReadAllText(fullFilePath), localInputFile));
                TextureCompositeArgs comp = compositeArgs.Value;

                args.Sources = new TextureProcessorArgs.Source[int.PopCount((int)comp.Channels & 0xf)];
                AssetId[] additionalSources = new AssetId[args.Sources.Length];

                for (int i = 0, j = 0; i < 4; i++)
                {
                    if (FlagUtility.HasFlag(comp.Channels, (TextureCompositeChannel)(1 << i)))
                    {
                        TextureCompositeChannelArgs channelArgs = default;

                        switch (i)
                        {
                            case 0: channelArgs = comp.Red; break;
                            case 1: channelArgs = comp.Green; break;
                            case 2: channelArgs = comp.Blue; break;
                            case 3: channelArgs = comp.Alpha; break;
                        }

                        string? path = pipeline.Identifier.RetrievePathForId(channelArgs.Asset);
                        if (path == null)
                        {
                            EdLog.Assets.Error("Failed to find asset required for composite texture: {a}", channelArgs.Asset);
                            return false;
                        }

                        ProjectSubFilesystem? newFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(path));
                        Debug.Assert(newFilesystem != null);

                        args.Sources[j] = new TextureProcessorArgs.Source
                        {
                            AbsoluteFilepath = newFilesystem.GetFullPath(path),
                            Channel = channelArgs.Source switch
                            {
                                TextureCompositeChannel.Red => TextureSwizzleChannel.R,
                                TextureCompositeChannel.Green => TextureSwizzleChannel.G,
                                TextureCompositeChannel.Blue => TextureSwizzleChannel.B,
                                TextureCompositeChannel.Alpha => TextureSwizzleChannel.A,
                                _ => TextureSwizzleChannel.R
                            },
                        };
                        additionalSources[j] = channelArgs.Asset;

                        j++;
                    }
                }

                pipeline.Associator.MakeAssocations(id, additionalSources);
            }
            else if (fullFilePath.EndsWith(".cubemap"))
            {
                cubemapArgs = ReadCubemapDocument(Toml.ToModel<TomlTable>(File.ReadAllText(fullFilePath), localInputFile));
                TextureCubemapArgs cubemap = cubemapArgs.Value;

                args.Sources = new TextureProcessorArgs.Source[6];
                AssetId[] additionalSources = [cubemap.PositiveX, cubemap.NegativeX, cubemap.PositiveY,
                                               cubemap.NegativeY, cubemap.PositiveZ, cubemap.NegativeZ];

                for (int i = 0; i < additionalSources.Length; i++)
                {
                    string? path = pipeline.Identifier.RetrievePathForId(additionalSources[i]);
                    if (path == null)
                    {
                        EdLog.Assets.Error("Failed to find asset required for cubemap texture: {a}", additionalSources[i]);
                        return false;
                    }

                    ProjectSubFilesystem? newFilesystem = AssetPipeline.SelectAppropriateFilesystem(AssetPipeline.GetFileNamespace(path));
                    Debug.Assert(newFilesystem != null);

                    additionalSources[i] = additionalSources[i];
                    args.Sources[i] = new TextureProcessorArgs.Source
                    {
                        AbsoluteFilepath = newFilesystem.GetFullPath(path),
                    };
                }

                pipeline.Associator.MakeAssocations(id, additionalSources);
                args.ImageType = TextureImageType.Cubemap;
            }
            else
            {
                args.Sources = [new TextureProcessorArgs.Source { AbsoluteFilepath = fullFilePath }];
            }

            args.Logger = EdLog.Assets;
            args.AbsoluteOutputPath = outputFilePath;

            bool r = new TextureProcessor().Execute(args, compositeArgs, cubemapArgs);

            if (!r)
            {
                EdLog.Assets.Error("Failed to import texture: {local}", fullFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length));
                return false;
            }

            filesystem.RemapFile(localInputFile, localOutputFile);
            pipeline.ReloadAsset(id);

            Editor.GlobalSingleton.AssetDatabase.AddEntry<TextureAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localInputFile), localInputFile, true));
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
            {
                Editor.GlobalSingleton.AssetDatabase.AddEntry<TextureAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, false));
                return;
            }

            Editor.GlobalSingleton.AssetDatabase.AddEntry<TextureAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath, true));
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

        public static TextureCompositeArgs ReadCompositeDocument(TomlTable doc)
        {
            TomlTable root = doc;

            TextureCompositeArgs args = new TextureCompositeArgs
            {
                Channels = (TextureCompositeChannel)(long)root["channels"],
            };

            if (FlagUtility.HasFlag(args.Channels, TextureCompositeChannel.Red))
                args.Red = ReadChannel((TomlTable)root["red"]);
            else
                args.Red = new TextureCompositeChannelArgs();

            if (FlagUtility.HasFlag(args.Channels, TextureCompositeChannel.Green))
                args.Green = ReadChannel((TomlTable)root["green"]);
            else
                args.Green = new TextureCompositeChannelArgs();

            if (FlagUtility.HasFlag(args.Channels, TextureCompositeChannel.Blue))
                args.Blue = ReadChannel((TomlTable)root["blue"]);
            else
                args.Blue = new TextureCompositeChannelArgs();

            if (FlagUtility.HasFlag(args.Channels, TextureCompositeChannel.Alpha))
                args.Alpha = ReadChannel((TomlTable)root["alpha"]);
            else
                args.Alpha = new TextureCompositeChannelArgs();

            return args;

            static TextureCompositeChannelArgs ReadChannel(TomlTable table)
            {
                return new TextureCompositeChannelArgs
                {
                    Asset = (AssetId)(uint)(long)table["asset"],
                    Source = Enum.Parse<TextureCompositeChannel>((string)table["source"]),
                    Invert = (bool)table["invert"]
                };
            }
        }

        public static TextureCubemapArgs ReadCubemapDocument(TomlTable doc)
        {
            TomlTable root = doc;

            return new TextureCubemapArgs
            {
                Source = Enum.Parse<TextureCubemapSource>((string)root["source"]),

                PositiveX = (AssetId)(uint)(long)root["positive_x"],
                PositiveY = (AssetId)(uint)(long)root["positive_y"],
                PositiveZ = (AssetId)(uint)(long)root["positive_z"],

                NegativeX = (AssetId)(uint)(long)root["negative_x"],
                NegativeY = (AssetId)(uint)(long)root["negative_y"],
                NegativeZ = (AssetId)(uint)(long)root["negative_z"],
            };
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
