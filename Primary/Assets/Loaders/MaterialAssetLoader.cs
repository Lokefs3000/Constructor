using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Rendering.Assets;
using Primary.Serialization.Toml;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;
using Tomlyn.Text;

namespace Primary.Assets.Loaders
{
    internal sealed class MaterialAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new MaterialAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not MaterialAssetData materialData)
                throw new ArgumentException(nameof(assetData));

            return new MaterialAsset(materialData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, string localPath, BundleReader? bundleToReadFrom)
        {
            if (asset is not MaterialAsset material)
                throw new ArgumentException(nameof(asset));
            if (assetData is not MaterialAssetData materialData)
                throw new ArgumentException(nameof(assetData));

            materialData.Dispose();

            string? source = AssetFilesystem.ReadString(sourcePath, bundleToReadFrom);
            if (source == null)
            {
                materialData.UpdateAssetFailed(material);
                return;
            }

            MaterialTomlOutput? toml = TomlSerializer.Deserialize<MaterialTomlOutput>(source, MaterialTomlSerializerContext.Default);
            if (toml == null)
            {
                materialData.UpdateAssetFailed(material);
                return;
            }

            ShaderAsset shader = AssetManager.LoadAsset<ShaderAsset>(toml.Shader);

            shader.WaitIfNotLoaded();
            if (shader.Status != ResourceStatus.Success)
            {
                materialData.UpdateAssetFailed(material);
                return;
            }

            PropertyBlock? block = shader.CreatePropertyBlock();
            if (block == null)
                ThrowException("Failed to create property block from shader");

            foreach (ref readonly ShaderProperty property in shader.Properties)
            {
                if (!Flags.HasFlag(property.Flags, ShPropertyFlags.Property) || Flags.HasEither(property.Flags, ShPropertyFlags.Global | ShPropertyFlags.HasParent))
                    continue;

                switch (property.Type)
                {
                    case ShPropertyType.Texture:
                        {
                            if (!toml.Resources.TryGetValue(property.DisplayName, out AssetId assetId))
                            {
                                // EngLog.Assets.Error("[a:{path}]: Failed to find property: {prop}", sourcePath, property.DisplayName);

                                block.SetResource(property.DisplayName, property.Default switch
                                {
                                    ShPropertyDefault.NumOne => AssetManager.Static.DefaultWhite,
                                    ShPropertyDefault.NumZero => AssetManager.Static.DefaultBlack,
                                    ShPropertyDefault.NumIdentity => AssetManager.Static.DefaultWhite,
                                    ShPropertyDefault.TexWhite => AssetManager.Static.DefaultWhite,
                                    ShPropertyDefault.TexBlack => AssetManager.Static.DefaultBlack,
                                    ShPropertyDefault.TexMask => AssetManager.Static.DefaultMask,
                                    ShPropertyDefault.TexNormal => AssetManager.Static.DefaultNormal,
                                    _ => throw new NotImplementedException(),
                                });
                            }
                            else
                            {
                                TextureAsset texture = AssetManager.LoadAsset<TextureAsset>(assetId);
                                block.SetResource(property.DisplayName, texture);
                            }

                            break;
                        }
                    case ShPropertyType.Single:
                        {
                            if (!toml.Properties.TryGetValue(property.DisplayName, out string? valueStr) ||
                                !float.TryParse(valueStr, CultureInfo.InvariantCulture, out float valueReal))
                            {
                                EngLog.Assets.Error("[a:{path}]: Failed to find or parse property: {prop}", sourcePath, property.DisplayName);

                                block.SetSingle(property.DisplayName, property.Default switch
                                {
                                    ShPropertyDefault.NumOne => 1.0f,
                                    ShPropertyDefault.NumZero => 0.0f,
                                    ShPropertyDefault.NumIdentity => 0.0f,
                                    _ => throw new NotImplementedException(),
                                });
                            }
                            else
                            {
                                block.SetSingle(property.DisplayName, valueReal);
                            }

                            break;
                        }
                    case ShPropertyType.Double:
                        {
                            if (!toml.Properties.TryGetValue(property.DisplayName, out string? valueStr) ||
                                !double.TryParse(valueStr, CultureInfo.InvariantCulture, out double valueReal))
                            {
                                EngLog.Assets.Error("[a:{path}]: Failed to find or parse property: {prop}", sourcePath, property.DisplayName);

                                block.SetDouble(property.DisplayName, property.Default switch
                                {
                                    ShPropertyDefault.NumOne => 1.0,
                                    ShPropertyDefault.NumZero => 0.0,
                                    ShPropertyDefault.NumIdentity => 0.0,
                                    _ => throw new NotImplementedException(),
                                });
                            }
                            else
                            {
                                block.SetDouble(property.DisplayName, valueReal);
                            }

                            break;
                        }
                    case ShPropertyType.UInt32:
                        {
                            if (!toml.Properties.TryGetValue(property.DisplayName, out string? valueStr) ||
                                !uint.TryParse(valueStr, CultureInfo.InvariantCulture, out uint valueReal))
                            {
                                EngLog.Assets.Error("[a:{path}]: Failed to find or parse property: {prop}", sourcePath, property.DisplayName);

                                block.SetUInt(property.DisplayName, property.Default switch
                                {
                                    ShPropertyDefault.NumOne => 1,
                                    ShPropertyDefault.NumZero => 0,
                                    ShPropertyDefault.NumIdentity => 0,
                                    _ => throw new NotImplementedException(),
                                });
                            }
                            else
                            {
                                block.SetUInt(property.DisplayName, valueReal);
                            }

                            break;
                        }
                    case ShPropertyType.Int32:
                        {
                            if (!toml.Properties.TryGetValue(property.DisplayName, out string? valueStr) ||
                                !int.TryParse(valueStr, CultureInfo.InvariantCulture, out int valueReal))
                            {
                                EngLog.Assets.Error("[a:{path}]: Failed to find or parse property: {prop}", sourcePath, property.DisplayName);

                                block.SetSingle(property.DisplayName, property.Default switch
                                {
                                    ShPropertyDefault.NumOne => 1,
                                    ShPropertyDefault.NumZero => 0,
                                    ShPropertyDefault.NumIdentity => 0,
                                    _ => throw new NotImplementedException(),
                                });
                            }
                            else
                            {
                                block.SetInt(property.DisplayName, valueReal);
                            }

                            break;
                        }
                    case ShPropertyType.Vector2:
                        {
                            if (!toml.Properties.TryGetValue(property.DisplayName, out string? valueStr) ||
                                !Vector2TryParse(valueStr, out Vector2 valueReal))
                            {
                                EngLog.Assets.Error("[a:{path}]: Failed to find or parse property: {prop}", sourcePath, property.DisplayName);

                                block.SetVector2(property.DisplayName, property.Default switch
                                {
                                    ShPropertyDefault.NumOne => Vector2.Zero,
                                    ShPropertyDefault.NumZero => Vector2.Zero,
                                    ShPropertyDefault.NumIdentity => Vector2.Zero,
                                    _ => throw new NotImplementedException(),
                                });
                            }
                            else
                            {
                                block.SetVector2(property.DisplayName, valueReal);
                            }

                            break;
                        }
                    case ShPropertyType.Vector3:
                        {
                            if (!toml.Properties.TryGetValue(property.DisplayName, out string? valueStr) ||
                                !Vector3TryParse(valueStr, out Vector3 valueReal))
                            {
                                EngLog.Assets.Error("[a:{path}]: Failed to find or parse property: {prop}", sourcePath, property.DisplayName);

                                block.SetVector3(property.DisplayName, property.Default switch
                                {
                                    ShPropertyDefault.NumOne => Vector3.One,
                                    ShPropertyDefault.NumZero => Vector3.Zero,
                                    ShPropertyDefault.NumIdentity => Vector3.Zero,
                                    _ => throw new NotImplementedException(),
                                });
                            }
                            else
                            {
                                block.SetVector3(property.DisplayName, valueReal);
                            }

                            break;
                        }
                    case ShPropertyType.Vector4:
                        {
                            if (!toml.Properties.TryGetValue(property.DisplayName, out string? valueStr) ||
                                !Vector4TryParse(valueStr, out Vector4 valueReal))
                            {
                                EngLog.Assets.Error("[a:{path}]: Failed to find or parse property: {prop}", sourcePath, property.DisplayName);

                                block.SetVector4(property.DisplayName, property.Default switch
                                {
                                    ShPropertyDefault.NumOne => Vector4.One,
                                    ShPropertyDefault.NumZero => Vector4.Zero,
                                    ShPropertyDefault.NumIdentity => Vector4.Zero,
                                    _ => throw new NotImplementedException(),
                                });
                            }
                            else
                            {
                                block.SetVector4(property.DisplayName, valueReal);
                            }

                            break;
                        }
                    case ShPropertyType.Matrix4x4: //TODO: implement
                        throw new NotImplementedException("lazy :/");
                }
            }

            materialData.UpdateAssetData(material, shader, block);

            [DoesNotReturn]
            void ThrowException(string message, params object?[] args)
            {
                materialData.UpdateAssetFailed(material);

                EngLog.Assets.Error("[a:{path}]: " + message, [localPath, .. args]);
                throw new Exception("Unexpected error");
            }

            static bool Vector2TryParse(string serialized, out Vector2 value)
            {
                Unsafe.SkipInit(out value);
                Span<float> values = MemoryMarshal.CreateSpan(ref Unsafe.As<Vector2, float>(ref value), 2);
                return TryParseGenericSpan(serialized, ref values);
            }

            static bool Vector3TryParse(string serialized, out Vector3 value)
            {
                Unsafe.SkipInit(out value);
                Span<float> values = MemoryMarshal.CreateSpan(ref Unsafe.As<Vector3, float>(ref value), 2);
                return TryParseGenericSpan(serialized, ref values);
            }

            static bool Vector4TryParse(string serialized, out Vector4 value)
            {
                Unsafe.SkipInit(out value);
                Span<float> values = MemoryMarshal.CreateSpan(ref Unsafe.As<Vector4, float>(ref value), 2);
                return TryParseGenericSpan(serialized, ref values);
            }

            static bool TryParseGenericSpan(string serialized, ref Span<float> values)
            {
                TomlReader reader = TomlReader.Create(serialized);

                reader.Read();
                if (reader.TokenType != TomlTokenType.StartArray)
                    return false;

                for (int i = 0; i < values.Length; ++i)
                {
                    reader.Read();
                    if (reader.TokenType != TomlTokenType.Float)
                        return false;

                    TomlSourceSpan sourceSpan = reader.CurrentSpan!.Value;

                    if (!float.TryParse(serialized.AsSpan(sourceSpan.Start.Offset, sourceSpan.Length), CultureInfo.InvariantCulture, out float value))
                        return false;

                    values[i] = value;
                }

                reader.Read();
                if (reader.TokenType != TomlTokenType.EndArray)
                    return false;

                return true;
            }
        }
    }

    public sealed class MaterialTomlOutput
    {
        [TomlRequired]
        public AssetId Shader { get; set; } = AssetId.Invalid;

        public Dictionary<string, AssetId> Resources { get; set; } = [];
        public Dictionary<string, string> Properties { get; set; } = [];
    }

    [TomlSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, Converters = [
        typeof(AssetIdTomlConverter)
        ])]
    [TomlSerializable(typeof(MaterialTomlOutput))]
    public partial class MaterialTomlSerializerContext : TomlSerializerContext
    {
    }
}
