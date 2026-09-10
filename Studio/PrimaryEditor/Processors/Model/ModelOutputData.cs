using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;

namespace PrimaryEditor.Processors.Model
{
    public readonly record struct ModelOutputData(int VertexStride, int IndexStride, ImmutableArray<ModelMeshInfo> Meshes, ImmutableArray<ModelNodeInfo> Nodes);

    public readonly record struct ModelMeshInfo(string MeshName, int VertexCount, int IndexCount, byte[] RawVertexData, byte[] RawIndexData);
    public readonly record struct ModelNodeInfo(string NodeName, ushort MeshIndex, int ChildrenCount, ModelTransformInfo? Transform);
    public readonly record struct ModelTransformInfo(Vector3 Position, Quaternion Orientation, Vector3 Scale);
}
