using CommunityToolkit.HighPerformance;
using Editor.Processors;
using Editor.Shaders;
using Editor.Storage;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Utility;
using System.Runtime.CompilerServices;
using Tomlyn;
using Tomlyn.Model;

namespace Editor.Assets.Importers
{
    internal sealed class ShaderAssetImporter : IAssetImporter
    {
        public ShaderAssetImporter()
        {

        }

        public void Dispose() { }

        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile)
        {
            string localInputFile = fullFilePath.Substring(filesystem.AbsolutePath.Length);

            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localInputFile);

            bool hasLocalConfigFile = false;
            string tomlFile = pipeline.Configuration.GetFilePath(localInputFile, "Shader");
            if (!File.Exists(tomlFile))
            {
                tomlFile = Path.ChangeExtension(fullFilePath, ".toml");
                hasLocalConfigFile = true;
                if (!File.Exists(tomlFile))
                    return false;

                AssetId configId = pipeline.Identifier.GetOrRegisterAsset(Path.ChangeExtension(localInputFile, ".toml"));
                pipeline.Associator.MakeAssocations(id, new ReadOnlySpan<AssetId>(in configId), true);
            }
            else
                pipeline.Associator.ClearAssocations(id);

            TomlTable root = Toml.ToModel<TomlTable>(File.ReadAllText(tomlFile));
            NewShaderProcessorArgs args = new NewShaderProcessorArgs
            {
                SourceFilepath = fullFilePath,
                OutputFilepath = outputFilePath,

                IncludeDirectories = [
                    EditorFilepaths.ContentPath,
                    Path.GetDirectoryName(EditorFilepaths.EditorPath) ?? EditorFilepaths.EditorPath,
                    Path.GetDirectoryName(EditorFilepaths.EnginePath) ?? EditorFilepaths.EnginePath,
                    Path.GetDirectoryName(fullFilePath) ?? string.Empty
                ],

                Logger = EdLog.Assets,

                //HACK: implement dynamic switching here instead
                Targets = Shaders.ShaderCompileTarget.Direct3D12,

                TopologyType = DecodeTopologyType(root),
                Rasterizer = DecodeRasterizer(root),
                DepthStencil = DecodeDepthStencil(root),
                Blend = DecodeBlend(root),
                Blends = DecodeBlends(root)
            };

            Processors.ShaderProcessor processor = new Processors.ShaderProcessor();
            ShaderProcesserResult? resultNullable = processor.Execute(args);
            if (!resultNullable.HasValue)
            {
                return false;
            }

            ShaderProcesserResult result = resultNullable.Value;

            filesystem.RemapFile(localInputFile, localOutputFile);

            if (result.IncludedFiles.Length > 0)
            {
                using RentedArray<AssetId> ids = RentedArray<AssetId>.Rent(result.IncludedFiles.Length);
                int realFiles = 0;

                foreach (string readFile in result.IncludedFiles)
                {
                    AssetId readFileId = pipeline.Identifier.GetOrRegisterAsset(readFile);
                    if (readFileId.IsInvalid)
                        EdLog.Assets.Error("[{s}]: Failed to find or register id for file read by shader: {f}", localInputFile, readFile);
                    else
                        ids[realFiles++] = readFileId;
                }

                if (realFiles > 0)
                    pipeline.Associator.MakeAssocations(id, ids.Span);
                else
                    pipeline.Associator.ClearAssocations(id);
            }
            else
                pipeline.Associator.ClearAssocations(id);

            pipeline.ReloadAsset(id);

            Editor.GlobalSingleton.AssetDatabase.AddEntry<ShaderAsset>(new AssetDatabaseEntry(id, localInputFile, true));
            return false;

            static SBCPrimitiveTopology DecodeTopologyType(TomlTable root) => Enum.Parse<SBCPrimitiveTopology>((string)root["primitive_topology"]);
            static SBCRasterizer DecodeRasterizer(TomlTable root)
            {
                TomlTable rasterizer = (TomlTable)root["rasterizer"];
                return new SBCRasterizer
                {
                    FillMode = Enum.Parse<SBCFillMode>((string)rasterizer["fill_mode"]),
                    CullMode = Enum.Parse<SBCCullMode>((string)rasterizer["cull_mode"]),
                    FrontCounterClockwise = (bool)rasterizer["front_counter_clockwise"],
                    DepthBias = (int)(long)rasterizer["depth_bias"],
                    DepthBiasClamp = (float)(double)rasterizer["depth_bias_clamp"],
                    SlopeScaledDepthBias = (float)(double)rasterizer["slope_scaled_depth_bias"],
                    DepthClipEnable = (bool)rasterizer["depth_clip_enable"],
                    ConservativeRaster = (bool)rasterizer["conservative_raster"],
                };
            }
            static SBCDepthStencil DecodeDepthStencil(TomlTable root)
            {
                TomlTable depthStencil = (TomlTable)root["depth_stencil"];

                TomlTable frontFace = (TomlTable)depthStencil["front_face"];
                TomlTable backFace = (TomlTable)depthStencil["back_face"];
                return new SBCDepthStencil
                {
                    DepthEnable = (bool)depthStencil["depth_enable"],
                    WriteMask = Enum.Parse<SBCDepthWriteMask>((string)depthStencil["depth_write_mask"]),
                    DepthFunc = Enum.Parse<SBCComparisonFunc>((string)depthStencil["depth_func"]),
                    StencilEnable = (bool)depthStencil["stencil_enable"],
                    StencilReadMask = (byte)(long)depthStencil["stencil_read_mask"],
                    StencilWriteMask = (byte)(long)depthStencil["stencil_write_mask"],
                    FrontFace = new SBCDepthStencilFace
                    {
                        Fail = Enum.Parse<SBCStencilOp>((string)frontFace["stencil_fail_op"]),
                        DepthFail = Enum.Parse<SBCStencilOp>((string)frontFace["stencil_depth_fail_op"]),
                        Pass = Enum.Parse<SBCStencilOp>((string)frontFace["stencil_pass_op"]),
                        Func = Enum.Parse<SBCComparisonFunc>((string)frontFace["stencil_func"]),
                    },
                    BackFace = new SBCDepthStencilFace
                    {
                        Fail = Enum.Parse<SBCStencilOp>((string)backFace["stencil_fail_op"]),
                        DepthFail = Enum.Parse<SBCStencilOp>((string)backFace["stencil_depth_fail_op"]),
                        Pass = Enum.Parse<SBCStencilOp>((string)backFace["stencil_pass_op"]),
                        Func = Enum.Parse<SBCComparisonFunc>((string)backFace["stencil_func"]),
                    }
                };
            }
            static SBCBlend DecodeBlend(TomlTable root)
            {
                TomlTable blend = (TomlTable)root["blend"];
                return new SBCBlend
                {
                    AlphaToCoverageEnable = (bool)blend["alpha_to_coverage_enable"],
                    IndependentBlendEnable = (bool)blend["independent_blend_enable"],
                };
            }
            static SBCRenderTargetBlend[] DecodeBlends(TomlTable root)
            {
                TomlTable blend = (TomlTable)root["blend"];
                TomlTableArray rtBlends = (TomlTableArray)blend["rtblends"];

                if (rtBlends.Count == 0)
                    return Array.Empty<SBCRenderTargetBlend>();

                SBCRenderTargetBlend[] array = new SBCRenderTargetBlend[rtBlends.Count];
                for (int i = 0; i < array.Length; i++)
                {
                    TomlTable rtBlend = rtBlends[i];
                    array[i] = new SBCRenderTargetBlend
                    {
                        BlendEnable = (bool)rtBlend["blend_enable"],
                        Source = Enum.Parse<SBCBlendSource>((string)rtBlend["src_blend"]),
                        Destination = Enum.Parse<SBCBlendSource>((string)rtBlend["dst_blend"]),
                        Operation = Enum.Parse<SBCBlendOp>((string)rtBlend["blend_op"]),
                        SourceAlpha = Enum.Parse<SBCBlendSource>((string)rtBlend["src_blend_alpha"]),
                        DestinationAlpha = Enum.Parse<SBCBlendSource>((string)rtBlend["dst_blend_alpha"]),
                        OperationAlpha = Enum.Parse<SBCBlendOp>((string)rtBlend["blend_op_alpha"]),
                        WriteMask = (byte)(long)rtBlend["render_target_write_mask"]
                    };
                }

                return array;
            }
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localFilePath);
            using Stream? stream = filesystem.OpenStream(localFilePath);

            if (stream == null || stream.Length < Unsafe.SizeOf<SBCHeader>())
            {
                Editor.GlobalSingleton.AssetDatabase.AddEntry<ShaderAsset>(new AssetDatabaseEntry(id, localFilePath, false));
                return;
            }

            SBCHeader header = stream.Read<SBCHeader>();
            if (header.Header != SBCHeader.ConstHeader || header.Version != SBCHeader.ConstVersion)
            {
                Editor.GlobalSingleton.AssetDatabase.AddEntry<ShaderAsset>(new AssetDatabaseEntry(id, localFilePath, false));
                return;
            }

            Editor.GlobalSingleton.AssetDatabase.AddEntry<ShaderAsset>(new AssetDatabaseEntry(id, localFilePath, true));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);

            if (stream == null)
                return pipeline.Configuration.DoesFileHaveConfig(localFilePath, "Shader");

            if (stream.Length < Unsafe.SizeOf<SBCHeader>())
                return false;

            SBCHeader header = stream.Read<SBCHeader>();
            if (header.Header != SBCHeader.ConstHeader || header.Version != SBCHeader.ConstVersion)
                return false;

            return true;
        }

        public string? CustomFileIcon => "Editor/Textures/Icons/FileShader2.png";
    }
}
