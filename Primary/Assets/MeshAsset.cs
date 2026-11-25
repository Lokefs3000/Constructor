using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using Primary.Mathematics;
using Primary.Rendering.Assets;

namespace Primary.Assets
{
    public sealed class MeshAsset : BaseAssetDefinition<MeshAsset, MeshAssetData>, IRawRenderMesh
    {
        internal MeshAsset(MeshAssetData assetData) : base(assetData)
        {

        }

        public ModelAsset? ParentAsset => AssetData.ParentAsset;
        
        IRenderMeshSource? IRawRenderMesh.MeshSource => IsLoaded && (ParentAsset?.IsLoaded ?? false) ? ParentAsset.InternalAssetData : null;
        public AABB Boundaries => AssetData.Boundaries;
        public ref readonly RenderMeshDrawArgs Args => ref AssetData.Args;
        public int MeshIndex => AssetData.MeshIndex;

        int IRawRenderMesh.UniqueId => Id.LocalId;
    }

    public sealed class MeshAssetData : BaseInternalAssetData<MeshAsset>
    {
        private ModelAsset? _parentAsset;

        private AABB _boundaries;
        private RenderMeshDrawArgs _drawArgs;

        private ushort _meshIndex;

        internal MeshAssetData(AssetId id, int uniqueIndex) : base(id, uniqueIndex)
        {
            _parentAsset = null;

            _boundaries = AABB.Zero;
            _drawArgs = default;

            _meshIndex = ushort.MaxValue;
        }

        public void UpdateAssetData(MeshAsset asset, ModelAsset parentAsset, AABB boundaries, RenderMeshDrawArgs drawArgs, ushort meshIndex)
        {
            base.UpdateAssetData(asset);

            _parentAsset = parentAsset;

            _boundaries = boundaries;
            _drawArgs = drawArgs;

            _meshIndex = meshIndex;
        }

        public ModelAsset? ParentAsset => _parentAsset;

        public AABB Boundaries => _boundaries;
        public ref readonly RenderMeshDrawArgs Args => ref _drawArgs;

        public ushort MeshIndex => _meshIndex;
    }
}
