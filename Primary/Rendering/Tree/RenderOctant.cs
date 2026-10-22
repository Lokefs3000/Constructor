using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Components;
using Primary.Mathematics;
using Primary.Scenes;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Primary.Rendering.Tree
{
    public sealed class RenderOctant
    {
        private readonly RenderOctant? _parent;

        private readonly AABB _bounds;
        private AABB _worldBounds;
        private readonly OctreePoint _point;
        private readonly long _octantId;
        private readonly int _depth;

        private RenderOctant[]? _octants;
        private List<SceneEntity> _children;

        internal RenderOctant(RenderOctant? parent, AABB worldBounds, Vector3 basePosition)
        {
            _parent = parent;

            _bounds = worldBounds;
            _worldBounds = worldBounds;
            _point = GetOctreePointForOctant(worldBounds.Center - basePosition);
            _octantId = CraftIdFromPoint(_point);
            _depth = parent != null ? parent._depth + 1 : 0;

            _octants = null;
            _children = new List<SceneEntity>();
        }

        internal void EmplaceChild(SceneEntity entity, float oversize)
        {
            if (!_children.Contains(entity))
            {
                if (oversize > 0.0f)
                {
                    ref RenderBounds bounds = ref entity.GetComponent<RenderBounds>();
                    _worldBounds = AABB.Union(_worldBounds, bounds.ComputedBounds);
                }

                _children.Add(entity);
            }
        }

        internal void RemoveChild(SceneEntity entity)
        {
            Debug.Assert(_children.Contains(entity));
            _children.Remove(entity);
        }

        internal void OversizeInternalBoundaries(AABB oversizeBounds)
        {
            _worldBounds = AABB.Union(_worldBounds, oversizeBounds);
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

        public AABB SourceBoundaries => _bounds;
        public AABB Boundaries => _worldBounds;
        public OctreePoint Point => _point;
        public long OctantId => _octantId;
        public int Depth => _depth;

        public ReadOnlySpan<RenderOctant> Octants => _octants;
        public ROList<SceneEntity> Children => _children;

        public static OctreePoint GetOctreePointForOctant(Vector3 position)
        {
            const float MinimumOctantSize = RegionOctree.OctreeRegionSize / (RegionOctree.MaxOctantTreeDepth * RegionOctree.MaxOctantTreeDepth); //verify

            Vector128<float> vector = Vector128.Truncate(position.AsVector128Unsafe() / MinimumOctantSize);
            Vector128<int> vectorInt = Vector128.ConvertToInt32(vector);

            //prob dont need "Unsafe.WriteUnaligned" as it should be so fast it wont matter
            return new OctreePoint((short)vectorInt.GetElement(0), (short)vectorInt.GetElement(1), (short)vectorInt.GetElement(2));
        }

        public static long CraftIdFromPoint(OctreePoint point)
        {
            return Unsafe.BitCast<ulong, long>((ulong)((ushort)point.X) << 32 | (ulong)((ushort)point.Y) << 16 | (ulong)((ushort)point.Z));
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
