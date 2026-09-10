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
        public bool TryGetPathForId(FileId assetId, bool getLocalPath, [NotNullWhen(true)] out string? value);

        /// <summary>Thread-safe</summary>
        public bool TryGetAnyPathForId(FileId assetId, [NotNullWhen(true)] out string? value);

        /// <summary>Thread-safe</summary>
        public bool TryGetLocalAndAssetPathsForId(FileId assetId, [NotNullWhen(true)] out string? localPath, [MaybeNullWhen(true)] out string? assetPath);

        /// <summary>Thread-safe</summary>
        public bool TryLookupIdForPath(ReadOnlySpan<char> path, [NotNullWhen(true)] out FileId value);

        /// <summary>Thread-safe</summary>
        public bool IsIdValid(FileId assetId);

        /// <summary>Thread-safe</summary>
        public bool DoesPathHaveLookup(ReadOnlySpan<char> path);

        /// <inheritdoc cref="TryGetPathForId(FileId, bool, out string?)" />
        public bool TryGetLocalPathForId(FileId assetId, [NotNullWhen(true)] out string? value) => TryGetPathForId(assetId, true, out value);

        /// <inheritdoc cref="TryGetPathForId(FileId, bool, out string?)" />
        public bool TryGetAssetPathForId(FileId assetId, [NotNullWhen(true)] out string? value) => TryGetPathForId(assetId, false, out value);
    }

    [JsonConverter(typeof(AssetIdJsonConverter)), TomlConverter(typeof(AssetIdTomlConverter))]
    public readonly record struct AssetId : IEquatable<AssetId>, IComparable<AssetId>, IFormattable, IComparisonOperators<AssetId, AssetId, bool>, IEqualityOperators<AssetId, AssetId, bool>
    {
        private readonly FileId _fileId;
        private readonly int _localId;

        public AssetId() => this = Invalid;
        public AssetId(FileId fileId, int localId) { _fileId = fileId; _localId = localId; }

        public readonly AssetId WithoutLocalId() => new AssetId(_fileId, NoLocalId);
        public readonly AssetId WithLocalId(int localId) => new AssetId(_fileId, localId);

        /// <summary>Produces same result as <see cref="Guid.GetHashCode"/></summary>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(_fileId, _localId);
        }

        public readonly override string ToString() => ToString(null, CultureInfo.CurrentCulture);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format) => ToString(format, CultureInfo.InvariantCulture);
        public readonly string ToString(IFormatProvider? formatProvider) => ToString("N", formatProvider);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format, IFormatProvider? formatProvider) => $"{{ FileId={_fileId.ToString(format, formatProvider)} LocalId={(_localId == NoLocalId ? "<none>" : _localId.ToString(formatProvider))} }}";

        public readonly bool Equals(AssetId other)
        {
            return _fileId.Equals(other._fileId) && _localId == other._localId;
        }

        public readonly int CompareTo(AssetId other)
        {
            int r = _fileId.CompareTo(other._fileId);
            return r == 0 ? _localId.CompareTo(other._localId) : r;
        }

        public readonly FileId FileId => _fileId;
        public readonly int LocalId => _localId;

        public readonly bool IsInvalid => _fileId.IsInvalid;
        public readonly bool HasLocalId => _localId != NoLocalId;

        public static readonly AssetId Invalid = new AssetId(FileId.Invalid, NoLocalId);
        public const int NoLocalId = int.MinValue;

        public static bool operator >(AssetId left, AssetId right) => left._fileId == right._fileId ? (left._localId > right._localId) : (left > right);
        public static bool operator >=(AssetId left, AssetId right) => left._fileId == right._fileId ? (left._localId >= right._localId) : (left >= right);
        public static bool operator <(AssetId left, AssetId right) => left._fileId == right._fileId ? (left._localId < right._localId) : (left < right);
        public static bool operator <=(AssetId left, AssetId right) => left._fileId == right._fileId ? (left._localId <= right._localId) : (left <= right);

        public static implicit operator FileId(AssetId assetId) => assetId._fileId;
        public static implicit operator AssetId(FileId fileId) => new AssetId(fileId, NoLocalId);
    }

    [JsonConverter(typeof(FileIdJsonConverter)), TomlConverter(typeof(FileIdTomlConverter))]
    public readonly record struct FileId : IEquatable<FileId>, IComparable<FileId>, IFormattable, ISpanFormattable, IUtf8SpanFormattable, IComparisonOperators<FileId, FileId, bool>, IEqualityOperators<FileId, FileId, bool>
    {
        // assume little endian architecture
        private readonly ulong _high;
        private readonly ulong _low;

        public FileId() => this = Invalid;
        public FileId(ulong high, ulong low) { _high = high; _low = low; }
        public FileId(Guid guid) => this = Unsafe.BitCast<Guid, FileId>(guid);

        /// <summary>Produces same result as <see cref="Guid.GetHashCode"/></summary>
        public readonly override int GetHashCode()
        {
            ref int r = ref Unsafe.As<ulong, int>(ref Unsafe.AsRef(in _high));
            return r ^ Unsafe.Add(ref r, 1) ^ Unsafe.Add(ref r, 2) ^ Unsafe.Add(ref r, 3);
        }

        public readonly override string ToString() => ToString("N", CultureInfo.InvariantCulture);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format) => ToString(format, CultureInfo.InvariantCulture);
        public readonly string ToString(IFormatProvider? formatProvider) => ToString("N", formatProvider);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format, IFormatProvider? formatProvider) => Unsafe.BitCast<FileId, Guid>(this).ToString(format, formatProvider);

        public readonly bool TryFormat(Span<char> destination, out int charsWritten) => Unsafe.BitCast<FileId, Guid>(this).TryFormat(destination, out charsWritten, "N");
        public readonly bool TryFormat(Span<byte> utf8Destination, out int bytesWritten) => Unsafe.BitCast<FileId, Guid>(this).TryFormat(utf8Destination, out bytesWritten, "N");

        public readonly bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format) => Unsafe.BitCast<FileId, Guid>(this).TryFormat(destination, out charsWritten, format);
        public readonly bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format) => Unsafe.BitCast<FileId, Guid>(this).TryFormat(utf8Destination, out bytesWritten, format);

        readonly bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format, IFormatProvider? provider) => Unsafe.BitCast<FileId, Guid>(this).TryFormat(destination, out charsWritten, format);
        readonly bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.GuidFormat)] ReadOnlySpan<char> format, IFormatProvider? provider) => Unsafe.BitCast<FileId, Guid>(this).TryFormat(utf8Destination, out bytesWritten, format);

        public readonly bool Equals(FileId other)
        {
            if (Vector128.IsHardwareAccelerated)
                return Unsafe.BitCast<FileId, Vector128<byte>>(this) == Unsafe.BitCast<FileId, Vector128<byte>>(other);
            else
                return _high == other._high && _low == other._low;
        }

        public readonly int CompareTo(FileId other)
        {
            int r = _high.CompareTo(other._high);
            return r == 0 ? _low.CompareTo(other._low) : r;
        }

        public readonly ulong High => _high;
        public readonly ulong Low => _low;

        public readonly Guid Guid => Unsafe.BitCast<FileId, Guid>(this);

        public readonly bool IsInvalid => (_low | _high) == 0;

        public static readonly FileId Invalid = default;

        public static explicit operator FileId(Guid guid) => new FileId(guid);
        public static implicit operator Guid(FileId id) => id.Guid;

        public static bool operator >(FileId left, FileId right) => left._high == right._high ? (left._low > right._low) : (left._high > right._high);
        public static bool operator >=(FileId left, FileId right) => left._high == right._high ? (left._low >= right._low) : (left._high >= right._high);
        public static bool operator <(FileId left, FileId right) => left._high == right._high ? (left._low <= right._low) : (left._high <= right._high);
        public static bool operator <=(FileId left, FileId right) => left._high == right._high ? (left._low < right._low) : (left._high < right._high);
    }
}
