using Primary.Serialization.Json;
using Primary.Serialization.Toml;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.Json.Serialization;
using Tomlyn.Serialization;

namespace Primary.Assets.Types
{
    public interface IAssetIdProvider
    {
        /// <summary>Thread-safe</summary>
        public bool TryGetPathForId(AssetId assetId, bool getLocalPath, [NotNullWhen(true)] out string? value);

        /// <summary>Thread-safe</summary>
        public bool TryGetAnyPathForId(AssetId assetId, [NotNullWhen(true)] out string? value);

        /// <summary>Thread-safe</summary>
        public bool TryGetLocalAndAssetPathsForId(AssetId assetId, [NotNullWhen(true)] out string? localPath, [MaybeNullWhen(true)] out string? assetPath);

        /// <summary>Thread-safe</summary>
        public bool TryLookupIdForPath(ReadOnlySpan<char> path, [NotNullWhen(true)] out AssetId value);

        /// <summary>Thread-safe</summary>
        public bool IsIdValid(AssetId assetId);

        /// <summary>Thread-safe</summary>
        public bool DoesPathHaveLookup(ReadOnlySpan<char> path);

        /// <inheritdoc cref="TryGetPathForId(AssetId, bool, out string?)" />
        public bool TryGetLocalPathForId(AssetId assetId, [NotNullWhen(true)] out string? value) => TryGetPathForId(assetId, true, out value);

        /// <inheritdoc cref="TryGetPathForId(AssetId, bool, out string?)" />
        public bool TryGetAssetPathForId(AssetId assetId, [NotNullWhen(true)] out string? value) => TryGetPathForId(assetId, false, out value);

        public static readonly AssetId Invalid = new AssetId(Guid.Empty);
    }

    [JsonConverter(typeof(AssetIdJsonConverter)), TomlConverter(typeof(AssetIdTomlConverter))]
    public readonly record struct AssetId : IEquatable<AssetId>, IComparable<AssetId>, IFormattable, ISpanFormattable, IUtf8SpanFormattable, IComparisonOperators<AssetId, AssetId, bool>, IEqualityOperators<AssetId, AssetId, bool>
    {
        // assume little endian architecture
        private readonly ulong _high;
        private readonly ulong _low;

        public AssetId() => this = IAssetIdProvider.Invalid;
        public AssetId(ulong high, ulong low) { _high = high; _low = low; }
        public AssetId(Guid guid) => this = Unsafe.BitCast<Guid, AssetId>(guid);

        /// <summary>Produces same result as <see cref="Guid.GetHashCode"/></summary>
        public override int GetHashCode()
        {
            ref int r = ref Unsafe.As<ulong, int>(ref Unsafe.AsRef(in _high));
            return r ^ Unsafe.Add(ref r, 1) ^ Unsafe.Add(ref r, 2) ^ Unsafe.Add(ref r, 3);
        }

        public override string ToString() => ToString("N", CultureInfo.InvariantCulture);
        public string ToString([StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format) => ToString(format, CultureInfo.InvariantCulture);
        public string ToString(IFormatProvider? formatProvider) => ToString("N", formatProvider);
        public string ToString([StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format, IFormatProvider? formatProvider) => Unsafe.BitCast<AssetId, Guid>(this).ToString(format, formatProvider);

        public bool TryFormat(Span<char> destination, out int charsWritten) => Unsafe.BitCast<AssetId, Guid>(this).TryFormat(destination, out charsWritten, "N");
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) => Unsafe.BitCast<AssetId, Guid>(this).TryFormat(utf8Destination, out bytesWritten, "N");

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format) => Unsafe.BitCast<AssetId, Guid>(this).TryFormat(destination, out charsWritten, format);
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format) => Unsafe.BitCast<AssetId, Guid>(this).TryFormat(utf8Destination, out bytesWritten, format);

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format, IFormatProvider? provider) => Unsafe.BitCast<AssetId, Guid>(this).TryFormat(destination, out charsWritten, format);
        bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format, IFormatProvider? provider) => Unsafe.BitCast<AssetId, Guid>(this).TryFormat(utf8Destination, out bytesWritten, format);

        public bool Equals(AssetId other)
        {
            if (Vector128.IsHardwareAccelerated)
                return Unsafe.BitCast<AssetId, Vector128<byte>>(this) == Unsafe.BitCast<AssetId, Vector128<byte>>(other);
            else
                return _high == other._high && _low == other._low;
        }

        public int CompareTo(AssetId other)
        {
            int r = _high.CompareTo(other._high);
            return r == 0 ? _low.CompareTo(other._low) : r;
        }

        public ulong High => _high;
        public ulong Low => _low;

        public Guid Guid => Unsafe.BitCast<AssetId, Guid>(this);

        public bool IsInvalid => (_low | _high) == 0;

        public static readonly AssetId Invalid = IAssetIdProvider.Invalid;

        public static explicit operator AssetId(Guid guid) => new AssetId(guid);
        public static implicit operator Guid(AssetId id) => id.Guid;

        public static bool operator >(AssetId left, AssetId right) => left._high == right._high ? (left._low > right._low) : (left._high > right._high);
        public static bool operator >=(AssetId left, AssetId right) => left._high == right._high ? (left._low >= right._low) : (left._high >= right._high);
        public static bool operator <(AssetId left, AssetId right) => left._high == right._high ? (left._low <= right._low) : (left._high <= right._high);
        public static bool operator <=(AssetId left, AssetId right) => left._high == right._high ? (left._low < right._low) : (left._high < right._high);
    }
}
