namespace Primary.Assets
{
    internal interface IInternalAssetData : IDisposable
    {
        public Type AssetType { get; }

        public void PromoteStateToRunning();
        public void ResetInternalState();
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
