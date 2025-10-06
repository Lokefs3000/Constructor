using Editor.Processors;
using Editor.Storage;
using Primary.Assets;
using Tomlyn;
using Tomlyn.Model;
using RHI = Primary.RHI;

namespace Editor.Assets.Importers
{
    internal class ShaderAssetImporter : IAssetImporter
    {
        private ShaderProcessor _processor;

        public ShaderAssetImporter()
        {
            _processor = new ShaderProcessor();
        }

        public void Dispose()
        {

        }

        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile)
        {
            string localInputFile = fullFilePath.Substring(filesystem.AbsolutePath.Length);

            bool hasLocalConfigFile = false;
            string tomlFile = pipeline.Configuration.GetFilePath(localInputFile, "Shader");
            if (!File.Exists(tomlFile))
            {
                tomlFile = Path.ChangeExtension(fullFilePath, ".toml");
                hasLocalConfigFile = true;
                if (!File.Exists(tomlFile))
                    return false;
            }

            TomlTable root = Toml.ToModel<TomlTable>(File.ReadAllText(tomlFile));
            ShaderProcessorArgs args = new ShaderProcessorArgs
            {
                AbsoluteFilepath = fullFilePath,
                AbsoluteOutputPath = outputFilePath,

                ContentSearchDir = EditorFilepaths.ContentPath,

                Logger = EdLog.Assets,

                Target = RHI.GraphicsAPI.Direct3D12,

                Description = DecodeDescription(root),
                Blends = !root.ContainsKey("blends") ? Array.Empty<BlendDescriptionArgs>() : DecodeBlends((TomlTableArray)root["blends"])
            };

            ShaderProcessor processor = new ShaderProcessor();

            bool r = processor.Execute(args);

            if (!r)
            {
                EdLog.Assets.Error("Failed to import shader: {local}", fullFilePath.Substring(Editor.GlobalSingleton.ProjectPath.Length));
                return false;
            }

            Editor.GlobalSingleton.ProjectShaderLibrary.AddFileToMapping(localInputFile, localOutputFile);
            filesystem.RemapFile(localInputFile, localOutputFile);

            if (hasLocalConfigFile)
                pipeline.MakeFileAssociations(localInputFile, [tomlFile, .. processor.ReadFiles]);
            else
                pipeline.MakeFileAssociations(localInputFile, processor.ReadFiles);
            if (processor.ShaderPath != null)
                pipeline.ReloadAsset(pipeline.Identifier.GetOrRegisterAsset(localInputFile));

            Editor.GlobalSingleton.AssetDatabase.AddEntry<ShaderAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localInputFile), localInputFile));
            return true;
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);

            if (stream == null)
            {
                return pipeline.Configuration.DoesFileHaveConfig(localFilePath, "Shader") || filesystem.Exists(Path.ChangeExtension(localFilePath, ".toml"));
            }
            if (stream.Length < 8)
                return false;

            using BinaryReader br = new BinaryReader(stream);


            if (br.ReadUInt32() != ShaderProcessor.HeaderId)
                return false;
            if (br.ReadUInt32() != ShaderProcessor.HeaderVersion)
                return false;

            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            if (!ValidateFile(localFilePath, filesystem, pipeline))
                return;

            Editor.GlobalSingleton.AssetDatabase.AddEntry<ShaderAsset>(new AssetDatabaseEntry(pipeline.Identifier.GetOrRegisterAsset(localFilePath), localFilePath));
        }

        public string CustomFileIcon => "Editor/Textures/Icons/FileShader.png";

        private static ShaderDescriptionArgs DecodeDescription(TomlTable document)
        {
            TomlTable stencilFrontFace = (TomlTable)document["front_face"];
            TomlTable stencilBackFace = (TomlTable)document["back_face"];

            return new ShaderDescriptionArgs
            {
                FillMode = Enum.Parse<RHI.FillMode>((string)document["fill_mode"]),
                CullMode = Enum.Parse<RHI.CullMode>((string)document["cull_mode"]),

                FrontCounterClockwise = (bool)document["front_counter_clockwise"],

                DepthBias = (int)(long)document["depth_bias"],
                DepthBiasClamp = (float)(double)document["depth_bias_clamp"],
                SlopeScaledDepthBias = (float)(double)document["slope_scaled_depth_bias"],
                DepthClipEnable = (bool)document["depth_clip_enable"],

                ConservativeRaster = (bool)document["conservative_raster"],

                DepthEnable = (bool)document["depth_enable"],
                DepthWriteMask = Enum.Parse<RHI.DepthWriteMask>((string)document["depth_write_mask"]),
                DepthFunc = Enum.Parse<RHI.ComparisonFunc>((string)document["depth_func"]),

                StencilEnable = (bool)document["stencil_enable"],
                StencilReadMask = (byte)(long)document["stencil_read_mask"],
                StencilWriteMask = (byte)(long)document["stencil_write_mask"],

                PrimitiveTopology = Enum.Parse<RHI.PrimitiveTopologyType>((string)document["primitive_topology"]),

                AlphaToCoverageEnable = (bool)document["alpha_to_coverage_enable"],
                IndependentBlendEnable = (bool)document["independent_blend_enable"],

                LogicOpEnable = (bool)document["logic_op_enable"],
                LogicOp = Enum.Parse<RHI.LogicOp>((string)document["logic_op"]),

                FrontFace = new StencilFaceDescriptionArgs
                {
                    FailOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_fail_op"]),
                    DepthFailOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_depth_fail_op"]),
                    PassOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_pass_op"]),
                    Func = Enum.Parse<RHI.ComparisonFunc>((string)stencilFrontFace["stencil_func"])
                },
                BackFace = new StencilFaceDescriptionArgs
                {
                    FailOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_fail_op"]),
                    DepthFailOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_depth_fail_op"]),
                    PassOp = Enum.Parse<RHI.StencilOp>((string)stencilFrontFace["stencil_pass_op"]),
                    Func = Enum.Parse<RHI.ComparisonFunc>((string)stencilFrontFace["stencil_func"])
                }
            };
        }

        private static BlendDescriptionArgs[] DecodeBlends(TomlTableArray blendsTable)
        {
            BlendDescriptionArgs[] blends = new BlendDescriptionArgs[blendsTable.Count];
            for (int i = 0; i < blendsTable.Count; i++)
            {
                TomlTable blendTable = blendsTable[i];
                blends[i] = new BlendDescriptionArgs
                {
                    BlendEnable = (bool)blendTable["blend_enable"],

                    SourceBlend = Enum.Parse<RHI.Blend>((string)blendTable["src_blend"]),
                    DestinationBlend = Enum.Parse<RHI.Blend>((string)blendTable["dst_blend"]),
                    BlendOp = Enum.Parse<RHI.BlendOp>((string)blendTable["blend_op"]),

                    SourceBlendAlpha = Enum.Parse<RHI.Blend>((string)blendTable["src_blend_alpha"]),
                    DestinationBlendAlpha = Enum.Parse<RHI.Blend>((string)blendTable["dst_blend_alpha"]),
                    BlendOpAlpha = Enum.Parse<RHI.BlendOp>((string)blendTable["blend_op_alpha"]),

                    RenderTargetWriteMask = (byte)(long)blendTable["render_target_write_mask"]
                };
            }

            return blends;
        }
    }
}
