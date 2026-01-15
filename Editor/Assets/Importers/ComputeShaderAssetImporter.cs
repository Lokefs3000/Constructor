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
    internal sealed class ComputeShaderAssetImporter : IAssetImporter
    {
        public ComputeShaderAssetImporter()
        {

        }

        public void Dispose() { }

        public bool Import(AssetPipeline pipeline, ProjectSubFilesystem filesystem, string fullFilePath, string outputFilePath, string localOutputFile)
        {
            string localInputFile = fullFilePath.Substring(filesystem.AbsolutePath.Length);

            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localInputFile);

            bool hasLocalConfigFile = false;
            string tomlFile = pipeline.Configuration.GetFilePath(localInputFile, "CsShader");
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
            Processors.ComputeShaderProcessorArgs args = new Processors.ComputeShaderProcessorArgs
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
            };

            Processors.ComputeShaderProcessor processor = new Processors.ComputeShaderProcessor();
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

            Editor.GlobalSingleton.AssetDatabase.AddEntry<ComputeShaderAsset>(new AssetDatabaseEntry(id, localInputFile, true));
            return false;
        }

        public void Preload(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            AssetId id = pipeline.Identifier.GetOrRegisterAsset(localFilePath);
            using Stream? stream = filesystem.OpenStream(localFilePath);

            if (stream == null || stream.Length < Unsafe.SizeOf<CBCHeader>())
            {
                Editor.GlobalSingleton.AssetDatabase.AddEntry<ComputeShaderAsset>(new AssetDatabaseEntry(id, localFilePath, false));
                return;
            }

            CBCHeader header = stream.Read<CBCHeader>();
            if (header.Header != CBCHeader.ConstHeader || header.Version != CBCHeader.ConstVersion)
            {
                Editor.GlobalSingleton.AssetDatabase.AddEntry<ComputeShaderAsset>(new AssetDatabaseEntry(id, localFilePath, false));
                return;
            }

            Editor.GlobalSingleton.AssetDatabase.AddEntry<ComputeShaderAsset>(new AssetDatabaseEntry(id, localFilePath, true));
        }

        public bool ValidateFile(string localFilePath, ProjectSubFilesystem filesystem, AssetPipeline pipeline)
        {
            using Stream? stream = filesystem.OpenStream(localFilePath);

            if (stream == null)
                return pipeline.Configuration.DoesFileHaveConfig(localFilePath, "CsShader");

            if (stream.Length < Unsafe.SizeOf<CBCHeader>())
                return false;

            CBCHeader header = stream.Read<CBCHeader>();
            if (header.Header != CBCHeader.ConstHeader || header.Version != CBCHeader.ConstVersion)
                return false;

            return true;
        }

        public string? CustomFileIcon => "Editor/Textures/Icons/FileShader2.png";
    }
}
