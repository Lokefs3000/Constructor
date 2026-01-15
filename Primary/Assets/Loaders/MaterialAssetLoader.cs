using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Rendering.Assets;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Tomlyn;
using Tomlyn.Model;

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

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not MaterialAsset material)
                throw new ArgumentException(nameof(asset));
            if (assetData is not MaterialAssetData materialData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                string? source = AssetFilesystem.ReadString(sourcePath, bundleToReadFrom);
                if (source == null)
                {
                    materialData.UpdateAssetFailed(material);
                    return;
                }

                TomlTable table = Toml.ToModel<TomlTable>(source, sourcePath);

                if (!table.TryGetValue("shader", out object shaderId) || !(shaderId is string or long))
                {
                    materialData.UpdateAssetFailed(material);
                    return;
                }

                ShaderAsset shader = shaderId is string ?
                    AssetManager.LoadAsset<ShaderAsset>((string)shaderId, true) :
                    AssetManager.LoadAsset<ShaderAsset>(new AssetId((ulong)(long)shaderId), true);

                if (shader.Status != ResourceStatus.Success)
                {
                    materialData.UpdateAssetFailed(material);
                    return;
                }

                PropertyBlock? block = shader.CreatePropertyBlock();
                if (block == null)
                    ThrowException("Failed to create property block from shader");

                bool hasActualProperties = false;
                foreach (ref readonly ShaderProperty property in shader.Properties)
                {
                    if (FlagUtility.HasFlag(property.Flags, ShPropertyFlags.Property) && !FlagUtility.HasEither(property.Flags, ShPropertyFlags.Global | ShPropertyFlags.HasParent))
                    {
                        hasActualProperties = true;
                        break;
                    }
                }

                if (hasActualProperties)
                {
                    TomlTable propertiesTable = (TomlTable)table["properties"];

                    foreach (ref readonly ShaderProperty property in shader.Properties)
                    {
                        if (!FlagUtility.HasFlag(property.Flags, ShPropertyFlags.Property) || FlagUtility.HasEither(property.Flags, ShPropertyFlags.Global | ShPropertyFlags.HasParent))
                            continue;

                        switch (property.Type)
                        {
                            case ShPropertyType.Texture:
                                {
                                    if (!table.TryGetValue(property.Name, out object assetId) || !(assetId is string or long))
                                    {
                                        EngLog.Assets.Error("[a:{path}]: Failed to find property: {prop}", sourcePath, property.Name);

                                        block.SetResource(property.Name.GetDjb2HashCode(), property.Default switch
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
                                        block.SetResource(property.Name.GetDjb2HashCode(), assetId is string ?
                                            AssetManager.LoadAsset<TextureAsset>((string)assetId) :
                                            AssetManager.LoadAsset<TextureAsset>(new AssetId((ulong)(long)assetId)));
                                    }

                                    break;
                                }
                            case ShPropertyType.Single:
                                {
                                    if (!table.TryGetValue(property.Name, out object assetId) || !(assetId is double))
                                    {
                                        EngLog.Assets.Error("[a:{path}]: Failed to find property: {prop}", sourcePath, property.Name);

                                        block.SetSingle(property.Name.GetDjb2HashCode(), property.Default switch
                                        {
                                            ShPropertyDefault.NumOne => 1.0f,
                                            ShPropertyDefault.NumZero => 0.0f,
                                            ShPropertyDefault.NumIdentity => 0.0f,
                                            _ => throw new NotImplementedException(),
                                        });
                                    }
                                    else
                                    {
                                        block.SetSingle(property.Name.GetDjb2HashCode(), (float)(double)assetId);
                                    }

                                    break;
                                }
                            case ShPropertyType.Double:
                                {
                                    if (!table.TryGetValue(property.Name, out object assetId) || !(assetId is double))
                                    {
                                        EngLog.Assets.Error("[a:{path}]: Failed to find property: {prop}", sourcePath, property.Name);

                                        block.SetDouble(property.Name.GetDjb2HashCode(), property.Default switch
                                        {
                                            ShPropertyDefault.NumOne => 1.0,
                                            ShPropertyDefault.NumZero => 0.0,
                                            ShPropertyDefault.NumIdentity => 0.0,
                                            _ => throw new NotImplementedException(),
                                        });
                                    }
                                    else
                                    {
                                        block.SetDouble(property.Name.GetDjb2HashCode(), (double)assetId);
                                    }

                                    break;
                                }
                            case ShPropertyType.UInt32:
                                {
                                    if (!table.TryGetValue(property.Name, out object assetId) || !(assetId is long))
                                    {
                                        EngLog.Assets.Error("[a:{path}]: Failed to find property: {prop}", sourcePath, property.Name);

                                        block.SetUInt(property.Name.GetDjb2HashCode(), property.Default switch
                                        {
                                            ShPropertyDefault.NumOne => 1,
                                            ShPropertyDefault.NumZero => 0,
                                            ShPropertyDefault.NumIdentity => 0,
                                            _ => throw new NotImplementedException(),
                                        });
                                    }
                                    else
                                    {
                                        block.SetUInt(property.Name.GetDjb2HashCode(), (uint)(long)assetId);
                                    }

                                    break;
                                }
                            case ShPropertyType.Int32:
                                {
                                    if (!table.TryGetValue(property.Name, out object assetId) || !(assetId is long))
                                    {
                                        EngLog.Assets.Error("[a:{path}]: Failed to find property: {prop}", sourcePath, property.Name);

                                        block.SetInt(property.Name.GetDjb2HashCode(), property.Default switch
                                        {
                                            ShPropertyDefault.NumOne => 1,
                                            ShPropertyDefault.NumZero => 0,
                                            ShPropertyDefault.NumIdentity => 0,
                                            _ => throw new NotImplementedException(),
                                        });
                                    }
                                    else
                                    {
                                        block.SetInt(property.Name.GetDjb2HashCode(), (int)(long)assetId);
                                    }

                                    break;
                                }
                            case ShPropertyType.Vector2:
                                {
                                    if (!table.TryGetValue(property.Name, out object array) || !(array is TomlArray))
                                    {
                                        EngLog.Assets.Error("[a:{path}]: Failed to find property: {prop}", sourcePath, property.Name);

                                        block.SetVector2(property.Name.GetDjb2HashCode(), property.Default switch
                                        {
                                            ShPropertyDefault.NumOne => Vector2.One,
                                            ShPropertyDefault.NumZero => Vector2.Zero,
                                            ShPropertyDefault.NumIdentity => Vector2.Zero,
                                            _ => throw new NotImplementedException(),
                                        });
                                    }
                                    else
                                    {
                                        TomlArray castArray = (TomlArray)array;
                                        block.SetVector2(property.Name.GetDjb2HashCode(), new Vector2((float)(double)castArray[0]!, (float)(double)castArray[1]!));
                                    }

                                    break;
                                }
                            case ShPropertyType.Vector3:
                                {
                                    if (!table.TryGetValue(property.Name, out object array) || !(array is TomlArray))
                                    {
                                        EngLog.Assets.Error("[a:{path}]: Failed to find property: {prop}", sourcePath, property.Name);

                                        block.SetVector3(property.Name.GetDjb2HashCode(), property.Default switch
                                        {
                                            ShPropertyDefault.NumOne => Vector3.One,
                                            ShPropertyDefault.NumZero => Vector3.Zero,
                                            ShPropertyDefault.NumIdentity => Vector3.Zero,
                                            _ => throw new NotImplementedException(),
                                        });
                                    }
                                    else
                                    {
                                        TomlArray castArray = (TomlArray)array;
                                        block.SetVector3(property.Name.GetDjb2HashCode(), new Vector3((float)(double)castArray[0]!, (float)(double)castArray[1]!, (float)(double)castArray[2]!));
                                    }

                                    break;
                                }
                            case ShPropertyType.Vector4:
                                {
                                    if (!table.TryGetValue(property.Name, out object array) || !(array is TomlArray))
                                    {
                                        EngLog.Assets.Error("[a:{path}]: Failed to find property: {prop}", sourcePath, property.Name);

                                        block.SetVector4(property.Name.GetDjb2HashCode(), property.Default switch
                                        {
                                            ShPropertyDefault.NumOne => Vector4.One,
                                            ShPropertyDefault.NumZero => Vector4.Zero,
                                            ShPropertyDefault.NumIdentity => Vector4.Zero,
                                            _ => throw new NotImplementedException(),
                                        });
                                    }
                                    else
                                    {
                                        TomlArray castArray = (TomlArray)array;
                                        block.SetVector4(property.Name.GetDjb2HashCode(), new Vector4((float)(double)castArray[0]!, (float)(double)castArray[1]!, (float)(double)castArray[2]!, (float)(double)castArray[3]!));
                                    }

                                    break;
                                }
                            case ShPropertyType.Matrix4x4: //TODO: implement
                                throw new NotImplementedException("lazy :/");
                        }
                    }
                }

                materialData.UpdateAssetData(material, shader, block);

                [DoesNotReturn]
                void ThrowException(string message, params object?[] args)
                {
                    materialData.UpdateAssetFailed(material);

                    EngLog.Assets.Error("[a:{path}]: " + message, [sourcePath, .. args]);
                    throw new Exception("Unexpected error");
                }
            }
#if DEBUG
            finally
            {

            }
#else
            catch (Exception ex)
            {
                materialData.UpdateAssetFailed(material);
                EngLog.Assets.Error(ex, "Failed to load material: {name}", sourcePath);
            }
#endif
        }
    }
}
