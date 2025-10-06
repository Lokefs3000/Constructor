namespace Primary.Assets
{
    public interface IInternalAssetData : IDisposable
    {
        public Type AssetType { get; }
        public IAssetDefinition? Definition { get; }

        /// <summary>Thread-safe</summary>
        public void SetAssetInternalStatus(ResourceStatus status);
        /// <summary>Thread-safe</summary>
        public void SetAssetInternalName(string name);
    }

    public enum ResourceStatus : byte
    {
        Pending = 0,
        Running,
        Error,
        Disposed,
        Success
    }
}
