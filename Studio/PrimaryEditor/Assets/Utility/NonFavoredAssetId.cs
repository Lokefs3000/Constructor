using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets.Types;
using Tomlyn.Serialization;

namespace PrimaryEditor.Assets.Utility
{
    // Tomlyn annoyingly favours attribute converters over the ones in the options so i have to just a proxy type
    public readonly record struct NonFavoredAssetId(AssetId Id) : IEquatable<NonFavoredAssetId>, IEquatable<AssetId>
    {
        public bool Equals(AssetId other) => Id.Equals(other);

        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => Id.ToString();

        public static implicit operator AssetId(NonFavoredAssetId id) => id.Id;
        public static implicit operator NonFavoredAssetId(AssetId id) => new NonFavoredAssetId(id);

        public sealed class EarlyConverter : TomlConverter<NonFavoredAssetId>
        {
            private readonly EarlyAssetIdTomlConverter _converter;

            public EarlyConverter()
            {
                _converter = new EarlyAssetIdTomlConverter();
            }

            public override NonFavoredAssetId Read(TomlReader reader) => _converter.Read(reader);
            public override void Write(TomlWriter writer, NonFavoredAssetId value) => _converter.Write(writer, value.Id);

            public static readonly EarlyConverter Instance = new EarlyConverter();
        }
    }
}
