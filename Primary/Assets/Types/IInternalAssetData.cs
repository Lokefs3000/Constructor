namespace Primary.Assets.Types
{
    public interface IInternalAssetData : IDisposable
    {
        public AssetId Id { get; }
        public IAssetDefinition? Definition { get; }

        public ResourceStatus Status { get; }
        public string Name { get; }

        public int LoadIndex { get; }

        public Type AssetType { get; }

        /// <summary>Thread-safe</summary>
        public void SetAssetInternalStatus(ResourceStatus status);
        /// <summary>Thread-safe</summary>
        public void SetAssetInternalName(string name);

        /// <summary>Thread-safe</summary>
        protected internal void UpdateAssetFailed(IAssetDefinition asset);
    }

    public enum ResourceStatus : byte
    {
        Pending = 0,
        Running,
        Error,
        Bad,
        Disposed,
        Success
    }
}
