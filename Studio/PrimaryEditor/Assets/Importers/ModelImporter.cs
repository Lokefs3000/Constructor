using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Editor.Processors.Texture;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Utility;
using PrimaryEditor.Assets.Database;
using PrimaryEditor.Assets.Exceptions;
using PrimaryEditor.Assets.Utility;
using PrimaryEditor.Processors.Model;
using PrimaryEditor.Processors.TextureAtlas;
using TerraFX.Interop.Windows;
using Tomlyn;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Importers
{
    internal sealed class ModelImporter : IAssetImporter
    {
        public void ImportFile(in ImportContext context)
        {
            ModelConfiguration config = context.GetAssetConfiguration<ModelConfiguration>();

            ModelOutputData outputData;
            try
            {
                outputData = ModelProcessor.Process(context.InputStream, config, Path.GetExtension(context.LocalPath));
            }
            catch (AssetImportException ex)
            {
                EdLog.Assets.Error(ex, "[{f}]: Model processor threw an exception when trying to import", context.LocalPath);
                throw new AssetImportException("Error", ex);
            }

            uint vertexCount = 0;
            uint indexCount = 0;

            foreach (ModelMeshInfo meshInfo in outputData.Meshes)
            {
                vertexCount += (uint)meshInfo.VertexCount;
                indexCount += (uint)meshInfo.IndexCount;
            }

            context.OutputStream.Write(new FileModelHeader
            {
                FileHeader = FileModelHeader.Header,
                FileVersion = FileModelHeader.Version,

                VertexCount = vertexCount,
                IndexCount = indexCount,

                VertexStride = (byte)outputData.VertexStride,
                IndexStride = (byte)outputData.IndexStride,

                MeshCount = (ushort)outputData.Meshes.Length
            });

            foreach (ModelMeshInfo meshInfo in outputData.Meshes)
            {
                string meshName = outputData.Meshes.Length == 1 ? Path.GetFileNameWithoutExtension(context.LocalPath) : meshInfo.MeshName;

                context.AddSubAsset<MeshAsset>(meshInfo.MeshName.GetDjb2HashCode(), meshName);
                context.OutputStream.Write(new FileModelMesh
                {
                    LocalId = meshInfo.MeshName.GetDjb2HashCode(),

                    VertexCount = (uint)meshInfo.VertexCount,
                    IndexCount = (uint)meshInfo.IndexCount,

                    NameLength = (byte)Math.Min(meshName.Length, byte.MaxValue)
                });

                if (meshName.Length > byte.MaxValue)
                    EdLog.Assets.Warning("[{f}]: Mesh name '{n}' will be trunacated to 255 characters max", context.LocalPath, meshName);
                if (meshName.Length > 0)
                    context.OutputStream.Write(meshName.AsSpan(0, Math.Min(meshName.Length, byte.MaxValue)));

                context.OutputStream.Write(meshInfo.RawVertexData);
                context.OutputStream.Write(meshInfo.RawIndexData);
            }

            foreach (ModelNodeInfo nodeInfo in outputData.Nodes)
            {
                NodeTransformFeatures features = NodeTransformFeatures.None;
                if (nodeInfo.Transform.HasValue)
                {
                    ModelTransformInfo transform = nodeInfo.Transform.Value;
                    if (transform.Position != Vector3.Zero)
                        features |= NodeTransformFeatures.Position;
                    if (transform.Orientation != Quaternion.Identity)
                        features |= NodeTransformFeatures.Rotation;
                    if (transform.Scale != Vector3.One)
                    {
                        if (transform.Scale.X == transform.Scale.Y && transform.Scale.X == transform.Scale.Z)
                            features |= NodeTransformFeatures.UniformScale;
                        else
                            features |= NodeTransformFeatures.Scale;
                    }
                }

                context.OutputStream.Write(new FileModelNode
                {
                    NameLength = (byte)Math.Min(nodeInfo.NodeName.Length, byte.MaxValue),
                    MeshIndex = nodeInfo.MeshIndex,

                    Transform = features,

                    ChildCount = (ushort)nodeInfo.ChildrenCount
                });

                if (nodeInfo.NodeName.Length > byte.MaxValue)
                    EdLog.Assets.Warning("[{f}]: Node name '{n}' will be trunacated to 255 characters max", context.LocalPath, nodeInfo.NodeName);
                if (nodeInfo.NodeName.Length > 0)
                    context.OutputStream.Write(nodeInfo.NodeName.AsSpan(0, Math.Min(nodeInfo.NodeName.Length, byte.MaxValue)));

                if (features.HasFlags(NodeTransformFeatures.Position))
                    context.OutputStream.Write(nodeInfo.Transform.GetValueOrDefault().Position);
                if (features.HasFlags(NodeTransformFeatures.Rotation))
                    context.OutputStream.Write(nodeInfo.Transform.GetValueOrDefault().Orientation);
                if (features.HasFlags(NodeTransformFeatures.UniformScale))
                    context.OutputStream.Write(nodeInfo.Transform.GetValueOrDefault().Scale.X);
                else if (features.HasFlags(NodeTransformFeatures.Scale))
                    context.OutputStream.Write(nodeInfo.Transform.GetValueOrDefault().Scale);
            }
        }

        public bool ValidateFile(AssetPipeline pipeline, AssetId id, string localPath)
        {
            using Stream? stream = FilesystemManager.OpenStream(localPath);

            if (stream == null || stream.Length < Unsafe.SizeOf<FileModelHeader>())
                return false;

            FileModelHeader header = stream.Read<FileModelHeader>();

            if (header.FileHeader != FileModelHeader.Header) return false;
            if (header.FileVersion != FileModelHeader.Version) return false;

            return true;
        }

        public string UniqueId => "model";
        public Type AssetDefinitionType => typeof(ModelAsset);

        public string? DefaultConfigName => "DefaultConfig_Model.toml";
        public Type? ConfigType => typeof(ModelConfiguration);

        public TomlConverter[] Converters => [];

        private static readonly TomlSerializerOptions s_tomlOptions = new TomlSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }
}
