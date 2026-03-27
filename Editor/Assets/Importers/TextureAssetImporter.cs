using CommunityToolkit.HighPerformance;
using Editor.Processors.Texture;
using Editor.Storage;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using Primary.Serialization.Toml;
using Primary.Utility;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Tomlyn;

namespace Editor.Assets.Importers
{
    internal class TextureAssetImporter : IAssetImporter
    {
        public TextureAssetImporter()
        {

        }

        public void Dispose()
        {

        }

        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile)
        {
            string localInputFile = fullFilePath.Substring(filesystem.AbsolutePath.Length);
            filesystem.RemapFile(localInputFile, null);

            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localInputFile);

            using Stream? outputStream = FileUtility.TryWaitOpenNoThrow(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            if (outputStream == null)
            {
                throw new HiddenException();
            }

            string? configFile = null;
            bool isLocal = false;

            TextureConfiguration config;
            if (fullFilePath.EndsWith(".texcomp"))
            {
                string? sourceText = filesystem.ReadString(localInputFile);
                if (sourceText == null)
                {
                    EdLog.Assets.Error("[{p}]: Failed to read composite texture configuration", localInputFile);
                    throw new HiddenException();
                }

                try
                {
                    config = TomlSerializer.Deserialize<CompositeConfiguration>(sourceText, s_tomlOptions)
                        ?? throw new NullReferenceException("Deserialize returned null");
                }
                catch (TomlException ex)
                {
                    EdLog.Assets.Error(ex, "[{p}]: Error occured parsing composite texture configuration", localInputFile);
                    throw new HiddenException();
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "[{p}]: Unhandled exception occured parsing compsite texture configuration", localInputFile);
                    throw new HiddenException();
                }

                CompositeConfiguration composite = Unsafe.As<CompositeConfiguration>(config);

                if (composite.CompositeInfo.Channels == TextureCompositeChannel.None)
                {
                    EdLog.Assets.Error("[{p}]: No channels specified for composite texture", localInputFile);
                    throw new HiddenException();
                }

                AssetId[] additionalSources = new AssetId[int.PopCount((int)composite.CompositeInfo.Channels)];
                for (int i = 0, j = 0; i < 4; i++)
                {
                    if (Flags.HasFlag(composite.CompositeInfo.Channels, (TextureCompositeChannel)(1 << i)))
                    {
                        CompositeConfiguration.CompsiteChannel channel = i switch
                        {
                            0 => composite.CompositeInfo.Red,
                            1 => composite.CompositeInfo.Green,
                            2 => composite.CompositeInfo.Blue,
                            3 => composite.CompositeInfo.Alpha,
                            _ => default
                        };

                        if (!pipeline.Identifier.IsIdValid(channel.Asset))
                        {
                            EdLog.Assets.Error("[{p}]: Composite channel id is not valid: {id} ({ch})", channel.Asset, (TextureCompositeChannel)(1 << i));
                            throw new HiddenException();
                        }

                        additionalSources[j++] = channel.Asset;
                    }
                }

                pipeline.Associator.MakeAssocations(id, additionalSources);
            }
            else if (fullFilePath.EndsWith(".cubemap"))
            {
                string? sourceText = filesystem.ReadString(localInputFile);
                if (sourceText == null)
                {
                    EdLog.Assets.Error("[{p}]: Failed to read cubemap texture configuration", localInputFile);
                    throw new HiddenException();
                }

                try
                {
                    config = TomlSerializer.Deserialize<CubemapConfiguration>(sourceText, s_tomlOptions)
                        ?? throw new NullReferenceException("Deserialize returned null");
                }
                catch (TomlException ex)
                {
                    EdLog.Assets.Error(ex, "[{p}]: Error occured parsing cubemap texture configuration", localInputFile);
                    throw new HiddenException();
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "[{p}]: Unhandled exception occured parsing cubemap texture configuration", localInputFile);
                    throw new HiddenException();
                }

                CubemapConfiguration cubemap = Unsafe.As<CubemapConfiguration>(config);

                if (cubemap.CubemapInfo.Source == TextureCubemapSource.Composited)
                {
                    CubemapConfiguration.Composited composited = cubemap.CompositedInfo;

                    AssetId[] additionalSources = [composited.PositiveX, composited.NegativeX, composited.PositiveY,
                                                   composited.NegativeY, composited.PositiveZ, composited.NegativeZ];

                    for (int i = 0; i < additionalSources.Length; ++i)
                    {
                        if (!pipeline.Identifier.IsIdValid(additionalSources[i]))
                        {
                            string faceName = i switch
                            {
                                0 => "X+",
                                1 => "X-",
                                2 => "Y+",
                                3 => "Y-",
                                4 => "Z+",
                                5 => "Z-",
                                _ => string.Empty
                            };

                            EdLog.Assets.Error("[{p}]: Cubemap composited face id is not valid: {id} ({ch})", additionalSources[i], faceName);
                            throw new HiddenException();
                        }
                    }

                    pipeline.Associator.MakeAssocations(id, additionalSources);
                }
            }
            else
            {
                configFile = pipeline.Configuration.GetFilePathOrLocal(localInputFile, "Texture", out isLocal);
                if (configFile == null)
                {
                    return false;
                }

                string? sourceText = filesystem.ReadString(configFile);
                if (sourceText == null)
                {
                    EdLog.Assets.Error("[{p}]: Failed to read texture configuration", localInputFile);
                    throw new HiddenException();
                }

                try
                {
                    config = TomlSerializer.Deserialize<TextureConfiguration>(sourceText, s_tomlOptions)
                        ?? throw new NullReferenceException("Deserialize returned null");
                }
                catch (TomlException ex)
                {
                    EdLog.Assets.Error(ex, "[{p}]: Error occured parsing texture configuration", localInputFile);
                    throw new HiddenException();
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "[{p}]: Unhandled exception occured parsing texture configuration", localInputFile);
                    throw new HiddenException();
                }
            }

            config.DefaultId = id;
            config.IdProvider = pipeline.Identifier;

            outputStream.Write(new TextureHeader());
            outputStream.Write(new TextureSampler());

            ProcessedTextureData textureData = TextureProcessor.Execute(config, outputStream);

            outputStream.Seek(0, SeekOrigin.Begin);
            outputStream.Write(new TextureHeader
            {
                FileHeader = TextureHeader.Header,
                FileVersion = TextureHeader.Version,

                Width = (ushort)textureData.Width,
                Height = (ushort)textureData.Height,
                Depth = 1,

                Format = FormatToFileEquivalent(textureData.Format),
                Flags = (config is CubemapConfiguration) ? TextureFlags.Cubemap : TextureFlags.None,

                MipLevels = (ushort)textureData.MipCount,
                ArraySize = (ushort)textureData.ArraySize
            });

            outputStream.Write(new TextureSampler
            {
                Swizzle = textureData.Swizzle,

                ReductionType = config.VisualInfo.ReductionType,
                MinFilter = config.VisualInfo.MinFilter,
                MagFilter = config.VisualInfo.MagFilter,
                MipFilter = config.VisualInfo.MipFilter,

                AddressModeU = config.VisualInfo.AddressModeU,
                AddressModeV = config.VisualInfo.AddressModeV,
                AddressModeW = config.VisualInfo.AddressModeW,

                ComparisonFunction = config.VisualInfo.ComparisonFunction,

                BorderColor = config.VisualInfo.BorderColor,

                MipLODBias = config.VisualInfo.MipLODBias,
                MinLOD = config.VisualInfo.MinLOD,
                MaxLOD = config.VisualInfo.MaxLOD,

                MaxAnisotropy = (byte)config.VisualInfo.MaxAnisotropy
            });

            {
                if (config is CubemapConfiguration cubemap)
                {
                    using RentedList<AssetId> idList = new RentedList<AssetId>();

                    if (cubemap.CubemapInfo.Source == TextureCubemapSource.Composited)
                    {
                        idList.Add(cubemap.CompositedInfo.PositiveX);
                        idList.Add(cubemap.CompositedInfo.PositiveY);
                        idList.Add(cubemap.CompositedInfo.PositiveZ);
                        idList.Add(cubemap.CompositedInfo.NegativeX);
                        idList.Add(cubemap.CompositedInfo.NegativeY);
                        idList.Add(cubemap.CompositedInfo.NegativeZ);
                    }

                    if (isLocal)
                    {
                        AssetId configId = pipeline.Identifier.GetOrRegisterAsset(configFile);
                        idList.Add(configId);
                    }

                    pipeline.Associator.MakeAssocations(id, idList.AsSpan(), true);
                }
                else if (config is CompositeConfiguration composite)
                {
                    using RentedList<AssetId> idList = new RentedList<AssetId>();

                    if (Flags.HasFlag(composite.CompositeInfo.Channels, TextureCompositeChannel.Red))
                        idList.Add(composite.CompositeInfo.Red.Asset);
                    if (Flags.HasFlag(composite.CompositeInfo.Channels, TextureCompositeChannel.Green))
                        idList.Add(composite.CompositeInfo.Green.Asset);
                    if (Flags.HasFlag(composite.CompositeInfo.Channels, TextureCompositeChannel.Blue))
                        idList.Add(composite.CompositeInfo.Blue.Asset);
                    if (Flags.HasFlag(composite.CompositeInfo.Channels, TextureCompositeChannel.Alpha))
                        idList.Add(composite.CompositeInfo.Alpha.Asset);

                    if (isLocal)
                    {
                        AssetId configId = pipeline.Identifier.GetOrRegisterAsset(configFile);
                        idList.Add(configId);
                    }

                    pipeline.Associator.MakeAssocations(id, idList.AsSpan(), true);
                }
                else
                {
                    if (isLocal)
                    {
                        AssetId configId = pipeline.Identifier.GetOrRegisterAsset(configFile);
                        pipeline.Associator.MakeAssocations(id, new ReadOnlySpan<AssetId>(in configId), true);
                    }
                    else
                        pipeline.Associator.ClearAssocations(id);
                }
            }

            filesystem.RemapFile(localInputFile, localOutputFile);
            pipeline.ReloadAsset(id);

            AssetDatabase database = Editor.GlobalSingleton.AssetDatabase;
            database.AddEntry<TextureAsset>(new AssetDatabaseEntry(id, localInputFile, true));
            
            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            AssetDatabase database = Editor.GlobalSingleton.AssetDatabase;
            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localFilePath);

            database.AddEntry<TextureAsset>(new AssetDatabaseEntry(id, localFilePath, ValidateFile(localFilePath, filesystem, pipeline)));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            if (!filesystem.IsFileRemapped(localFilePath))
                return false;

            if (localFilePath.EndsWith(".texcomp"))
            {
                string? sourceFile = filesystem.ReadString(localFilePath);
                if (sourceFile == null)
                    return false;
                return TomlSerializer.TryDeserialize<CompositeConfiguration>(sourceFile, out _, s_tomlOptions);
            }
            else if (localFilePath.EndsWith(".cubemap"))
            {
                string? sourceFile = filesystem.ReadString(localFilePath);
                if (sourceFile == null)
                    return false;
                return TomlSerializer.TryDeserialize<CubemapConfiguration>(sourceFile, out _, s_tomlOptions);
            }
            else
            {
                using Stream? stream = filesystem.OpenStream(localFilePath);

                if (stream == null || stream.Length < Unsafe.SizeOf<TextureHeader>())
                    return false;

                TextureHeader header = stream.Read<TextureHeader>();

                if (header.FileHeader != TextureHeader.Header) return false;
                if (header.FileVersion != TextureHeader.Version) return false;

                if (header.Width > 16384) return false;
                if (header.Height > 16384) return false;
                if (header.Depth > 2048) return false;

                return true;
            }
        }

        public string CustomFileIcon => "Editor/Textures/Icons/FileTexture.png";

        private static TextureFormat FormatToFileEquivalent(TextureImageFormat format) => format switch
        {
            TextureImageFormat.BC7 => TextureFormat.BC7,
            TextureImageFormat.BC6s => TextureFormat.BC6s,
            TextureImageFormat.BC6u => TextureFormat.BC6u,
            TextureImageFormat.BC5u => TextureFormat.BC5u,
            TextureImageFormat.BC4u => TextureFormat.BC4u,
            TextureImageFormat.BC3 => TextureFormat.BC3,
            TextureImageFormat.BC3n => TextureFormat.BC3n,
            TextureImageFormat.BC2 => TextureFormat.BC2,
            TextureImageFormat.BC1a => TextureFormat.BC1a,
            TextureImageFormat.BC1 => TextureFormat.BC1,
            TextureImageFormat.R8a => TextureFormat.R8a,
            TextureImageFormat.RG8 => TextureFormat.RG8,
            TextureImageFormat.RGB8 => TextureFormat.RGB8,
            TextureImageFormat.RGBA8 => TextureFormat.RGBA8,
            TextureImageFormat.R16 => TextureFormat.R16,
            TextureImageFormat.RG16 => TextureFormat.RG16,
            TextureImageFormat.RGBA16 => TextureFormat.RGBA16,
            TextureImageFormat.R32 => TextureFormat.R32,
            TextureImageFormat.RG32 => TextureFormat.RG32,
            TextureImageFormat.RGBA32 => TextureFormat.RGBA32,
            _ => throw new NotImplementedException(format.ToString()),
        };

        private static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            Converters = [
                new AssetIdTomlConverter(),
                new ColorTomlConverter(),
                new TextureSwizzleConverter()
                ],
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }
}
