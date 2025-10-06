using CsToml;
using Editor.Processors;
using Editor.Storage;
using K4os.Compression.LZ4.Streams;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Common;
using Primary.Utility;
using System.Runtime.CompilerServices;
using Tomlyn;
using Tomlyn.Model;

namespace Editor.Assets.Importers
{
    internal class ModelAssetImporter : IAssetImporter
    {
        public ModelAssetImporter()
        {

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

            TomlTable rootConfig = Toml.ToModel<TomlTable>(File.ReadAllText(configFile));
            ModelProcessorArgs args = ReadTomlDocument(rootConfig);

            args.AbsoluteFilepath = fullFilePath;
            args.AbsoluteOutputPath = outputFilePath;

            ModelProcessor proc = new ModelProcessor();
            bool r = proc.Execute(args);

            if (!r)
            {
                EdLog.Assets.Error("Failed to import model: {local}", localInputFile);
                return false;
            }

            filesystem.RemapFile(localInputFile, localOutputFile);
            pipeline.ReloadAsset(pipeline.Identifier.GetOrRegisterAsset(localInputFile));

            if (hasLocalConfig)
                pipeline.MakeFileAssociations(localInputFile, configFile);

            AssetId modelId = pipeline.Identifier.GetOrRegisterAsset(localInputFile);
            Editor.GlobalSingleton.AssetDatabase.AddEntry<ModelAsset>(new AssetDatabaseEntry(modelId, localInputFile));

            AssetCategoryDatabase category = Editor.GlobalSingleton.AssetDatabase.GetCategory<RenderMesh>()!;
            for (int i = 0; i < proc.MeshInfos.Length; i++)
            {
                ref ModelMeshInfo mmi = ref proc.MeshInfos[i];
                category.AddEntry(new AssetDatabaseEntry(modelId, mmi.Name));
            }

            return true;
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);

            if (stream == null)
            {
                return pipeline.Configuration.DoesFileHaveConfig(localFilePath, "Model") || filesystem.Exists(Path.ChangeExtension(localFilePath, ".toml"));
            }

            if (stream.Length < Unsafe.SizeOf<PMFHeader>())
                return false;

            PMFHeader header = stream.Read<PMFHeader>();
            if (header.Header != PMFHeader.ConstHeader || header.Version != PMFHeader.ConstVersion)
                return false;

            return true;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);

            if (stream == null)
                return;

            PMFHeader header = stream.Read<PMFHeader>();
            if (header.Header != PMFHeader.ConstHeader || header.Version != PMFHeader.ConstVersion)
                return;

            AssetId modelId = pipeline.Identifier.GetOrRegisterAsset(localFilePath);
            Editor.GlobalSingleton.AssetDatabase.AddEntry<ModelAsset>(new AssetDatabaseEntry(modelId, localFilePath));

            AssetCategoryDatabase category = Editor.GlobalSingleton.AssetDatabase.GetCategory<RenderMesh>()!;

            Stream dataReadStream = stream;
            if (FlagUtility.HasFlag(header.Flags, PMFHeaderFlags.IsCompressed))
                dataReadStream = LZ4Stream.Decode(stream);

            using BinaryReader br = new BinaryReader(dataReadStream);

            int indexStride = FlagUtility.HasFlag(header.Flags, PMFHeaderFlags.LargeIndices) ? 4 : 2;
            for (int i = 0; i < header.MeshCount; i++)
            {
                string meshName = br.ReadString();
                category.AddEntry(new AssetDatabaseEntry(modelId, meshName));

                uint vertexCount = br.ReadUInt32();
                uint indexCount = br.ReadUInt32();

                ushort vertexStride = br.ReadUInt16();
                br.Skip(sizeof(byte)); //UVChannelMask

                br.Skip((int)vertexCount * (int)vertexStride + (int)indexCount * indexStride);
            }
        }

        public static ModelProcessorArgs ReadTomlDocument(TomlTable document)
        {
            return new ModelProcessorArgs
            {
                IsCompressed = (bool)document["is_compressed"],

                IndexStrideMode = Enum.Parse<ModelIndexStrideMode>((string)document["index_stride_mode"], true),

                UseHalfPrecisionNodes = (bool)document["use_half_precision_nodes"],
                UseHalfPrecisionVertices = (bool)document["use_half_precision_vertices"],
            };
        }

        public string CustomFileIcon => "Editor/Textures/Icons/FileModel.png";
    }
}
