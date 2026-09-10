using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using K4os.Compression.LZ4.Streams;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.Rendering.Assets;
using Primary.RHI;
using Primary.Utility;

namespace Primary.Assets.Loaders
{
    internal unsafe class ModelAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new ModelAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            return new ModelAsset((ModelAssetData)assetData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, string localPath, BundleReader? bundleToReadFrom)
        {
            ModelAsset modelAsset = (ModelAsset)asset;
            ModelAssetData modelAssetData = (ModelAssetData)assetData;

            modelAssetData.Dispose();

            using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom) ?? throw new AssetLoadException("Failed to open asset stream");

            FileModelHeader header = stream.Read<FileModelHeader>();
            if (header.FileHeader != FileModelHeader.Header)
                throw new AssetLoadException("Model header does not match with expected value");
            if (header.FileVersion != FileModelHeader.Version)
                throw new AssetLoadException("Model header is in a different version than what is supported");

            using RentedArray<byte> rawVertexData = new RentedArray<byte>((int)(header.VertexCount * header.VertexStride));
            using RentedArray<byte> rawIndexData = new RentedArray<byte>((int)(header.IndexCount * header.IndexStride));

            int vertexOffset = 0;
            int indexOffset = 0;

            LazyMesh[] meshes = new LazyMesh[header.MeshCount];
            for (int i = 0; i < header.MeshCount; ++i)
            {
                FileModelMesh mesh = stream.Read<FileModelMesh>();
                string meshName = stream.ReadStringUtf16(mesh.NameLength);

                stream.ReadExactly(rawVertexData.Span.Slice(vertexOffset * header.VertexStride, (int)(mesh.VertexCount * header.VertexStride)));
                stream.ReadExactly(rawIndexData.Span.Slice(indexOffset * header.IndexStride, (int)(mesh.IndexCount * header.IndexStride)));

                vertexOffset += (int)mesh.VertexCount;
                indexOffset += (int)mesh.IndexCount;

                Lazy<MeshAsset> lazy = new Lazy<MeshAsset>(() =>
                {
                    return AssetManager.LoadAsset<MeshAsset>(modelAssetData.Id.WithLocalId(mesh.LocalId));
                }, LazyThreadSafetyMode.ExecutionAndPublication);

                meshes[i] = new LazyMesh(lazy, mesh.LocalId, meshName);
            }

#if DEBUG
            Span<float> vtxDbg = MemoryMarshal.Cast<byte, float>(rawVertexData.Span);
            Span<ushort> idx16Dbg = MemoryMarshal.Cast<byte, ushort>(rawIndexData.Span);
            Span<uint> idx32Dbg = MemoryMarshal.Cast<byte, uint>(rawIndexData.Span);
#endif

            RHIBuffer? vertexBuffer = null;
            RHIBuffer? indexBuffer = null;
            try
            {
                vertexBuffer = RHIDevice.Instance!.CreateBuffer(new RHIBufferDescription
                {
                    Width = (uint)rawVertexData.Count,
                    Stride = header.VertexStride,
                    Usage = RHIResourceUsage.VertexInput
                }, rawVertexData.Span, $"{localPath} Vtx");

                indexBuffer = RHIDevice.Instance!.CreateBuffer(new RHIBufferDescription
                {
                    Width = (uint)rawIndexData.Count,
                    Stride = header.IndexStride,
                    Usage = RHIResourceUsage.IndexInput
                }, rawIndexData.Span, $"{localPath} Idx");
            }
            catch (Exception)
            {
                vertexBuffer?.Dispose();
                indexBuffer?.Dispose();
                throw;
            }

            ModelNode rootNode = ReadNodeInStream(modelAsset, stream, meshes);
            modelAssetData.UpdateAssetData(modelAsset, [.. meshes], rootNode, vertexBuffer!, indexBuffer!);
        }

        static ModelNode ReadNodeInStream(ModelAsset asset, Stream stream, ReadOnlySpan<LazyMesh> meshes)
        {
            FileModelNode node = stream.Read<FileModelNode>();
            string nodeName = stream.ReadStringUtf16(node.NameLength);

            Vector3 position = Vector3.Zero;
            Quaternion rotation = Quaternion.Identity;
            Vector3 scale = Vector3.One;

            if (node.Transform.HasFlags(NodeTransformFeatures.Position))
                position = stream.Read<Vector3>();
            if (node.Transform.HasFlags(NodeTransformFeatures.Rotation))
                rotation = stream.Read<Quaternion>();
            if (node.Transform.HasFlags(NodeTransformFeatures.UniformScale))
                scale = new Vector3(stream.Read<float>());
            else if (node.Transform.HasFlags(NodeTransformFeatures.Scale))
                scale = stream.Read<Vector3>();

            ImmutableArray<ModelNode> childNodes = ImmutableArray<ModelNode>.Empty;
            if (node.ChildCount > 0)
            {
                using RentedArray<ModelNode> tempNodeArray = new RentedArray<ModelNode>(node.ChildCount);
                for (int i = 0; i < node.ChildCount; ++i)
                {
                    tempNodeArray[i] = ReadNodeInStream(asset, stream, meshes);
                }

                childNodes = [.. tempNodeArray];
            }

            return new ModelNode(asset, childNodes, new ModelTransform(position, rotation, scale), nodeName, node.MeshIndex == ushort.MaxValue ? null : meshes[node.MeshIndex].Asset);
        }
    }

    public struct FileModelHeader
    {
        public uint FileHeader;
        public uint FileVersion;

        public uint VertexCount;
        public uint IndexCount;

        public byte VertexStride;
        public byte IndexStride;

        public ushort MeshCount;

        public const uint Header = 0x204c444d;
        public const uint Version = 1;
    }

    public struct FileModelMesh
    {
        public int LocalId;

        public uint VertexCount;
        public uint IndexCount;

        public byte NameLength;
    }

    public struct FileModelNode
    {
        public byte NameLength;
        public ushort MeshIndex;

        public NodeTransformFeatures Transform;

        public ushort ChildCount;
    }

    public enum NodeTransformFeatures : byte
    {
        None = 0,

        Position = 1 << 0,
        Rotation = 1 << 1,
        UniformScale = 1 << 2,
        Scale = 1 << 3
    }
}
