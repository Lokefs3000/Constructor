namespace Primary.Assets
{
    public interface IAssetIdProvider
    {
        public string? RetrievePathForId(AssetId assetId);
        public AssetId RetriveIdForPath(ReadOnlySpan<char> path);

        public const ulong Invalid = ulong.MaxValue;
    }

    public readonly record struct AssetId : IEquatable<AssetId>, IComparable<AssetId>
    {
        private readonly ulong _id;

        public AssetId() => _id = IAssetIdProvider.Invalid;
        public AssetId(ulong id) => _id = id;

        public override int GetHashCode() => _id.GetHashCode();
        public override string ToString() => _id.ToString();
        public bool Equals(AssetId other) => _id == other._id;
        public int CompareTo(AssetId other) => _id.CompareTo(other._id);

        public ulong Value => _id;
        public bool IsInvalid => _id == IAssetIdProvider.Invalid;

        public static readonly AssetId Invalid = new AssetId(IAssetIdProvider.Invalid);

        public static implicit operator AssetId(ulong id) => new AssetId(id);
        public static explicit operator ulong(AssetId id) => id._id;
    }
}
