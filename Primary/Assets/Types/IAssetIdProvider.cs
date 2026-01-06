using System.Numerics;
using System.Runtime.InteropServices;

namespace Primary.Assets.Types
{
    public interface IAssetIdProvider
    {
        public string? RetrievePathForId(AssetId assetId);
        public AssetId RetriveIdForPath(ReadOnlySpan<char> path);

        public const ulong Invalid = ulong.MaxValue;
        public const uint InvalidAssetId = uint.MaxValue;
        public const uint InvalidProjectId = uint.MaxValue;
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly record struct AssetId : IEquatable<AssetId>, IComparable<AssetId>, IComparisonOperators<AssetId, AssetId, bool>, IEqualityOperators<AssetId, AssetId, bool>
    {
        [FieldOffset(0)]
        private readonly ulong _id;

        [FieldOffset(0)]
        private readonly uint _projectId;
        [FieldOffset(4)]
        private readonly uint _assetId;

        public AssetId() => _id = IAssetIdProvider.Invalid;
        public AssetId(ulong id) => _id = id;
        public AssetId(uint project, uint id) => _id = project | (ulong)id << 32;

        public override int GetHashCode() => _id.GetHashCode();
        public override string ToString() => _id.ToString();
        public bool Equals(AssetId other) => _id == other._id;
        public int CompareTo(AssetId other) => _id.CompareTo(other._id);

        public ulong Value => _id;
        public bool IsInvalid => _id == IAssetIdProvider.Invalid;

        public uint ProjectId => _projectId;
        public uint Id => _assetId;

        public static readonly AssetId Invalid = new AssetId(IAssetIdProvider.Invalid);

        public static explicit operator AssetId(ulong id) => new AssetId(id);
        public static implicit operator ulong(AssetId id) => id._id;

        public static bool operator >(AssetId left, AssetId right) => left._id > right._id;
        public static bool operator >=(AssetId left, AssetId right) => left._id >= right._id;
        public static bool operator <(AssetId left, AssetId right) => left._id < right._id;
        public static bool operator <=(AssetId left, AssetId right) => left._id <= right._id;
    }
}
