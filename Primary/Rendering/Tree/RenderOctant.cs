using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Scenes;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace Primary.Rendering.Tree
{
    public sealed class RenderOctant
    {
        private readonly RenderOctant? _parent;

        private readonly AABB _worldBounds;
        private readonly OctreePoint _point;
        private readonly int _octantId;

        private RenderOctant[]? _octants;
        private List<SceneEntity> _children;

        internal RenderOctant(RenderOctant? parent, AABB worldBounds, Vector3 basePosition)
        {
            _parent = parent;

            _worldBounds = worldBounds;
            _point = GetOctreePointForOctant(worldBounds.Center - basePosition);
            _octantId = CraftIdFromPoint(_point);

            _octants = null;
            _children = new List<SceneEntity>();
        }

        internal void EmplaceChild(SceneEntity entity)
        {
            if (!_children.Contains(entity))
                _children.Add(entity);
        }

        internal void RemoveChild(SceneEntity entity)
        {
            Debug.Assert(_children.Contains(entity));
            _children.Remove(entity);
        }

        public RenderOctant? GetOctantAt(OctreePoint point)
        {
            if (_octants == null)
                return null;

            int idx = point.X + point.Y * 2 + point.Z * 4;
            Debug.Assert((uint)idx < _octants!.Length);

            return _octants.DangerousGetReferenceAt(idx);
        }

        internal RenderOctant[]? OctantsList { get => _octants; set => _octants = value; }
        internal List<SceneEntity> ChildrenList => _children;

        public RenderOctant? Owner => _parent;

        public AABB Boundaries => _worldBounds;
        public OctreePoint Point => _point;
        public int OctantId => _octantId;

        public ReadOnlySpan<RenderOctant> Octants => _octants;
        public IReadOnlyList<SceneEntity> Children => _children;

        public static OctreePoint GetOctreePointForOctant(Vector3 position)
        {
            const float MinimumOctantSize = RegionOctree.OctreeRegionSize / (RegionOctree.MaxOctantTreeDepth * RegionOctree.MaxOctantTreeDepth); //verify

            Vector128<float> vector = Vector128.Truncate(position.AsVector128Unsafe() / MinimumOctantSize);
            Vector128<int> vectorInt = Vector128.ConvertToInt32(vector);

            //prob dont need "Unsafe.WriteUnaligned" as it should be so fast it wont matter
            return new OctreePoint(vectorInt.GetElement(0), vectorInt.GetElement(1), vectorInt.GetElement(2));
        }

        public static int CraftIdFromPoint(OctreePoint point)
        {
            return point.X << 21 | point.Y << 10 | point.Z;
        }
    }

    public enum OctantSide : byte
    {
        BottomLeftBack = 0,
        BottomRightBack,
        TopLeftBack,
        TopRightBack,

        BottomLeftFront,
        BottomRightFront,
        TopLeftFront,
        TopRightFront,
    }
}
