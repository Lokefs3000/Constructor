using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Editor.Processors.Texture;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Serialization.Toml;
using PrimaryEditor.Assets.Exceptions;
using TerraFX.Interop.Windows;
using Tomlyn;

namespace PrimaryEditor.Assets.Importers
{
    internal sealed class TextureImporter : IAssetImporter
    {
        public void ImportFile(AssetPipeline pipeline, AssetId id, Stream inputStream, Stream outputStream, string localPath, string localOutputPath)
        {
            pipeline.FilesystemManager.SetFileRemap(localPath, null);

            string? configFile = null;
            bool isLocal = false;

            bool isDefaultConfig = false;

            TextureConfiguration config;
            if (localPath.EndsWith(".texcomp"))
            {
                string? sourceText = FilesystemManager.ReadAllText(localPath);
                if (sourceText == null)
                {
                    EdLog.Assets.Error("[{file}]: Failed to read composite texture configuration", localPath);
                    throw new AssetImportException();
                }

                try
                {
                    config = TomlSerializer.Deserialize<CompositeConfiguration>(sourceText, s_tomlOptions)!;
                }
                catch (TomlException ex)
                {
                    EdLog.Assets.Error(ex, "[{file}]: Error occured parsing composite texture configuration", localPath);
                    throw new AssetImportException();
                }

                CompositeConfiguration composite = Unsafe.As<CompositeConfiguration>(config);

                if (composite.CompositeInfo.Channels == TextureCompositeChannel.None)
                {
                    EdLog.Assets.Error("[{file}]: No channels specified for composite texture", localPath);
                    throw new AssetImportException();
                }

                AssetId[] additionalSources = new AssetId[int.PopCount((int)composite.CompositeInfo.Channels)];
                for (int i = 0, j = 0; i < 4; i++)
                {
                    if (composite.CompositeInfo.Channels.HasFlags((TextureCompositeChannel)(1 << i)))
                    {
                        CompositeConfiguration.CompsiteChannel channel = i switch
                        {
                            0 => composite.CompositeInfo.Red,
                            1 => composite.CompositeInfo.Green,
                            2 => composite.CompositeInfo.Blue,
                            3 => composite.CompositeInfo.Alpha,
                            _ => default
                        };

                        if (!pipeline.AssetRegistry.IsIdValid(channel.Asset))
                        {
                            EdLog.Assets.Error("[{file}]: Composite channel '{c}' id is not valid", localPath, (TextureCompositeChannel)(1 << i));
                            throw new AssetImportException();
                        }

                        additionalSources[j++] = channel.Asset;
                    }
                }

                pipeline.Associator.MakeAssociations(id, additionalSources, true);
            }
            else if (localPath.EndsWith(".cubemap"))
            {
                string? sourceText = FilesystemManager.ReadAllText(localPath);
                if (sourceText == null)
                {
                    EdLog.Assets.Error("[{file}]: Failed to read cubemap texture configuration", localPath);
                    throw new AssetImportException();
                }

                try
                {
                    config = TomlSerializer.Deserialize<CubemapConfiguration>(sourceText, s_tomlOptions)!;
                }
                catch (TomlException ex)
                {
                    EdLog.Assets.Error(ex, "[{file}]: Error occured parsing cubemap texture configuration", localPath);
                    throw new AssetImportException();
                }

                CubemapConfiguration cubemap = Unsafe.As<CubemapConfiguration>(config);

                if (cubemap.CubemapInfo.Source == TextureCubemapSource.Composited)
                {
                    CubemapConfiguration.Composited composited = cubemap.CompositedInfo;

                    AssetId[] additionalSources = [composited.PositiveX, composited.NegativeX, composited.PositiveY,
                                                   composited.NegativeY, composited.PositiveZ, composited.NegativeZ];

                    for (int i = 0; i < additionalSources.Length; i++)
                    {
                        if (!pipeline.AssetRegistry.IsIdValid(additionalSources[i]))
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

                            EdLog.Assets.Error("[{file}]: Cubemap face '{f}' id is not valid", localPath, faceName);
                            throw new AssetImportException();
                        }
                    }

                    pipeline.Associator.MakeAssociations(id, additionalSources, true);
                }
            }
            else
            {
                configFile = pipeline.Configuration.GetFilePathOrLocal(localPath, "Texture", out isLocal);
                if (configFile == null)
                {
                    // EdLog.Assets.Error("[{file}]: No configuration file found for texture", localPath);
                    throw new AssetImportException();
                }

                string? sourceText = FilesystemManager.ReadAllText(configFile);
                if (sourceText == null)
                {
                    EdLog.Assets.Error("[{file}]: Failed to read texture configuration", localPath);
                    throw new AssetImportException();
                }

                try
                {
                    config = TomlSerializer.Deserialize<TextureConfiguration>(sourceText, s_tomlOptions)!;
                }
                catch (TomlException ex)
                {
                    EdLog.Assets.Error(ex, "[{file}]: Error occured parsing texture configuration", localPath);
                    throw new AssetImportException();
                }

                isDefaultConfig = true;
            }

            config.DefaultId = id;
            config.IdProvider = pipeline.AssetRegistry;

            outputStream.Write(new TextureHeader());
            outputStream.Write(new TextureSampler());

            ProcessedTextureData textureData;
            try
            {
                textureData = TextureProcessor.Execute(config, outputStream);
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Texture processor encountered an error");
                throw new AssetLoadException();
            }

            outputStream.Seek(0, SeekOrigin.Begin);
            outputStream.Write(new TextureHeader
            {
                FileHeader = TextureHeader.Header,
                FileVersion = TextureHeader.Version,

                Width = (ushort)textureData.Width,
                Height = (ushort)textureData.Height,
                Depth = 1,

                Format = GetFormatToFileEquivalent(textureData.Format),
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

            if (isDefaultConfig)
            {
                if (configFile != null && isLocal)
                {
                    AssetId configFileId = pipeline.AssetRegistry.GetOrRegisterIdFor(configFile);
                    pipeline.Associator.MakeAssociation(id, configFileId);
                }
                else
                {
                    pipeline.Associator.ClearAssociations(id);
                }
            }

            pipeline.FilesystemManager.SetFileRemap(localPath, localOutputPath);
            pipeline.ReloadAsset(id);
        }

        public void PreloadFile(AssetPipeline pipeline, AssetId id)
        {
            
        }

        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
            if (localPath.EndsWith(".texcomp"))
            {
                string? sourceFile = FilesystemManager.ReadAllText(localPath);
                if (sourceFile == null)
                    return false;
                return TomlSerializer.TryDeserialize<CompositeConfiguration>(sourceFile, out _, s_tomlOptions);
            }
            else if (localPath.EndsWith(".cubemap"))
            {
                string? sourceFile = FilesystemManager.ReadAllText(localPath);
                if (sourceFile == null)
                    return false;
                return TomlSerializer.TryDeserialize<CubemapConfiguration>(sourceFile, out _, s_tomlOptions);
            }
            else
            {
                string? configFile = pipeline.Configuration.GetFilePathOrLocal(localPath, "Texture", out bool _);
                if (configFile == null)
                    return false;

                string? sourceFile = FilesystemManager.ReadAllText(configFile);
                if (sourceFile == null)
                    return false;
                if (!TomlSerializer.TryDeserialize<TextureConfiguration>(sourceFile, out _, s_tomlOptions))
                    return false;

                using Stream? stream = FilesystemManager.OpenStream(localPath);

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

        private static TextureFormat GetFormatToFileEquivalent(TextureImageFormat format) => format switch
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
