using CommunityToolkit.HighPerformance;
using K4os.Compression.LZ4.Streams;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.RHI2;
using Primary.Utility;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

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
            if (assetData is not ModelAssetData modelData)
                throw new ArgumentException(nameof(assetData));

            return new ModelAsset(modelData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not ModelAsset model)
                throw new ArgumentException(nameof(asset));
            if (assetData is not ModelAssetData modelData)
                throw new ArgumentException(nameof(assetData));

            RHIBuffer? vertexBuffer = null;
            RHIBuffer? indexBuffer = null;

            try
            {
                using Stream stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom)!;

                PMFHeader header = CommunityToolkit.HighPerformance.StreamExtensions.Read<PMFHeader>(stream);

                if (header.Header != PMFHeader.ConstHeader)
                    ThrowException("Invalid header present in file: {header} ({utf8})", header.Header, Encoding.UTF8.GetString(MemoryMarshal.Cast<uint, byte>(new ReadOnlySpan<uint>(in header.Header))));
                if (header.Version != PMFHeader.ConstVersion)
                    ThrowException("Invalid version present in file: {version}", header.Version);

                Stream dataReadStream = stream;
                if (FlagUtility.HasFlag(header.Flags, PMFHeaderFlags.IsCompressed))
                    dataReadStream = LZ4Stream.Decode(stream);

                using BinaryReader br = new BinaryReader(dataReadStream);

                RenderMesh[] meshes = new RenderMesh[header.MeshCount];

                int vertexDataOffset = 0;
                int indexDataOffset = 0;

                uint indexOffsetTotal = 0;

                byte[] vertexData = new byte[header.VertexTotalSize];
                byte[] indexData = new byte[header.IndexCount * (FlagUtility.HasFlag(header.Flags, PMFHeaderFlags.LargeIndices) ? sizeof(uint) : sizeof(ushort))];

                for (int i = 0; i < meshes.Length; i++)
                {
                    PMFMesh mesh = new PMFMesh
                    {
                        Name = br.ReadString(),

                        VertexCount = br.ReadUInt32(),
                        IndexCount = br.ReadUInt32(),

                        VertexStride = br.ReadUInt16(),
                        UVChannelMask = br.ReadByte()
                    };

                    int uvChannelCount = 8 - byte.LeadingZeroCount(mesh.UVChannelMask);
                    if (uvChannelCount > 1)
                        throw new NotImplementedException();

                    Span<float> vertices = MemoryMarshal.Cast<byte, float>(new Span<byte>(vertexData, vertexDataOffset, (int)mesh.VertexCount * (12 + (uvChannelCount * 2)) * sizeof(float)));

                    if (FlagUtility.HasFlag(header.Flags, PMFHeaderFlags.HalfVertexValues))
                    {
                        using PoolArray<Half> pool = ArrayPool<Half>.Shared.Rent(vertices.Length);
                        br.Read(pool.AsSpan(0, vertices.Length));

                        for (int j = 0; j < pool.Array.Length; j++)
                        {
                            vertices[j] = (float)pool[j];
                        }
                    }
                    else
                    {
                        br.Read(vertices);
                    }

                    vertexDataOffset += vertices.Length * sizeof(float);

                    if (FlagUtility.HasFlag(header.Flags, PMFHeaderFlags.LargeIndices))
                    {
                        int indexDataSize = (int)(mesh.IndexCount * sizeof(uint));
                        Span<uint> indices = MemoryMarshal.Cast<byte, uint>(new Span<byte>(indexData, indexDataOffset, indexDataSize));

                        br.Read(indices);

                        indexDataOffset += indexDataSize;
                    }
                    else
                    {
                        int indexDataSize = (int)(mesh.IndexCount * sizeof(ushort));
                        Span<ushort> indices = MemoryMarshal.Cast<byte, ushort>(new Span<byte>(indexData, indexDataOffset, indexDataSize));

                        br.Read(indices);

                        indexDataOffset += indexDataSize;
                    }

                    int totalChannels = 12 + (uvChannelCount * 2);

                    AABB aabb = new AABB(Unsafe.As<float, Vector3>(ref vertices[0]), Unsafe.As<float, Vector3>(ref vertices[0]));

                    for (int j = totalChannels; j < vertices.Length; j += totalChannels)
                    {
                        Vector3 pos = Unsafe.As<float, Vector3>(ref vertices[j]);

                        aabb.Minimum = Vector3.Min(aabb.Minimum, pos);
                        aabb.Maximum = Vector3.Max(aabb.Maximum, pos);
                    }

                    meshes[i] = new RenderMesh(modelData, i, mesh.Name, aabb, 0, indexOffsetTotal, mesh.IndexCount);
                    indexOffsetTotal += mesh.IndexCount;
                }

                List<ModelNode> rootNodeChildren = new List<ModelNode>();
                for (int i = 0; i < header.NodeCount;)
                {
                    rootNodeChildren.Add(RecursiveReadNodes(ref i));
                }

                //ModelNode rootNode = new ModelNode(model, null, rootNodeChildren.ToArray(), new ModelTransform(Vector3.Zero, Quaternion.Identity, Vector3.One), string.Empty, null);

                ModelNode RecursiveReadNodes(ref int i, ModelNode? parent = null)
                {
                    i++;
                    PMFNode node = new PMFNode
                    {
                        Name = br.ReadString(),
                        Transform = ReadTransform(br, FlagUtility.HasFlag(header.Flags, PMFHeaderFlags.HalfNodeTransforms)),

                        ChildCount = br.ReadUInt16(),

                        MeshIndex = br.ReadUInt16()
                    };

                    if (node.ChildCount == 0)
                    {
                        return new ModelNode(
                            model,
                            parent,
                            Array.Empty<ModelNode>(),
                            new ModelTransform(node.Transform.Position, node.Transform.Rotation, node.Transform.Scale),
                            node.Name,
                            node.MeshIndex == ushort.MaxValue ? null : meshes[node.MeshIndex].Id);
                    }

                    ModelNode[] nodes = new ModelNode[node.ChildCount];
                    for (int j = 0; j < node.ChildCount; j++, i++)
                    {
                        nodes[j] = RecursiveReadNodes(ref i);
                    }

                    return new ModelNode(
                            model,
                            parent,
                            nodes,
                            new ModelTransform(node.Transform.Position, node.Transform.Rotation, node.Transform.Scale),
                            node.Name,
                            node.MeshIndex == ushort.MaxValue ? null : meshes[node.MeshIndex].Id);
                }

                unsafe
                {
                    //vertex buffer
                    fixed (byte* ptr = vertexData)
                    {
                        vertexBuffer = RHIDevice.Instance!.CreateBuffer(new RHIBufferDescription
                        {
                            Width = (uint)vertexData.Length,
                            Stride = 12 * sizeof(float) + 2 * sizeof(float),

                            Usage = RHIResourceUsage.VertexInput,
                        }, (nint)ptr);
                    }

                    //index buffer
                    fixed (byte* ptr = indexData)
                    {
                        indexBuffer = RHIDevice.Instance!.CreateBuffer(new RHIBufferDescription
                        {
                            Width = (uint)indexData.Length,
                            Stride = FlagUtility.HasFlag(header.Flags, PMFHeaderFlags.LargeIndices) ? 4 : 2,

                            Usage = RHIResourceUsage.IndexInput
                        }, (nint)ptr);
                    }
                }

                Debug.Assert(vertexBuffer != null && indexBuffer != null);

                modelData.UpdateAssetData(model, meshes, rootNodeChildren[0], vertexBuffer, indexBuffer);

                vertexBuffer = null;
                indexBuffer = null;

                static PMFTransform ReadTransform(BinaryReader br, bool halfPrecision)
                {
                    PMFTransform transform = new PMFTransform
                    {
                        Features = br.Read<PMFTransformFeatures>(),

                        Position = Vector3.Zero,
                        Rotation = Quaternion.Identity,
                        Scale = Vector3.One
                    };

                    if (FlagUtility.HasFlag(transform.Features, PMFTransformFeatures.Position))
                    {
                        transform.Position = halfPrecision ?
                            new Vector3((float)br.ReadHalf(), (float)br.ReadHalf(), (float)br.ReadHalf()) :
                            new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    }

                    if (FlagUtility.HasFlag(transform.Features, PMFTransformFeatures.Rotation))
                    {
                        transform.Rotation = halfPrecision ?
                            new Quaternion((float)br.ReadHalf(), (float)br.ReadHalf(), (float)br.ReadHalf(), (float)br.ReadHalf()) :
                            new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    }

                    if (FlagUtility.HasFlag(transform.Features, PMFTransformFeatures.UniformScale))
                    {
                        transform.Scale = new Vector3(halfPrecision ?
                            (float)br.ReadHalf() :
                            br.ReadSingle());
                    }

                    if (FlagUtility.HasFlag(transform.Features, PMFTransformFeatures.Scale))
                    {
                        transform.Scale = halfPrecision ?
                            new Vector3((float)br.ReadHalf(), (float)br.ReadHalf(), (float)br.ReadHalf()) :
                            new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    }

                    return transform;
                }

                void ThrowException(string message, params object?[] args)
                {
                    EngLog.Assets.Error("[a:{path}]: " + message, [sourcePath, .. args]);
                    throw new Exception("Unexpected error");
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                modelData.UpdateAssetFailed(model);
                EngLog.Assets.Error(ex, "Failed to load model: {name}", sourcePath);
            }
#endif
            finally
            {
                vertexBuffer?.Dispose();
                indexBuffer?.Dispose();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct PMFHeader
    {
        public uint Header;
        public uint Version;

        public PMFHeaderFlags Flags;

        public uint VertexTotalSize;
        public uint IndexCount;

        public ushort MeshCount;
        public ushort NodeCount;

        public const uint ConstHeader = 0x4d502046;
        public const uint ConstVersion = 1;
    }

    public enum PMFHeaderFlags : byte
    {
        None = 0,

        IsCompressed = 1 << 0,

        LargeIndices = 1 << 1,

        HalfNodeTransforms = 1 << 2,
        HalfVertexValues = 1 << 3,
    }

    public record struct PMFMesh
    {
        public string Name;

        public uint VertexCount;
        public uint IndexCount;

        public ushort VertexStride;
        public byte UVChannelMask;
    }

    public record struct PMFNode
    {
        public string Name;
        public PMFTransform Transform;

        public ushort ChildCount;

        public ushort MeshIndex;
    }

    public record struct PMFTransform
    {
        public PMFTransformFeatures Features;

        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public enum PMFTransformFeatures : byte
    {
        None = 0,

        Position = 1 << 0,
        Rotation = 1 << 1,
        UniformScale = 1 << 2,
        Scale = 1 << 3,
    }
}
