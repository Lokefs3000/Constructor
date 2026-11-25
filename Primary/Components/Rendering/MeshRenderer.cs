using Primary.Assets;

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
