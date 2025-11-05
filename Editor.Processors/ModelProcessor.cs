using CommunityToolkit.HighPerformance;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using Primary.Assets.Loaders;
using Primary.Common;
using Silk.NET.Assimp;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Editor.Processors
{
    public unsafe class ModelProcessor : IAssetProcessor
    {
        private static readonly Assimp s_api = Assimp.GetApi();

        public ModelMeshInfo[] MeshInfos = Array.Empty<ModelMeshInfo>();

        public bool Execute(object args_in)
        {
            ModelProcessorArgs args = (ModelProcessorArgs)args_in;

            PostProcessSteps steps = PostProcessPreset.TargetRealTimeMaximumQuality;
            Scene* scene = s_api.ImportFile(args.AbsoluteFilepath, (uint)steps);
            if (scene == null)
            {
                //bad
                return false;
            }

            ModelMetrics metrics = CalculateMetics(scene);

            List<PMFMesh> meshes = ParseMeshesFromScene(scene, ref metrics);
            List<PMFNode> nodes = ParseNodesFromScene(scene, ref metrics);

            {
                using FileStream stream = System.IO.File.Open(args.AbsoluteOutputPath, FileMode.Create, FileAccess.Write);
                WriteHeader(stream, scene, meshes, ref metrics, ref args);

                Stream dataToWrite = stream;
                if (args.IsCompressed)
                    dataToWrite = LZ4Stream.Encode(stream, LZ4Level.L12_MAX);

                using BinaryWriter bw = new BinaryWriter(stream);

                bool useLargeIndices = args.IndexStrideMode switch
                {
                    ModelIndexStrideMode.Auto => metrics.LargestIndice > ushort.MaxValue,
                    ModelIndexStrideMode.Only16Bit => false,
                    ModelIndexStrideMode.Only32Bit => true,
                    _ => throw new NotImplementedException()
                };

                WriteMeshes(bw, meshes, scene, args.UseHalfPrecisionVertices, useLargeIndices);
                WriteNodes(bw, nodes, args.UseHalfPrecisionNodes);
            }

            return true;
        }

        #region Parsing
        private static ModelMetrics CalculateMetics(Scene* scene)
        {
            uint vertexCount = 0;
            uint indexCount = 0;

            uint largestIndice = 0;

            for (int i = 0; i < scene->MNumMeshes; i++)
            {
                Mesh* mesh = scene->MMeshes[i];

                vertexCount += mesh->MNumVertices;

                for (int j = 0; j < mesh->MNumFaces; j++)
                {
                    ref Face face = ref mesh->MFaces[j];
                    if (face.MNumIndices != 3)
                        throw new Exception("Unexpected non 3 indice face in mesh data");

                    indexCount += face.MNumIndices;

                    //TODO: use SIMD to speed up function
                    largestIndice = Math.Max(Math.Max(face.MIndices[0], face.MIndices[1]), face.MIndices[2]);
                }
            }

            Queue<Ptr<Node>> nodeQueue = new Queue<Ptr<Node>>();
            nodeQueue.Enqueue(new Ptr<Node>(scene->MRootNode));

            uint nodeCount = 0;
            while (nodeQueue.TryDequeue(out Ptr<Node> node))
            {
                nodeCount++;

                for (int i = 0; i < node.Pointer->MNumChildren; i++)
                {
                    nodeQueue.Enqueue(new Ptr<Node>(node.Pointer->MChildren[i]));
                }
            }

            return new ModelMetrics(vertexCount, indexCount, scene->MNumMeshes, nodeCount, largestIndice);
        }

        private static List<PMFMesh> ParseMeshesFromScene(Scene* scene, ref ModelMetrics metrics)
        {
            List<PMFMesh> meshes = new List<PMFMesh>((int)metrics.MeshCount);

            for (int i = 0; i < scene->MNumMeshes; i++)
            {
                Mesh* assimpMesh = scene->MMeshes[i];

                uint indexCount = 0;
                for (int j = 0; j < assimpMesh->MNumFaces; j++)
                    indexCount += assimpMesh->MFaces[j].MNumIndices;

                byte uvChannelMask = 0;
                for (int j = 0; j < 8; j++)
                    if (assimpMesh->MTextureCoords[j] != null)
                        uvChannelMask |= (byte)(1 << j);

                PMFMesh mesh = new PMFMesh
                {
                    Name = assimpMesh->MName.AsString,

                    VertexCount = assimpMesh->MNumVertices,
                    IndexCount = indexCount,

                    VertexStride = (ushort)(12 * sizeof(float) + byte.PopCount(uvChannelMask) * 2 * sizeof(float)),
                    UVChannelMask = uvChannelMask,
                };

                meshes.Add(mesh);
            }

            return meshes;
        }

        private static List<PMFNode> ParseNodesFromScene(Scene* scene, ref ModelMetrics metrics)
        {
            List<PMFNode> nodes = new List<PMFNode>();
            IterateRecursiveNode(scene->MRootNode);

            void IterateRecursiveNode(Node* assimpNode)
            {
                bool decomposeResult = Matrix4x4.Decompose(assimpNode->MTransformation, out Vector3 scale, out Quaternion rotation, out Vector3 translation);

                PMFTransformFeatures features = PMFTransformFeatures.None;
                if (decomposeResult)
                {
                    if (translation != Vector3.Zero)
                        features |= PMFTransformFeatures.Position;
                    if (rotation != Quaternion.Identity)
                        features |= PMFTransformFeatures.Rotation;
                    if (scale != Vector3.One)
                    {
                        if (scale.X == scale.Y && scale.X == scale.Z)
                            features |= PMFTransformFeatures.UniformScale;
                        else
                            features |= PMFTransformFeatures.Scale;
                    }
                }

                PMFTransform transform = new PMFTransform
                {
                    Features = features,

                    Position = translation,
                    Rotation = rotation,
                    Scale = scale
                };

                ushort meshIndex = ushort.MaxValue;
                if (assimpNode->MNumMeshes > 0)
                {
                    meshIndex = (ushort)assimpNode->MMeshes[0];
                }

                PMFNode node = new PMFNode
                {
                    Name = assimpNode->MName.AsString,
                    Transform = transform,

                    ChildCount = (ushort)assimpNode->MNumChildren,

                    MeshIndex = meshIndex
                };

                nodes.Add(node);

                for (int i = 0; i < assimpNode->MNumChildren; i++)
                {
                    IterateRecursiveNode(assimpNode->MChildren[i]);
                }
            }

            return nodes;
        }
        #endregion

        #region Writing
        private void WriteHeader(Stream rawStream, Scene* scene, List<PMFMesh> meshes, ref ModelMetrics metrics, ref ModelProcessorArgs args)
        {
            uint vertexDataSize = 0;
            uint indexCount = 0;

            Span<PMFMesh> span = meshes.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                ref PMFMesh mesh = ref span[i];

                vertexDataSize += mesh.VertexStride * mesh.VertexCount;
                indexCount += mesh.IndexCount;
            }

            PMFHeaderFlags flags = PMFHeaderFlags.None;

            if (args.IsCompressed)
                flags |= PMFHeaderFlags.IsCompressed;
            if (args.UseHalfPrecisionNodes)
                flags |= PMFHeaderFlags.HalfNodeTransforms;
            if (args.UseHalfPrecisionVertices)
                flags |= PMFHeaderFlags.HalfVertexValues;

            switch (args.IndexStrideMode)
            {
                case ModelIndexStrideMode.Auto:
                    {
                        if (metrics.LargestIndice > ushort.MaxValue)
                            flags |= PMFHeaderFlags.LargeIndices;
                        break;
                    }
                case ModelIndexStrideMode.Only32Bit: flags |= PMFHeaderFlags.LargeIndices; break;
            }

            PMFHeader header = new PMFHeader
            {
                Header = PMFHeader.ConstHeader,
                Version = PMFHeader.ConstVersion,

                Flags = flags,

                VertexTotalSize = vertexDataSize,
                IndexCount = indexCount,

                MeshCount = (ushort)metrics.MeshCount,
                NodeCount = (ushort)metrics.NodeCount
            };

            rawStream.Write(header);
        }

        private void WriteMeshes(BinaryWriter bw, List<PMFMesh> meshes, Scene* scene, bool useHalfPrecision, bool useLargeIndices)
        {
            MeshInfos = new ModelMeshInfo[meshes.Count];

            Span<PMFMesh> span = meshes.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                ref PMFMesh mesh = ref span[i];

                MeshInfos[i] = new ModelMeshInfo(mesh.Name);

                bw.Write(mesh.Name);
                bw.Write(mesh.VertexCount);
                bw.Write(mesh.IndexCount);
                bw.Write(mesh.VertexStride);
                bw.Write(mesh.UVChannelMask);

                int channelCount = 8 - byte.LeadingZeroCount(mesh.UVChannelMask);
                int stride = 12 + channelCount * 2;

                Mesh* assimpMesh = scene->MMeshes[i];
                bool hasTangentAndBitangents = assimpMesh->MTangents != null && assimpMesh->MBitangents != null;

                Vector3 zero = Vector3.Zero;

                if (useHalfPrecision)
                {
                    using PoolArray<Half> vertices = ArrayPool<Half>.Shared.Rent((int)mesh.VertexCount * stride);
                    Span<Half> verticesSpan = vertices.AsSpan((int)mesh.VertexCount * stride);


                    for (int j = 0; j < assimpMesh->MNumVertices; j++)
                    {
                        Span<Half> localVertices = verticesSpan.Slice(j * stride);

                        ref Vector3 position = ref assimpMesh->MVertices[j];
                        ref Vector3 normal = ref assimpMesh->MNormals[j];
                        ref Vector3 tangent = ref assimpMesh->MTangents[j];
                        ref Vector3 bitangent = ref assimpMesh->MBitangents[j];

                        if (!hasTangentAndBitangents)
                        {
                            tangent = ref zero;
                            bitangent = ref normal;
                        }

                        localVertices[0] = (Half)position.X;
                        localVertices[1] = (Half)position.Y;
                        localVertices[2] = (Half)position.Z;

                        localVertices[3] = (Half)normal.X;
                        localVertices[4] = (Half)normal.Y;
                        localVertices[5] = (Half)normal.Z;

                        localVertices[6] = (Half)tangent.X;
                        localVertices[7] = (Half)tangent.Y;
                        localVertices[8] = (Half)tangent.Z;

                        localVertices[9] = (Half)bitangent.X;
                        localVertices[10] = (Half)bitangent.Y;
                        localVertices[11] = (Half)bitangent.Z;

                        int offset = 0;
                        for (int k = 0; k < channelCount; k++)
                        {
                            if ((mesh.UVChannelMask & (1 << k)) > 0)
                            {
                                ref Vector3 uv = ref assimpMesh->MTextureCoords[k][j];

                                localVertices[offset] = (Half)uv.X;
                                localVertices[offset + 1] = (Half)uv.Y;

                                offset += 2;
                            }
                        }
                    }

                    bw.Write(verticesSpan);
                }
                else
                {
                    using PoolArray<float> vertices = ArrayPool<float>.Shared.Rent((int)mesh.VertexCount * stride);
                    Span<float> verticesSpan = vertices.AsSpan(0, (int)mesh.VertexCount * stride);

                    for (int j = 0; j < assimpMesh->MNumVertices; j++)
                    {
                        Span<float> localVertices = verticesSpan.Slice(j * stride);

                        ref Vector3 position = ref assimpMesh->MVertices[j];
                        ref Vector3 normal = ref assimpMesh->MNormals[j];
                        ref Vector3 tangent = ref assimpMesh->MTangents[j];
                        ref Vector3 bitangent = ref assimpMesh->MBitangents[j];

                        if (!hasTangentAndBitangents)
                        {
                            tangent = ref zero;
                            bitangent = ref normal;
                        }

                        ref Vector3 baseV3 = ref Unsafe.As<float, Vector3>(ref localVertices.DangerousGetReference());

                        baseV3 = position;
                        Unsafe.Add(ref baseV3, 1) = normal;
                        Unsafe.Add(ref baseV3, 2) = tangent;
                        Unsafe.Add(ref baseV3, 3) = bitangent;

                        ref Vector2 baseV2 = ref Unsafe.As<float, Vector2>(ref localVertices.DangerousGetReferenceAt(12));

                        int offset = 0;
                        for (int k = 0; k < channelCount; k++)
                        {
                            if ((mesh.UVChannelMask & (1 << k)) > 0)
                            {
                                ref Vector3 uv = ref assimpMesh->MTextureCoords[k][j];
                                Unsafe.Add(ref baseV2, offset++) = new Vector2(uv.X, uv.Y);
                            }
                        }
                    }

                    bw.Write(verticesSpan);
                }

                if (useLargeIndices)
                {
                    using PoolArray<uint> indices = ArrayPool<uint>.Shared.Rent((int)mesh.IndexCount);
                    Span<uint> indicesSpan = indices.AsSpan(0, (int)mesh.IndexCount);

                    for (int j = 0; j < assimpMesh->MNumFaces; j++)
                    {
                        ref Face assimpFace = ref assimpMesh->MFaces[j];
                        Debug.Assert(assimpFace.MNumIndices != 3);

                        int k = j * 3;
                        indicesSpan[k] = assimpFace.MIndices[0];
                        indicesSpan[k + 1] = assimpFace.MIndices[1];
                        indicesSpan[k + 2] = assimpFace.MIndices[2];
                    }

                    bw.Write(indicesSpan);
                }
                else
                {
                    using PoolArray<ushort> indices = ArrayPool<ushort>.Shared.Rent((int)mesh.IndexCount);
                    Span<ushort> indicesSpan = indices.AsSpan(0, (int)mesh.IndexCount);

                    for (int j = 0; j < assimpMesh->MNumFaces; j++)
                    {
                        ref Face assimpFace = ref assimpMesh->MFaces[j];
                        Debug.Assert(assimpFace.MNumIndices == 3);

                        int k = j * 3;
                        indicesSpan[k] = (ushort)assimpFace.MIndices[0];
                        indicesSpan[k + 1] = (ushort)assimpFace.MIndices[1];
                        indicesSpan[k + 2] = (ushort)assimpFace.MIndices[2];
                    }

                    bw.Write(indicesSpan);
                }
            }
        }

        private void WriteNodes(BinaryWriter bw, List<PMFNode> nodes, bool useHalfPrecision)
        {
            Span<PMFNode> span = nodes.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                ref PMFNode node = ref span[i];

                bw.Write(node.Name);

                bw.Write(node.Transform.Features);
                if (FlagUtility.HasFlag(node.Transform.Features, PMFTransformFeatures.Position))
                {
                    if (useHalfPrecision)
                    {
                        bw.Write((Half)node.Transform.Position.X);
                        bw.Write((Half)node.Transform.Position.Y);
                        bw.Write((Half)node.Transform.Position.Z);
                    }
                    else
                        bw.Write(node.Transform.Position);
                }

                if (FlagUtility.HasFlag(node.Transform.Features, PMFTransformFeatures.Rotation))
                {
                    if (useHalfPrecision)
                    {
                        bw.Write((Half)node.Transform.Rotation.X);
                        bw.Write((Half)node.Transform.Rotation.Y);
                        bw.Write((Half)node.Transform.Rotation.Z);
                        bw.Write((Half)node.Transform.Rotation.W);
                    }
                    else
                        bw.Write(node.Transform.Rotation);
                }

                if (FlagUtility.HasFlag(node.Transform.Features, PMFTransformFeatures.UniformScale))
                {
                    if (useHalfPrecision)
                        bw.Write((Half)node.Transform.Scale.X);
                    else
                        bw.Write(node.Transform.Scale.X);
                }
                else if (FlagUtility.HasFlag(node.Transform.Features, PMFTransformFeatures.Scale))
                {
                    if (useHalfPrecision)
                    {
                        bw.Write((Half)node.Transform.Scale.X);
                        bw.Write((Half)node.Transform.Scale.Y);
                        bw.Write((Half)node.Transform.Scale.Z);
                    }
                    else
                        bw.Write(node.Transform.Scale);
                }

                bw.Write(node.ChildCount);
                bw.Write(node.MeshIndex);
            }
        }
        #endregion

        private readonly record struct ModelMetrics(uint VertexCount, uint IndexCount, uint MeshCount, uint NodeCount, uint LargestIndice);
    }

    public struct ModelProcessorArgs
    {
        public string AbsoluteFilepath;
        public string AbsoluteOutputPath;

        public bool IsCompressed;

        public ModelIndexStrideMode IndexStrideMode;

        public bool UseHalfPrecisionNodes;
        public bool UseHalfPrecisionVertices;
    }

    public enum ModelIndexStrideMode : byte
    {
        Auto = 0,
        Only16Bit,
        Only32Bit
    }

    public readonly record struct ModelMeshInfo(string Name);
}
