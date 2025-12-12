namespace Primary.Assets.Types
{
    public interface IAssetDefinition
    {
        public ResourceStatus Status { get; }

        public string Name { get; }
        public AssetId Id { get; }
    }
}
