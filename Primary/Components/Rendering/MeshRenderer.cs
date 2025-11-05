using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Components.Rendering
{
    public struct MeshRenderer : IComponent
    {
        public MaterialAsset[] Materials;
        public ShadowCastingMode Mode;

        public MeshRenderer()
        {
            Materials = Array.Empty<MaterialAsset>();
            Mode = ShadowCastingMode.Default;
        }
    }

    public enum ShadowCastingMode : byte
    {
        Default = 0,
        Disabled,
        DoubleSided,
        ShadowsOnly,
    }
}
