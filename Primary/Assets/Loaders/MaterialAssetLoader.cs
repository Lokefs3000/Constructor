using CsToml;
using CsToml.Values;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Rendering;

namespace Primary.Assets.Loaders
{
    internal unsafe class MaterialAssetLoader : IAssetLoader
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

                TomlDocumentNode docNode = root["shader"u8];
                if (docNode.ValueType != TomlValueType.Integer && docNode.ValueType != TomlValueType.String)
                {
                    materialData.UpdateAssetFailed(material);
                    return;
                }

                ShaderAsset shaderAsset = docNode.ValueType == TomlValueType.Integer ?
                    AssetManager.LoadAsset<ShaderAsset>((AssetId)(uint)docNode.GetInt64(), true) :
                    AssetManager.LoadAsset<ShaderAsset>(docNode.GetString(), true);
                if (shaderAsset.Status != ResourceStatus.Success)
                {
                    materialData.ChangeCurrentShader(null, null);
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
                                Default = property.Default
                            });

                            defaultGroup.SetResource(variable.Name, property.Default switch
                            {
                                ShaderPropertyDefault.White => AssetManager.Static.DefaultWhite,
                                ShaderPropertyDefault.Normal => AssetManager.Static.DefaultNormal,
                                ShaderPropertyDefault.Mask => AssetManager.Static.DefaultMask,
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
                                        TextureAsset? textureAsset = null;
                                        if (child.TryGetInt64(out long id))
                                        {
                                            textureAsset = AssetManager.LoadAsset<TextureAsset>(new AssetId((uint)id));
                                        }
                                        else if (!child.TryGetString(out string value))
                                        {
                                            textureAsset = AssetManager.LoadAsset<TextureAsset>(value);
                                        }

                                        if (textureAsset != null && textureAsset.Status != ResourceStatus.Error)
                                            defaultGroup.SetResource(kvp.Value.VariableName, textureAsset);
                                        break;
                                    }
                            }
                        }
                    }
                }

                materialData.ChangeCurrentShader(shaderAsset, defaultGroup);
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
                EngLog.Assets.Error(ex, "Failed to load shader: {name}", sourcePath);
            }
#endif
        }
    }
}
