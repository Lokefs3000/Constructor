using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Common.Streams;
using Primary.RHI;
using Primary.Utility;
using Serilog;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Array = System.Array;

namespace Primary.Assets.Loaders
{
    internal static unsafe class ModelAssetLoader
    {
        internal static IInternalAssetData FactoryCreateNull()
        {
            return new ModelAssetData();
        }

        internal static IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not ModelAssetData modelData)
                throw new ArgumentException(nameof(assetData));

            return new ModelAsset(modelData);
        }

        internal static void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not ModelAsset model)
                throw new ArgumentException(nameof(asset));
            if (assetData is not ModelAssetData modelData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                using Stream stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom)!;
                using BinaryReader br = new BinaryReader(stream);

                ExceptionUtility.Assert(br.ReadUInt32() == HeaderId, "Invalid header id");
                ExceptionUtility.Assert(br.ReadUInt32() == HeaderVersion, "Invalid header version");

                uint vertexCount = br.ReadUInt32();
                uint indexCount = br.ReadUInt32();

                using UnsafeArray<Vertex> vertices = new UnsafeArray<Vertex>((int)vertexCount);
                using UnsafeArray<ushort> indices = new UnsafeArray<ushort>((int)indexCount);

                RenderMesh[] meshes = System.Array.Empty<RenderMesh>();

                {

                    Vertex* verticesStart = (Vertex*)Unsafe.AsPointer(ref vertices[0]);
                    ushort* indicesStart = (ushort*)Unsafe.AsPointer(ref indices[0]);

                    Vertex* verticesBaseRef = verticesStart;
                    ushort* indicesBaseRef = indicesStart;

                    uint vtxOffset = 0;
                    uint idxOffset = 0;

                    ushort count = br.ReadUInt16();
                    meshes = new RenderMesh[count];

                    for (int i = 0; i < count; i++)
                    {
                        string meshName = br.ReadString();

                        uint meshVertexCount = br.ReadUInt32();
                        uint meshIndexCount = br.ReadUInt32();
                        byte numUVs = br.ReadByte();

                        ExceptionUtility.Assert((numUVs & 0b00000001) == 1, "UV style not implemented yet");

                        uint material = br.ReadUInt32();

                        meshes[i] = new RenderMesh(model, meshName, vtxOffset, idxOffset, meshIndexCount);

                        br.Read(verticesStart, (int)meshVertexCount);
                        br.Read(indicesStart, (int)meshIndexCount);

                        verticesStart += meshVertexCount;
                        indicesStart += meshIndexCount;

                        vtxOffset += meshVertexCount;
                        idxOffset += meshIndexCount;
                    }
                }

                br.Skip(4);

                ModelNode rootNode = new ModelNode(model, null, Array.Empty<ModelNode>(), string.Empty, Array.Empty<string>()); //DeserializeNodeAndChildren(br, meshes, model, null);

                GraphicsDevice device = Engine.GlobalSingleton.RenderingManager.GraphicsDevice;

                RHI.Buffer vertexBuffer = device.CreateBuffer(new BufferDescription
                {
                    ByteWidth = (uint)(vertices.Count * sizeof(Vertex)),
                    Stride = (uint)sizeof(Vertex),
                    Memory = MemoryUsage.Immutable,
                    Usage = BufferUsage.VertexBuffer,
                    Mode = BufferMode.None,
                    CpuAccessFlags = CPUAccessFlags.None
                }, (nint)Unsafe.AsPointer(ref vertices.AsSpan().DangerousGetReference()));

                RHI.Buffer indexBuffer = device.CreateBuffer(new BufferDescription
                {
                    ByteWidth = (uint)(indices.Count * sizeof(ushort)),
                    Stride = (uint)sizeof(ushort),
                    Memory = MemoryUsage.Immutable,
                    Usage = BufferUsage.IndexBuffer,
                    Mode = BufferMode.None,
                    CpuAccessFlags = CPUAccessFlags.None
                }, (nint)Unsafe.AsPointer(ref indices.AsSpan().DangerousGetReference()));

                vertexBuffer.Name = sourcePath + " - Vertex buffer";
                indexBuffer.Name = sourcePath + " - Index buffer";

                modelData.UpdateAssetData(model, meshes, rootNode, vertexBuffer, indexBuffer);
            }
#if DEBUG
            finally
            {

            }
#else
            catch (Exception ex)
            {
                modelData.UpdateAssetFailed(model);
                Log.Error(ex, "Failed to load model: {name}", sourcePath);
            }
#endif
        }

        private static ModelNode DeserializeNodeAndChildren(BinaryReader br, RenderMesh[] renderMeshes, ModelAsset asset, ModelNode? parent)
        {
            string name = br.ReadString();
            Matrix4x4 transform = br.Read<Matrix4x4>();

            ushort meshCount = br.ReadUInt16();
            string[] meshes = meshCount > 0 ? new string[meshCount] : Array.Empty<string>();
            if (meshCount > 0)
            {
                for (int i = 0; i < meshCount; i++)
                {
                    meshes[i] = renderMeshes[br.ReadUInt32()].Id;
                }
            }

            ushort nodeCount = br.ReadUInt16();
            ModelNode[] children = nodeCount > 0 ? new ModelNode[nodeCount] : Array.Empty<ModelNode>();

            ModelNode newNode = new ModelNode(asset, parent, children, name, meshes);

            if (nodeCount > 0)
            {
                for (int i = 0; i < nodeCount; i++)
                {
                    children[i] = DeserializeNodeAndChildren(br, renderMeshes, asset, newNode);
                }
            }

            return newNode;
        }

        private static uint HeaderId = 0x46444d45;
        private static uint HeaderVersion = 0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 Bitangent;
        public Vector2 UV;
    }
}
