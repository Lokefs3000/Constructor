using Primary.Assets;
using Primary.Mathematics;
using Primary.Serialization.Json;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.Gui.View
{
    internal sealed class ToolbarItemSerializable
    {
        [JsonRequired] public string Id { get; set; } = string.Empty;
        [JsonRequired] public AppearanceSerializable[] Appearances { get; set; } = [];
        [JsonRequired] public LogicBaseSerializable? Logic { get; set; } = null;
    }

    internal sealed class AppearanceSerializable
    {
        public string? Key { get; set; } = null;

        public string? Text { get; set; } = null;
        public TextureAsset? Texture { get; set; } = null;
        public AtlasData Atlas { get; set; } = default;

        [JsonConverter(typeof(AppearanceAtlasDataConverter))]
        internal readonly record struct AtlasData(Int2 Origin, Int2 Size);
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "Key")]
    [JsonDerivedType(typeof(EnumLogicSerializable), "Enum")]
    [JsonDerivedType(typeof(DropdownLogicSerializable), "Dropdown")]
    internal abstract class LogicBaseSerializable
    {
    }

    internal sealed class EnumLogicSerializable : LogicBaseSerializable
    {
        [JsonRequired] public string Type { get; set; } = string.Empty;
    }

    internal sealed class DropdownLogicSerializable : LogicBaseSerializable
    {
        [JsonRequired] public string Path { get; set; } = string.Empty;

        public bool IsToggleable { get; set; } = false;
    }

    [JsonSourceGenerationOptions()]
    [JsonSerializable(typeof(ToolbarItemSerializable[]))]
    [JsonSerializable(typeof(EnumLogicSerializable))]
    [JsonSerializable(typeof(DropdownLogicSerializable))]
    internal partial class ToolbarItemJsonContext : JsonSerializerContext
    {
    }

    internal sealed class AppearanceAtlasDataConverter : JsonConverter<AppearanceSerializable.AtlasData>
    {
        public override AppearanceSerializable.AtlasData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();

            AppearanceSerializable.AtlasData data = new AppearanceSerializable.AtlasData();
            Span<int> elements = MemoryMarshal.CreateSpan(ref Unsafe.As<AppearanceSerializable.AtlasData, int>(ref data), 4);

            for (int i = 0; i < elements.Length; i++)
            {
                reader.Read();
                elements[i] = reader.GetInt32();
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();

            return data;
        }

        public override void Write(Utf8JsonWriter writer, AppearanceSerializable.AtlasData value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Origin.X);
            writer.WriteNumberValue(value.Origin.Y);
            writer.WriteNumberValue(value.Size.X);
            writer.WriteNumberValue(value.Size.Y);
            writer.WriteEndArray();
        }
    }
}
