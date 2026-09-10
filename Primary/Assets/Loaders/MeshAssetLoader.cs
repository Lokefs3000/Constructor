using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Mathematics;
using Primary.Rendering.Assets;
using Primary.Utility;

namespace Primary.Assets.Loaders
{
    public sealed class MeshAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new MeshAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            return new MeshAsset((MeshAssetData)assetData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, string localPath, BundleReader? bundleToReadFrom)
        {
            MeshAsset meshAsset = (MeshAsset)asset;
            MeshAssetData meshAssetData = (MeshAssetData)assetData;

            meshAssetData.Dispose();

            using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom) ?? throw new AssetLoadException("Failed to open asset stream");

            FileModelHeader header = stream.Read<FileModelHeader>();
            if (header.FileHeader != FileModelHeader.Header)
                throw new AssetLoadException("Model header does not match with expected value");
            if (header.FileVersion != FileModelHeader.Version)
                throw new AssetLoadException("Model header is in a different version than what is supported");

            uint vertexOffset = 0;
            uint indexOffset = 0;

            int currentMeshIndex = 0;
            while (currentMeshIndex < header.MeshCount)
            {
                FileModelMesh mesh = stream.Read<FileModelMesh>();
                if (mesh.LocalId == meshAssetData.Id.LocalId)
                {
                    ModelAsset parentModel = AssetManager.LoadAsset<ModelAsset>(meshAssetData.Id.WithoutLocalId());

                    string meshName = stream.ReadStringUtf16(mesh.NameLength);

                    AABB boundaries = AABB.Zero;
                    {
                        int elementCount = header.VertexStride / sizeof(float);

                        using RentedArray<float> vertexData = new RentedArray<float>((int)(mesh.VertexCount * elementCount));
                        stream.ReadExactly(vertexData.Span);

                        Vector3 boundsMinimum = Vector3.Zero;
                        Vector3 boundsMaximum = Vector3.Zero;

                        for (int i = 0; i < vertexData.Count; i += elementCount)
                        {
                            Vector3 position = Vector3.LoadUnsafe(ref vertexData[i]);

                            if (i == 0)
                            {
                                boundsMinimum = position;
                                boundsMaximum = position;
                            }
                            else
                            {
                                boundsMinimum = Vector3.Min(boundsMinimum, position);
                                boundsMaximum = Vector3.Max(boundsMaximum, position);
                            }
                        }

                        boundaries = new AABB(boundsMinimum, boundsMaximum);
                    }

                    RenderMeshDrawArgs drawArgs = new RenderMeshDrawArgs(parentModel.InternalAssetData, vertexOffset, indexOffset, mesh.IndexCount, true);

                    meshAssetData.SetAssetInternalName(meshName);
                    meshAssetData.UpdateAssetData(meshAsset, parentModel, boundaries, drawArgs);

                    return;
                }
                else
                {
                    stream.Skip(mesh.NameLength * sizeof(char) + mesh.VertexCount * header.VertexStride + mesh.IndexCount * header.IndexStride);

                    vertexOffset += mesh.VertexCount;
                    indexOffset += mesh.IndexCount;
                }

                ++currentMeshIndex;
            }

            throw new AssetLoadException("No mesh with appropriate local id was found in the model");
        }
    }
}
