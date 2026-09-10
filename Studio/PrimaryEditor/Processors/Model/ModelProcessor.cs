using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Primary.Collections;
using Primary.Common;
using SharpGen.Runtime;
using Silk.NET.Assimp;

namespace PrimaryEditor.Processors.Model
{
    public static unsafe class ModelProcessor
    {
        public static ModelOutputData Process(Stream inputStream, ModelConfiguration config, string hint)
        {
            t_assimp ??= Assimp.GetApi();

            Scene* assimpScene = null;
            {
                using RentedArray<byte> streamContents = new RentedArray<byte>((int)inputStream.Length);
                inputStream.ReadExactly(streamContents.Span);

                uint flags = (uint)(PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.FlipWindingOrder);
                assimpScene = t_assimp.ImportFileFromMemory(streamContents.Span, (uint)streamContents.Count, flags, hint);
            }

            if (assimpScene == null)
            {
                throw new AssetProcessorException($"Failed to load model: {t_assimp.GetErrorStringS()}");
            }

            try
            {
                (int vertexStride, int startUVIndex, int indexStride) = CalculateStrideForModel(assimpScene, config.IndexStrideMode, config.UVChannels);

                using RentedArray<ModelMeshInfo> meshInfos = ImportMeshesFromScene(assimpScene, vertexStride, startUVIndex, indexStride);
                using RentedList<ModelNodeInfo> nodeInfos = ImportNodesFromScene(assimpScene);

                return new ModelOutputData(vertexStride, indexStride, [.. meshInfos], [.. nodeInfos]);
            }
            finally
            {
                t_assimp.FreeScene(assimpScene);
            }
        }

        private static (int VertexStride, int StartUVIndex, int IndexStride) CalculateStrideForModel(Scene* scene, ModelIndexStrideMode strideMode, ModelUVMask allowedChannels)
        {
            uint maxIndexValue = 0;
            ModelUVMask uvMask = ModelUVMask.None;

            for (int i = 0; i < scene->MNumMeshes; ++i)
            {
                Mesh* assimpMesh = scene->MMeshes[i];
                for (int j = 0; j < 8; ++j)
                {
                    if (assimpMesh->MTextureCoords[j] != null)
                    {
                        uvMask |= (ModelUVMask)(1 << j);
                    }
                }

                for (int j = 0; j < assimpMesh->MNumFaces; ++j)
                {
                    Face assimpFace = assimpMesh->MFaces[j];
                    if (assimpFace.MNumIndices != 3)
                        throw new AssetProcessorException("Face in model was not a triangle");

                    uint currentLimit = Math.Max(Math.Max(assimpFace.MIndices[0], assimpFace.MIndices[1]), assimpFace.MIndices[2]);
                    maxIndexValue = Math.Max(maxIndexValue, currentLimit);
                }
            }

            uvMask &= allowedChannels;

            int indexStride;
            switch (strideMode)
            {
                case ModelIndexStrideMode.Automatic:
                default: indexStride = maxIndexValue > ushort.MaxValue ? 4 : 2; break;
                case ModelIndexStrideMode.HalfPrecision: indexStride = 2; break;
                case ModelIndexStrideMode.FullPrecision: indexStride = 4; break;
            }

            return (BaseVertexStride + sizeof(float) * int.PopCount((int)uvMask) * 2, int.TrailingZeroCount((int)uvMask), indexStride);
        }

        private static RentedArray<ModelMeshInfo> ImportMeshesFromScene(Scene* assimpScene, int vertexStride, int startUVIndex, int indexStride)
        {
            RentedArray<ModelMeshInfo> meshInfos = new RentedArray<ModelMeshInfo>((int)assimpScene->MNumMeshes);

            int uvChannelCount = (vertexStride - BaseVertexStride) / 2 / sizeof(float);

            for (int i = 0; i < assimpScene->MNumMeshes; ++i)
            {
                Mesh* assimpMesh = assimpScene->MMeshes[i];

                byte[] rawVertexData = new byte[assimpMesh->MNumVertices * vertexStride];
                byte[] rawIndexData = new byte[assimpMesh->MNumFaces * indexStride * 3];

                {
                    bool hasTangentsAndBitangents = assimpMesh->MTangents != null && assimpMesh->MBitangents != null;
                    for (int j = 0; j < assimpMesh->MNumVertices; ++j)
                    {
                        Vector3 position = assimpMesh->MVertices[j];
                        Vector3 normal = assimpMesh->MNormals[j];
                        Vector4 tangent = Vector4.Zero;

                        if (hasTangentsAndBitangents)
                        {
                            Vector3 tan = assimpMesh->MTangents[j];
                            Vector3 bit = assimpMesh->MBitangents[j];

                            Vector3 orthoT = Vector3.Normalize(tan - normal * Vector3.Dot(normal, tan));
                            tangent = new Vector4(tan, Vector3.Dot(Vector3.Cross(normal, orthoT), bit) > 0.0f ? 1.0f : -1.0f);
                        }

                        Span<float> localOffset = MemoryMarshal.Cast<byte, float>(rawVertexData.AsSpan(vertexStride * j, vertexStride));
                        position.StoreUnsafe(ref localOffset[0]);
                        normal.StoreUnsafe(ref localOffset[3]);
                        tangent.StoreUnsafe(ref localOffset[6]);

                        for (int k = 0; k < uvChannelCount; ++k)
                        {
                            ref Vector3* uvChannel = ref assimpMesh->MTextureCoords[k + startUVIndex];
                            Vector2 uv = uvChannel != null ? uvChannel[j].AsVector2() : Vector2.Zero;

                            uv.StoreUnsafe(ref localOffset[10 + k * 2]);
                        }
                    }
                }

                if (indexStride == 2)
                {
                    Span<ushort> span = MemoryMarshal.Cast<byte, ushort>(rawIndexData.AsSpan());
                    for (int j = 0; j < assimpMesh->MNumFaces; ++j)
                    {
                        Face assimpFace = assimpMesh->MFaces[j];
                        Debug.Assert(assimpFace.MNumIndices == 3);

                        Span<ushort> localOffset = span.Slice(j * 3, 3);
                        localOffset[0] = (ushort)assimpFace.MIndices[0];
                        localOffset[1] = (ushort)assimpFace.MIndices[1];
                        localOffset[2] = (ushort)assimpFace.MIndices[2];
                    }
                }
                else
                {
                    Span<uint> span = MemoryMarshal.Cast<byte, uint>(rawIndexData.AsSpan());
                    for (int j = 0; j < assimpMesh->MNumFaces; ++j)
                    {
                        Face assimpFace = assimpMesh->MFaces[j];
                        Debug.Assert(assimpFace.MNumIndices == 3);

                        Span<uint> localOffset = span.Slice(j * 3, 3);
                        localOffset[0] = assimpFace.MIndices[0];
                        localOffset[1] = assimpFace.MIndices[1];
                        localOffset[2] = assimpFace.MIndices[2];
                    }
                }

                meshInfos[i] = new ModelMeshInfo(assimpMesh->MName.AsString, (int)assimpMesh->MNumVertices, (int)assimpMesh->MNumFaces * 3, rawVertexData, rawIndexData);
            }

            return meshInfos;
        }

        private static RentedList<ModelNodeInfo> ImportNodesFromScene(Scene* assimpScene)
        {
            RentedList<ModelNodeInfo> nodeInfos = new RentedList<ModelNodeInfo>();

            using RentedStack<Ptr<Node>> nodeStack = new RentedStack<Ptr<Node>>();
            nodeStack.Push(assimpScene->MRootNode);

            while (nodeStack.TryPop(out Ptr<Node> nodePtr))
            {
                Node* node = nodePtr.Pointer;

                ModelTransformInfo? transformInfo = null;
                if (Matrix4x4.Decompose(node->MTransformation, out Vector3 scale, out Quaternion quat, out Vector3 translation))
                {
                    if (translation != Vector3.Zero || quat != Quaternion.Identity || scale != Vector3.One)
                    {
                        transformInfo = new ModelTransformInfo(translation, quat, scale);
                    }
                }

                nodeInfos.Add(new ModelNodeInfo(node->MName.AsString, node->MNumMeshes > 0 ? (ushort)node->MMeshes[0] : ushort.MaxValue, (int)node->MNumChildren, transformInfo));

                for (int i = 0; i < node->MNumChildren; ++i)
                {
                    nodeStack.Push(node->MChildren[i]);
                }
            }

            return nodeInfos;
        }

        // Position: v3
        // Normal: v3
        // Tangent: v4
        private const int BaseVertexStride = sizeof(float) * (3 + 3 + 4);

#pragma warning disable IDE1006 // Naming Styles
        [ThreadStatic]
        private static Assimp? t_assimp;
#pragma warning restore IDE1006 // Naming Styles
    }
}
