using Primary.Assets;

namespace Primary.Components
{
    public struct PostProcessingVolume : IComponent
    {
        public VolumeBoundaries Boundaries;
        public int Priority;

        public PostProcessingVolumeAsset? Asset;
    }

    public enum VolumeBoundaries : byte
    {
        Global = 0,
    }
}
