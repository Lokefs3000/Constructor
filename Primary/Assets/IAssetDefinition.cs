namespace Primary.Assets
{
    public interface IAssetDefinition
    {
        public ResourceStatus Status { get; }

        public string Name { get; }
        public AssetId Id { get; }
    }
}
