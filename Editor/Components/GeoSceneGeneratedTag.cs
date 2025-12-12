using Primary.Assets.Types;
using Primary.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Components
{
    [ComponentRequirements(typeof(DontSerializeTag))]
    internal struct GeoSceneGeneratedTag : IComponent
    {
        public AssetId SceneAssetId;
    }
}
