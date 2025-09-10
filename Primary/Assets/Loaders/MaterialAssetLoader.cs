using CsToml;
using CsToml.Values;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Rendering;
using Serilog;
using System.Collections.Frozen;
using System.Diagnostics;

namespace Primary.Assets.Loaders
{
    internal static unsafe class MaterialAssetLoader
    {
        internal static IInternalAssetData FactoryCreateNull()
        {
            return new MaterialAssetData();
        }

        internal static IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not MaterialAssetData materialData)
                throw new ArgumentException(nameof(assetData));

            return new MaterialAsset(materialData);
        }

        internal static void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not MaterialAsset material)
                throw new ArgumentException(nameof(asset));
            if (assetData is not MaterialAssetData materialData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom);
                if (stream == null)
                {
                    materialData.UpdateAssetFailed(material);
                    return;
                }

                ExceptionUtility.Assert(stream != null);

                TomlDocument document = CsTomlSerializer.Deserialize<TomlDocument>(stream!);
                TomlDocumentNode root = document.RootNode;
            
                string shader = string.Empty;
                ExceptionUtility.Assert(root["shader"u8].TryGetString(out shader));

                ShaderAsset? shaderAsset = AssetManager.LoadAsset<ShaderAsset>(shader, true);
                if (shaderAsset == null)
                {
                    materialData.ChangeCurrentShader(shaderAsset, FrozenDictionary<string, MaterialProperty>.Empty, null);
                    materialData.UpdateAssetData(material);

                    return;
                }

                ShaderBindGroup defaultGroup = shaderAsset.CreateDefaultBindGroup();
                Dictionary<string, MaterialProperty> properties = new Dictionary<string, MaterialProperty>();

                foreach (ShaderVariable variable in shaderAsset.Variables.Values)
                {
                    if (variable.BindGroup == defaultGroup.GroupName)
                    {
                        int idx = Array.FindIndex(variable.Attributes, (x) => x.Type == ShaderAttributeType.Property);
                        if (idx >= 0)
                        {
                            ref ShaderVariableAttribute attribute = ref variable.Attributes[idx];
                            ShaderVariableAttribProperty property = (ShaderVariableAttribProperty)attribute.Value!;

                            properties.Add(property.Name, new MaterialProperty
                            {
                                Type = variable.Type switch
                                {
                                    ShaderVariableType.Texture1D => MaterialVariableType.Texture,
                                    ShaderVariableType.Texture2D => MaterialVariableType.Texture,
                                    ShaderVariableType.Texture3D => MaterialVariableType.Texture,
                                    ShaderVariableType.TextureCube => MaterialVariableType.Texture,
                                    _ => throw new NotSupportedException(variable.Type.ToString())
                                },
                                VariableName = variable.Name,
                            });

                            defaultGroup.SetResource(variable.Name, property.Default switch
                            {
                                ShaderPropertyDefault.White => AssetManager.Static.DefaultWhite,
                                ShaderPropertyDefault.Normal => AssetManager.Static.DefaultNormal,
                                _ => AssetManager.Static.DefaultWhite
                            });
                        }
                    }
                }

                TomlDocumentNode propertiesNode = root["properties"];
                if (propertiesNode.HasValue)
                {
                    foreach (var kvp in properties)
                    {
                        TomlDocumentNode child = propertiesNode[kvp.Key];
                        if (child.HasValue)
                        {
                            switch (kvp.Value.Type)
                            {
                                case MaterialVariableType.Texture:
                                    {
                                        if (!child.TryGetString(out string value))
                                        {
                                            //bad
                                            continue;
                                        }

                                        defaultGroup.SetResource(kvp.Value.VariableName, AssetManager.LoadAsset<TextureAsset>(value));
                                        break;
                                    }
                            }
                        }
                    }
                }

                materialData.ChangeCurrentShader(shaderAsset, properties.ToFrozenDictionary(), defaultGroup);
                materialData.UpdateAssetData(material);
            }
#if DEBUG
            finally
            {

            }
#else
            catch (Exception ex)
            {
                materialData.UpdateAssetFailed(material);
                Log.Error(ex, "Failed to load shader: {name}", sourcePath);
            }
#endif
        }
    }
}
