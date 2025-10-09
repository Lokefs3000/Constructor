using Primary.Common;
using Primary.Components;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.Tree
{
    public sealed class RenderTreeManager
    {
        private Dictionary<TreeRegion, RenderTree> _treeRegions;

        internal RenderTreeManager()
        {
            _treeRegions = new Dictionary<TreeRegion, RenderTree>();
        }

        internal void AddEntityToTree(SceneEntity entity)
        {
            ref WorldTransform transform = ref entity.GetComponent<WorldTransform>();
            AABB entityMaxBounds = AABB.Zero;

            Vector3 regionXYZf = Vector3.Round(entityMaxBounds.Center / Constants.rRenderTreeRegionSize);
            TreeRegion region = new TreeRegion((int)regionXYZf.X, (int)regionXYZf.Y, (int)regionXYZf.Z);

            RenderTree tree = GetTreeInRegion(region);
            tree.AddEntityToTree(entity);
        }
    }

    public readonly record struct TreeRegion(int RegionX, int RegionY, int RegionZ)
    {
        public override int GetHashCode()
        {
            return HashCode.Combine(RegionX, RegionY, RegionZ);
        }
    }
}
