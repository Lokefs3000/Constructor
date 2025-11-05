using System.Numerics;

namespace Primary.Assets
{
    public interface IAssetIdProvider
    {
        public string? RetrievePathForId(AssetId assetId);
        public AssetId RetriveIdForPath(ReadOnlySpan<char> path);

        public const uint Invalid = uint.MaxValue;
    }

    public readonly record struct AssetId : IEquatable<AssetId>, IComparable<AssetId>, IComparisonOperators<AssetId, AssetId, bool>, IEqualityOperators<AssetId, AssetId, bool>
    {
        private readonly uint _id;

        public AssetId() => _id = IAssetIdProvider.Invalid;
        public AssetId(uint id) => _id = id;

        public override int GetHashCode() => _id.GetHashCode();
        public override string ToString() => _id.ToString();
        public bool Equals(AssetId other) => _id == other._id;
        public int CompareTo(AssetId other) => _id.CompareTo(other._id);

        public uint Value => _id;
        public bool IsInvalid => _id == IAssetIdProvider.Invalid;

        public static readonly AssetId Invalid = new AssetId(IAssetIdProvider.Invalid);

        public static explicit operator AssetId(uint id) => new AssetId(id);
        public static implicit operator uint(AssetId id) => id._id;

        public static bool operator >(AssetId left, AssetId right) => left._id > right._id;
        public static bool operator >=(AssetId left, AssetId right) => left._id >= right._id;
        public static bool operator <(AssetId left, AssetId right) => left._id < right._id;
        public static bool operator <=(AssetId left, AssetId right) => left._id <= right._id;
    }
}
