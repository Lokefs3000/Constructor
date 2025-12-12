using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Rendering;
using Primary.Utility;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Rendering2.Assets.Loaders
{
    internal sealed class ShaderAsset2Loader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new ShaderAsset2Data(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not ShaderAsset2Data materialData)
                throw new ArgumentException(nameof(assetData));

            return new ShaderAsset2(materialData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not ShaderAsset2 shader)
                throw new ArgumentException(nameof(asset));
            if (assetData is not ShaderAsset2Data shaderData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom);
                if (stream == null)
                    ThrowException("Failed to open asset stream");

                SBCHeader header = stream.Read<SBCHeader>();

                if (header.Header != SBCHeader.ConstHeader)
                    ThrowException("Invalid header present in file: {header} ({utf8})", header.Header, Encoding.UTF8.GetString(MemoryMarshal.Cast<uint, byte>(new ReadOnlySpan<uint>(in header.Header))));
                if (header.Version != SBCHeader.ConstVersion)
                    ThrowException("Invalid version present in file: {version}", header.Version);

                //HACK: Implement dynamic switching
                if (!FlagUtility.HasFlag(header.Targets, SBCTarget.Direct3D12))
                    ThrowException("Shader does not have a target for current api: {api}", SBCTarget.Direct3D12);

                using BinaryReader br = new BinaryReader(stream);

                SBCPrimitiveTopology topology = stream.Read<SBCPrimitiveTopology>();
                int expectedConstantsSize = stream.ReadByte();
                SBCRasterizer rasterizer = stream.Read<SBCRasterizer>();
                SBCDepthStencil depthStencil = stream.Read<SBCDepthStencil>();
                SBCBlend blend = stream.Read<SBCBlend>();

                int rtBlendsCount = stream.Read<byte>();
                SBCRenderTargetBlend[] rtBlends = stream.ReadArray<SBCRenderTargetBlend>(rtBlendsCount);

                List<ShaderProperty> properties = new List<ShaderProperty>();
                ShaderResource[] resources = Array.Empty<ShaderResource>();

                Dictionary<int, string> customNameDict = new Dictionary<int, string>();

                //resources
                {
                    ushort resourceCount = stream.Read<ushort>();
                    if (resourceCount > 0)
                    {
                        resources = new ShaderResource[resourceCount];

                        for (int i = 0; i < resourceCount; i++)
                        {
                            SBCResourceType type = stream.Read<SBCResourceType>();
                            SBCResourceFlags flags = stream.Read<SBCResourceFlags>();

                            ShResourceType resourceType = ShResourceType.Unknown;
                            switch (type)
                            {
                                case SBCResourceType.Texture1D: resourceType = ShResourceType.Texture1D; break;
                                case SBCResourceType.Texture2D: resourceType = ShResourceType.Texture2D; break;
                                case SBCResourceType.Texture3D: resourceType = ShResourceType.Texture3D; break;
                                case SBCResourceType.TextureCube: resourceType = ShResourceType.TextureCube; break;
                                case SBCResourceType.ConstantBuffer: resourceType = ShResourceType.ConstantBuffer; break;
                                case SBCResourceType.StructuredBuffer: resourceType = ShResourceType.StructuredBuffer; break;
                                case SBCResourceType.ByteAddressBuffer: resourceType = ShResourceType.ByteAddressBuffer; break;
                                case SBCResourceType.SamplerState: resourceType = ShResourceType.SamplerState; break;
                                default: ThrowException("Unexpected resource type value: {t} ({v})", type, (int)type); break;
                            }

                            string name = br.ReadString();

                            resources[i] = new ShaderResource(name, resourceType, flags == 0 ? ShResourceFlags.None : ShResourceFlags.Property);

                            //Must be a property as there are only property-based attributes that can be assigned to a resource
                            if (flags > 0)
                            {
                                ShPropertyFlags propFlags = ShPropertyFlags.Property;
                                ShPropertyDisplay propDisplay = ShPropertyDisplay.Default;
                                ShPropertyDefault propDefault = ShPropertyDefault.TexWhite;

                                string? customName = null;
                                object? auxilary = null;

                                if (FlagUtility.HasFlag(flags, SBCResourceFlags.Constants))
                                    propFlags |= ShPropertyFlags.Constants;
                                if (FlagUtility.HasFlag(flags, SBCResourceFlags.Display))
                                {
                                    SBCPropertyDisplay read = stream.Read<SBCPropertyDisplay>();
                                    switch (read)
                                    {
                                        case SBCPropertyDisplay.Default: propDisplay = ShPropertyDisplay.Default; break;
                                        case SBCPropertyDisplay.Color: propDisplay = ShPropertyDisplay.Color; break;
                                        default: ThrowException("Unexpected resource property display value: {t} ({v})", read, (int)read); break;
                                    }
                                }
                                if (FlagUtility.HasFlag(flags, SBCResourceFlags.Global))
                                {
                                    propFlags |= ShPropertyFlags.Global;

                                    SBCAttributeGlobal attribute = stream.Read<SBCAttributeGlobal>();
                                    if (attribute.HasCustomName)
                                        customName = br.ReadString();
                                }
                                if (FlagUtility.HasFlag(flags, SBCResourceFlags.Property))
                                {
                                    SBCAttributeProperty attribute = stream.Read<SBCAttributeProperty>();
                                    propDefault = attribute.Default switch
                                    {
                                        SBCPropertyDefault.NumOne => ShPropertyDefault.NumOne,
                                        SBCPropertyDefault.NumZero => ShPropertyDefault.NumZero,
                                        SBCPropertyDefault.NumIdentity => ShPropertyDefault.NumIdentity,
                                        SBCPropertyDefault.TexWhite => ShPropertyDefault.TexWhite,
                                        SBCPropertyDefault.TexBlack => ShPropertyDefault.TexBlack,
                                        SBCPropertyDefault.TexNormal => ShPropertyDefault.TexNormal,
                                        SBCPropertyDefault.TexMask => ShPropertyDefault.TexMask,
                                        _ => throw new NotImplementedException()
                                    };

                                    if (attribute.HasCustomName)
                                        customName = br.ReadString();
                                }
                                if (FlagUtility.HasFlag(flags, SBCResourceFlags.Sampled))
                                {
                                    propFlags |= ShPropertyFlags.Sampled;

                                    SBCAttributeSampled attribute = stream.Read<SBCAttributeSampled>();
                                    if (attribute.HasCustomName)
                                        auxilary = br.ReadString();
                                    else
                                        auxilary = name + "_Sampler";
                                }

                                properties.Add(new ShaderProperty(name, (ushort)i, ushort.MaxValue, type switch
                                {
                                    SBCResourceType.Texture1D => ShPropertyType.Texture,
                                    SBCResourceType.Texture2D => ShPropertyType.Texture,
                                    SBCResourceType.Texture3D => ShPropertyType.Texture,
                                    SBCResourceType.TextureCube => ShPropertyType.Texture,
                                    SBCResourceType.ConstantBuffer => ShPropertyType.Buffer,
                                    SBCResourceType.StructuredBuffer => ShPropertyType.Buffer,
                                    SBCResourceType.ByteAddressBuffer => ShPropertyType.Buffer,
                                    _ => throw new InvalidOperationException()
                                }, propDefault, ShPropertyStages.GenericShader | ShPropertyStages.PixelShader/*HACK: Actually determine during compile time*/, propFlags, propDisplay));

                                if (customName != null)
                                    customNameDict[i] = customName;
                            }
                        }
                    }
                }


                //properties
                {
                    ushort propertyCount = stream.Read<ushort>();
                    if (propertyCount > 0)
                    {
                        properties.EnsureCapacity(propertyCount);

                        for (int i = 0; i < propertyCount; i++)
                        {
                            ushort size = stream.Read<ushort>();
                            ShPropertyType type = ShPropertyType.Buffer;

                            if (((size >> 15) & 0x1) > 0) //is struct
                            {
                                type = ShPropertyType.Struct;
                                size = (ushort)(size & ~(1 << 15));
                            }
                            else
                            {
                                SBCValueGeneric generic = (SBCValueGeneric)(size & 0xf);
                                int rows = (size >> 12) & 0xf;
                                int columns = (size >> 9) & 0xf;

                                switch (generic)
                                {
                                    case SBCValueGeneric.Single:
                                        size = (ushort)(sizeof(float) * rows * columns);
                                        if (columns == 4)
                                            type = ShPropertyType.Matrix4x4;
                                        else
                                            type = rows switch
                                            {
                                                1 => type = ShPropertyType.Single,
                                                2 => type = ShPropertyType.Vector2,
                                                3 => type = ShPropertyType.Vector3,
                                                4 => type = ShPropertyType.Vector4,
                                                _ => throw new NotImplementedException(),
                                            };

                                        break;
                                    case SBCValueGeneric.Double:
                                        size = (ushort)(sizeof(double) * rows * columns);
                                        type = ShPropertyType.Double;
                                        break;
                                    case SBCValueGeneric.Int:
                                        size = (ushort)(sizeof(int) * rows * columns);
                                        type = ShPropertyType.Int32;
                                        break;
                                    case SBCValueGeneric.UInt:
                                        size = (ushort)(sizeof(uint) * rows * columns);
                                        type = ShPropertyType.UInt32;
                                        break;
                                }
                            }

                            Debug.Assert(type > ShPropertyType.Texture);

                            ushort byteOffset = stream.Read<ushort>();
                            SBCPropertyFlags flags = stream.Read<SBCPropertyFlags>();
                            string name = br.ReadString();

                            ShPropertyFlags propFlags = ShPropertyFlags.None;
                            ShPropertyDisplay propDisplay = ShPropertyDisplay.Default;
                            ShPropertyDefault propDefault = ShPropertyDefault.NumIdentity;

                            string? customName = null;

                            if (FlagUtility.HasFlag(flags, SBCPropertyFlags.HasParent))
                                propFlags |= ShPropertyFlags.HasParent;

                            if (FlagUtility.HasFlag(flags, SBCPropertyFlags.Display))
                            {
                                propDisplay = stream.Read<SBCPropertyDisplay>() switch
                                {
                                    SBCPropertyDisplay.Default => ShPropertyDisplay.Default,
                                    SBCPropertyDisplay.Color => ShPropertyDisplay.Color,
                                    _ => throw new NotImplementedException(),
                                };
                            }
                            if (FlagUtility.HasFlag(flags, SBCPropertyFlags.Global))
                            {
                                flags |= SBCPropertyFlags.Global;

                                SBCAttributeGlobal attribute = stream.Read<SBCAttributeGlobal>();
                                if (attribute.HasCustomName)
                                    customName = br.ReadString();
                            }
                            if (FlagUtility.HasFlag(flags, SBCPropertyFlags.Property))
                            {
                                flags |= SBCPropertyFlags.Property;

                                SBCAttributeProperty attribute = stream.Read<SBCAttributeProperty>();
                                propDefault = attribute.Default switch
                                {
                                    SBCPropertyDefault.NumOne => ShPropertyDefault.NumOne,
                                    SBCPropertyDefault.NumZero => ShPropertyDefault.NumZero,
                                    SBCPropertyDefault.NumIdentity => ShPropertyDefault.NumIdentity,
                                    SBCPropertyDefault.TexWhite => ShPropertyDefault.TexWhite,
                                    SBCPropertyDefault.TexBlack => ShPropertyDefault.TexBlack,
                                    SBCPropertyDefault.TexNormal => ShPropertyDefault.TexNormal,
                                    SBCPropertyDefault.TexMask => ShPropertyDefault.TexMask,
                                    _ => throw new NotImplementedException()
                                };

                                if (attribute.HasCustomName)
                                    customName = br.ReadString();
                            }

                            properties.Add(new ShaderProperty(name, byteOffset, size, type, propDefault, ShPropertyStages.None, propFlags, propDisplay));

                            if (customName != null)
                                customNameDict[(1 << 15) | i] = customName;
                        }
                    }
                }

                ShaderProperty[] outputProperties = Array.Empty<ShaderProperty>();
                int propertyBlockSize = 0;
                int headerBlockSize = 0;
                Dictionary<int, int> remapTable = new Dictionary<int, int>();
                
                //sort dummy
                {
                    using RentedArray<PropertySortDummy> dummies = RentedArray<PropertySortDummy>.Rent(properties.Count + resources.Length);
                    int j = 0;

                    int actualUnique = 0;

                    {
                        Span<ShaderProperty> span = properties.AsSpan();
                        for (int i = 0; i < properties.Count; i++)
                        {
                            actualUnique++;

                            ref ShaderProperty property = ref span[i];
                            if (FlagUtility.HasFlag(property.Flags, ShPropertyFlags.HasParent))
                                continue;

                            PsSortDummyType type = PsSortDummyType.Resource;
                            if (property.Type == ShPropertyType.Struct)
                                type = PsSortDummyType.Struct;
                            else if (property.Type > ShPropertyType.Texture)
                                type = PsSortDummyType.Property;

                            PsSortDummyUsage usage = PsSortDummyUsage.Property;
                            if (FlagUtility.HasFlag(property.Flags, ShPropertyFlags.Global))
                                usage = PsSortDummyUsage.Global;
                            else if (FlagUtility.HasFlag(property.Flags, ShPropertyFlags.Constants))
                                usage = PsSortDummyUsage.Constants;

                            dummies[j++] = new PropertySortDummy(i, property.Name, type, usage);
                        }
                    }

                    {
                        Span<ShaderResource> span = resources.AsSpan();
                        for (int i = 0; i < span.Length; i++)
                        {
                            ref ShaderResource resource = ref span[i];
                            if (!FlagUtility.HasFlag(resource.Flags, ShResourceFlags.Property))
                            {
                                dummies[j++] = new PropertySortDummy(i | (1 << 31), resource.Name, PsSortDummyType.Resource, PsSortDummyUsage.Resource);
                                actualUnique++;
                            }
                        }
                    }

                    Span<PropertySortDummy> dummiesSpan = dummies.Span.Slice(0, j);
                    dummiesSpan.Sort();

                    Span<ShaderProperty> oldPropertiesSpan = properties.AsSpan();

                    int localByteOffset = 0;
                    int globalByteOffset = 0;

                    Queue<(string, int)> propertyQueue = new Queue<(string, int)>();

                    outputProperties = new ShaderProperty[actualUnique];

                    int outputIdx = 0;
                    int propIndex = 0;

                    for (int i = 0; i < dummiesSpan.Length; i++)
                    {
                        ref PropertySortDummy dummy = ref dummiesSpan[i];
                        int sourceIndex = dummy.SourceIndex & ~(1 << 31);

                        /*
                         * FIXME: stupid "CS8172" with the simpler "FlagUtility.HasFlag(property.Flags, ShPropertyFlags.Global) ? ref globalByteOffset : ref localByteOffset;" syntax
                         * And an if statement causes a "CS8374" >:(
                         * So a shitty solution is what i have come up with
                         * Though now that i think about it since global and local resources are seperate i can use the same variable with some simple guesswork
                         */

                        bool isRawResource = ((dummy.SourceIndex >> 31) & 0x1) > 0;

                        if (isRawResource)
                        {
                            ref ShaderResource resource = ref resources[sourceIndex];

                            headerBlockSize += sizeof(uint);

                            outputProperties[outputIdx++] = new ShaderProperty(resource.Name, (ushort)propIndex++, sizeof(uint), resource.Type switch
                            {
                                ShResourceType.Texture1D => ShPropertyType.Texture,
                                ShResourceType.Texture2D => ShPropertyType.Texture,
                                ShResourceType.Texture3D => ShPropertyType.Texture,
                                ShResourceType.TextureCube => ShPropertyType.Texture,
                                ShResourceType.ConstantBuffer => ShPropertyType.Buffer,
                                ShResourceType.StructuredBuffer => ShPropertyType.Buffer,
                                ShResourceType.ByteAddressBuffer => ShPropertyType.Buffer,
                                ShResourceType.SamplerState => ShPropertyType.Sampler,
                                _ => throw new NotImplementedException(),
                            }, ShPropertyDefault.TexWhite, ShPropertyStages.GenericShader | ShPropertyStages.PixelShader, ShPropertyFlags.None, ShPropertyDisplay.Default);
                            localByteOffset += sizeof(uint);

                            customNameDict.TryGetValue(sourceIndex, out string? customName);
                            remapTable.Add((customName ?? resource.Name).GetDjb2HashCode(), outputIdx - 1);
                        }
                        else
                        {
                            ref ShaderProperty property = ref oldPropertiesSpan[sourceIndex];

                            WeakRef<int> byteOffsetPtr = FlagUtility.HasFlag(property.Flags, ShPropertyFlags.Global) ? new WeakRef<int>(ref globalByteOffset) : new WeakRef<int>(ref localByteOffset);
                            ref int byteOffset = ref byteOffsetPtr.Ref;

                            if (property.Type <= ShPropertyType.Texture)
                            {
                                headerBlockSize += sizeof(uint);

                                outputProperties[outputIdx++] = new ShaderProperty(property.Name, (ushort)propIndex++, sizeof(uint), property.Type, property.Default, property.Stages, property.Flags, property.Display);
                                byteOffset += sizeof(uint);

                                customNameDict.TryGetValue(sourceIndex, out string? customName);
                                remapTable.Add((customName ?? property.Name).GetDjb2HashCode(), outputIdx - 1);
                            }
                            else if (property.Type == ShPropertyType.Struct)
                            {
                                propertyQueue.Clear();
                                propertyQueue.Enqueue((property.Name, int.MaxValue));

                                headerBlockSize += property.ByteWidth;
                                if (!FlagUtility.HasFlag(property.Flags, ShPropertyFlags.Global))
                                    propertyBlockSize += property.ByteWidth;

                                outputProperties[outputIdx++] = new ShaderProperty(property.Name, (ushort)byteOffset, property.ByteWidth, property.Type, property.Default, property.Stages, property.Flags | ShPropertyFlags.Property, property.Display);

                                customNameDict.TryGetValue(sourceIndex | (1 << 15), out string? customName);
                                remapTable.Add((customName ?? property.Name).GetDjb2HashCode(), outputIdx - 1);

                                int expectedByteOffset = byteOffset + property.ByteWidth;
                                j = sourceIndex + 1;

                                while (expectedByteOffset > byteOffset)
                                {
                                    (string @namespace, int expectedOffset) = propertyQueue.Peek();

                                    ref ShaderProperty subProperty = ref oldPropertiesSpan[j++];
                                    if (subProperty.Type == ShPropertyType.Struct)
                                    {
                                        string path = $"{@namespace}.{subProperty.Name}";
                                        propertyQueue.Enqueue((path, byteOffset + subProperty.ByteWidth));
                                        outputProperties[outputIdx++] = new ShaderProperty(path, (ushort)byteOffset, subProperty.ByteWidth, subProperty.Type, subProperty.Default, subProperty.Stages, subProperty.Flags, subProperty.Display);

                                        customNameDict.TryGetValue(sourceIndex | (1 << 15), out customName);
                                        remapTable.Add((customName ?? path).GetDjb2HashCode(), outputIdx - 1);
                                    }
                                    else
                                    {
                                        string path = $"{@namespace}.{subProperty.Name}";
                                        outputProperties[outputIdx++] = new ShaderProperty(path, (ushort)byteOffset, subProperty.ByteWidth, subProperty.Type, subProperty.Default, subProperty.Stages, subProperty.Flags, subProperty.Display);

                                        customNameDict.TryGetValue(sourceIndex | (1 << 15), out customName);
                                        remapTable.Add((customName ?? path).GetDjb2HashCode(), outputIdx - 1);

                                        byteOffset += subProperty.ByteWidth;
                                        if (expectedOffset >= byteOffset)
                                        {
                                            propertyQueue.Dequeue();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                RHI.InputElementDescription[] inputElements = Array.Empty<RHI.InputElementDescription>();

                //input layout
                {
                    byte count = (byte)stream.ReadByte();
                    if (count > 0)
                    {
                        inputElements = new RHI.InputElementDescription[count];
                        for (int i = 0; i < count; i++)
                        {
                            SBCInputElement inputElement = stream.Read<SBCInputElement>();
                            inputElements[i] = new RHI.InputElementDescription
                            {
                                Semantic = inputElement.Semantic switch
                                {
                                    SBCInputSemantic.Position => RHI.InputElementSemantic.Position,
                                    SBCInputSemantic.Texcoord => RHI.InputElementSemantic.Texcoord,
                                    SBCInputSemantic.Color => RHI.InputElementSemantic.Color,
                                    SBCInputSemantic.Normal => RHI.InputElementSemantic.Normal,
                                    SBCInputSemantic.Tangent => RHI.InputElementSemantic.Tangent,
                                    SBCInputSemantic.Bitangnet => RHI.InputElementSemantic.Bitangent,
                                    _ => throw new NotImplementedException(),
                                } + inputElement.SemanticIndex,
                                Format = inputElement.Format switch
                                {
                                    SBCInputFormat.Float1 => RHI.InputElementFormat.Float1,
                                    SBCInputFormat.Float2 => RHI.InputElementFormat.Float2,
                                    SBCInputFormat.Float3 => RHI.InputElementFormat.Float3,
                                    SBCInputFormat.Float4 => RHI.InputElementFormat.Float4,
                                    SBCInputFormat.UInt1 => RHI.InputElementFormat.UInt1,
                                    SBCInputFormat.UInt2 => RHI.InputElementFormat.UInt2,
                                    SBCInputFormat.UInt3 => RHI.InputElementFormat.UInt3,
                                    SBCInputFormat.UInt4 => RHI.InputElementFormat.UInt4,
                                    SBCInputFormat.Byte4 => RHI.InputElementFormat.Byte4,
                                    _ => throw new NotImplementedException(),
                                },
                                InputSlot = inputElement.InputSlot,
                                ByteOffset = inputElement.ByteOffset == ushort.MaxValue ? -1 : inputElement.ByteOffset,
                                InputSlotClass = inputElement.InputSlotClass switch
                                {
                                    SBCInputClassification.Vertex => RHI.InputClassification.Vertex,
                                    SBCInputClassification.Instance => RHI.InputClassification.Instance,
                                    _ => throw new NotImplementedException(),
                                },
                                InstanceDataStepRate = 0
                            };
                        }
                    }
                }

                KeyValuePair<uint, RHI.ImmutableSamplerDescription>[] staticSamplers = Array.Empty<KeyValuePair<uint, RHI.ImmutableSamplerDescription>>();

                //static samplers
                {
                    byte count = (byte)stream.ReadByte();
                    if (count > 0)
                    {
                        staticSamplers = new KeyValuePair<uint, RHI.ImmutableSamplerDescription>[count];
                        for (int i = 0; i < count; i++)
                        {
                            SBCStaticSampler staticSampler = stream.Read<SBCStaticSampler>();
                            staticSamplers[i] = new KeyValuePair<uint, RHI.ImmutableSamplerDescription>((uint)i, new RHI.ImmutableSamplerDescription
                            {
                                Filter = staticSampler.Filter switch
                                {
                                    SBCSamplerFilter.Point => RHI.TextureFilter.Point,
                                    SBCSamplerFilter.MinMagPointMipLinear => RHI.TextureFilter.MinMagPointMipLinear,
                                    SBCSamplerFilter.MinPointMagLinearMipPoint => RHI.TextureFilter.MinPointMagLinearMipPoint,
                                    SBCSamplerFilter.MinPointMagMipLinear => RHI.TextureFilter.MinPointMagMipLinear,
                                    SBCSamplerFilter.MinLinearMagMipPoint => RHI.TextureFilter.MinLinearMagMipPoint,
                                    SBCSamplerFilter.MinLinearMagPointMipLinear => RHI.TextureFilter.MinLinearMagPointMipLinear,
                                    SBCSamplerFilter.MinMagLinearMipPoint => RHI.TextureFilter.MinMagLinearMipPoint,
                                    SBCSamplerFilter.Linear => RHI.TextureFilter.Linear,
                                    SBCSamplerFilter.MinMagAnisotropicMipPoint => RHI.TextureFilter.MinMagAnisotropicMipPoint,
                                    _ => throw new NotImplementedException(),
                                },
                                AddressModeU = TranslateSAM(staticSampler.AddressModeU),
                                AddressModeV = TranslateSAM(staticSampler.AddressModeV),
                                AddressModeW = TranslateSAM(staticSampler.AddressModeW),
                                MaxAnistropy = staticSampler.MaxAnisotropy,
                                MipLODBias = staticSampler.MipLODBias,
                                MinLOD = staticSampler.MinLOD,
                                MaxLOD = staticSampler.MaxLOD,
                                Border = staticSampler.Border switch
                                {
                                    SBCSamplerBorder.TransparentBlack => RHI.SamplerBorder.TransparentBlack,
                                    SBCSamplerBorder.OpaqueBlack => RHI.SamplerBorder.OpaqueBlack,
                                    SBCSamplerBorder.OpaqueWhite => RHI.SamplerBorder.OpaqueWhite,
                                    SBCSamplerBorder.OpaqueBlackUInt => RHI.SamplerBorder.OpaqueBlackUInt,
                                    SBCSamplerBorder.OpaqueWhiteUInt => RHI.SamplerBorder.OpaqueWhiteUInt,
                                    _ => throw new NotImplementedException(),
                                }
                            });
                        }
                    }

                    static RHI.TextureAddressMode TranslateSAM(SBCSamplerAddressMode addressMode) => addressMode switch
                    {
                        SBCSamplerAddressMode.Repeat => RHI.TextureAddressMode.Repeat,
                        SBCSamplerAddressMode.Mirror => RHI.TextureAddressMode.Mirror,
                        SBCSamplerAddressMode.ClampToEdge => RHI.TextureAddressMode.ClampToEdge,
                        SBCSamplerAddressMode.ClampToBorder => RHI.TextureAddressMode.ClampToBorder,
                        _ => throw new NotImplementedException(),
                    };
                }

                RHI.GraphicsPipelineBytecode pipelineBytecode = new RHI.GraphicsPipelineBytecode();

                //bytecode
                {
                    int totalStages = int.PopCount((int)header.Stages);
                    int totalBytecodeCount = int.PopCount((int)header.Targets) * int.PopCount(totalStages);

                    //TODO: dynamic switching.. again
                    int baseOffset = int.PopCount(totalStages * int.TrailingZeroCount((int)SBCTarget.Direct3D12));
                    Debug.Assert(baseOffset + totalStages > totalBytecodeCount);

                    long startOffset = stream.Position;

                    ReadAndCheckBytecode(SBCStages.Vertex, (x) => pipelineBytecode.Vertex = x);
                    ReadAndCheckBytecode(SBCStages.Pixel, (x) => pipelineBytecode.Pixel = x);

                    void ReadAndCheckBytecode(SBCStages stage, Action<byte[]> callback)
                    {
                        if (FlagUtility.HasFlag(header.Stages, stage))
                        {
                            stream.Seek(startOffset + (baseOffset + int.TrailingZeroCount((int)stage) * 2) * sizeof(int), SeekOrigin.Begin);

                            int offset = stream.Read<int>();
                            int length = stream.Read<int>();

                            stream.Seek(offset, SeekOrigin.Begin);

                            byte[] array = new byte[length];
                            stream.ReadExactly(array);

                            callback(array);
                        }
                    }
                }

                RHI.GraphicsPipeline? graphicsPipeline = null;

                //rhi
                {
                    int propertiesValCount = 0;
                    for (int i = 0; i < outputProperties.Length; i++)
                    {
                        ref ShaderProperty property = ref outputProperties[i];
                        if (FlagUtility.HasEither(property.Flags, ShPropertyFlags.Property | ShPropertyFlags.Global) && !FlagUtility.HasFlag(property.Flags, ShPropertyFlags.HasParent))
                        {
                            propertiesValCount += property.ByteWidth;
                        }
                    }

                    propertiesValCount /= sizeof(uint);
                    bool uniqueCb = (propertiesValCount + expectedConstantsSize / sizeof(uint)) > 32;

                    RHI.GraphicsPipelineDescription pipelineDescription = new RHI.GraphicsPipelineDescription
                    {
                        FillMode = rasterizer.FillMode switch
                        {
                            SBCFillMode.Solid => RHI.FillMode.Solid,
                            SBCFillMode.Wireframe => RHI.FillMode.Wireframe,
                            _ => throw new NotImplementedException(),
                        },
                        CullMode = rasterizer.CullMode switch
                        {
                            SBCCullMode.None => RHI.CullMode.None,
                            SBCCullMode.Back => RHI.CullMode.Back,
                            SBCCullMode.Front => RHI.CullMode.Front,
                            _ => throw new NotImplementedException(),
                        },
                        FrontCounterClockwise = rasterizer.FrontCounterClockwise,
                        DepthBias = rasterizer.DepthBias,
                        DepthBiasClamp = rasterizer.DepthBiasClamp,
                        SlopeScaledDepthBias = rasterizer.SlopeScaledDepthBias,
                        DepthClipEnable = rasterizer.DepthClipEnable,
                        ConservativeRaster = rasterizer.ConservativeRaster,
                    
                        DepthEnable = depthStencil.DepthEnable,
                        DepthWriteMask = (RHI.DepthWriteMask)depthStencil.WriteMask,
                        DepthFunc = ConvertComparisonFunc(depthStencil.DepthFunc),
                        StencilEnable = depthStencil.StencilEnable,
                        StencilReadMask = depthStencil.StencilReadMask,
                        StencilWriteMask = depthStencil.StencilWriteMask,
                    
                        FrontFace = ConvertFace(depthStencil.FrontFace),
                        BackFace = ConvertFace(depthStencil.BackFace),
                    
                        LogicOpEnable = false,
                        LogicOp = RHI.LogicOp.NoOp,
                    
                        PrimitiveTopology = topology switch
                        {
                            SBCPrimitiveTopology.Triangle => RHI.PrimitiveTopologyType.Triangle,
                            SBCPrimitiveTopology.Line => RHI.PrimitiveTopologyType.Line,
                            SBCPrimitiveTopology.Point => RHI.PrimitiveTopologyType.Point,
                            SBCPrimitiveTopology.Patch => RHI.PrimitiveTopologyType.Patch,
                            _ => throw new NotImplementedException(),
                        },
                    
                        AlphaToCoverageEnable = blend.AlphaToCoverageEnable,
                        IndependentBlendEnable = blend.IndependentBlendEnable,
                    
                        Blends = rtBlends.Length == 0 ? Array.Empty<RHI.BlendDescription>() : ConvertRTBlends(rtBlends),

                        InputElements = inputElements,
                        BoundResources = Array.Empty<RHI.BoundResourceDescription>(),
                        ImmutableSamplers = staticSamplers,

                        ExpectedConstantsSize = (uint)expectedConstantsSize,

                        Num32BitValues = uniqueCb ? expectedConstantsSize / sizeof(uint) : propertiesValCount + expectedConstantsSize / sizeof(uint),
                        HasConstantBuffer = uniqueCb,
                    };

                    RHI.GraphicsDevice device = RHI.GraphicsDevice.Instance!;
                    graphicsPipeline = device.CreateGraphicsPipeline(pipelineDescription, pipelineBytecode);

                    static RHI.StencilFace ConvertFace(SBCDepthStencilFace face) => new RHI.StencilFace
                    {
                        StencilFailOp = ConvertStencilOp(face.Fail),
                        StencilDepthFailOp = ConvertStencilOp(face.DepthFail),
                        StencilPassOp = ConvertStencilOp(face.Pass),
                        StencilFunc = ConvertComparisonFunc(face.Func),
                    };
                    static RHI.BlendDescription[] ConvertRTBlends(SBCRenderTargetBlend[] rtBlends)
                    {
                        RHI.BlendDescription[] array = new RHI.BlendDescription[rtBlends.Length];
                        for (int i = 0; i < array.Length; i++)
                        {
                            ref SBCRenderTargetBlend data = ref rtBlends[i];
                            array[i] = new RHI.BlendDescription
                            {
                                BlendEnable = data.BlendEnable,
                                SrcBlend = ConvertBlendSource(data.Source),
                                DstBlend = ConvertBlendSource(data.Destination),
                                BlendOp = ConvertBlendOp(data.Operation),
                                SrcBlendAlpha = ConvertBlendSource(data.SourceAlpha),
                                DstBlendAlpha = ConvertBlendSource(data.DestinationAlpha),
                                BlendOpAlpha = ConvertBlendOp(data.OperationAlpha),
                                RenderTargetWriteMask = data.WriteMask
                            };
                        }

                        return array;
                    }

                    static RHI.ComparisonFunc ConvertComparisonFunc(SBCComparisonFunc func) => func switch
                    {
                        SBCComparisonFunc.None => RHI.ComparisonFunc.None,
                        SBCComparisonFunc.Never => RHI.ComparisonFunc.Never,
                        SBCComparisonFunc.Less => RHI.ComparisonFunc.Less,
                        SBCComparisonFunc.Equal => RHI.ComparisonFunc.Equal,
                        SBCComparisonFunc.LessEqual => RHI.ComparisonFunc.LessEqual,
                        SBCComparisonFunc.Greater => RHI.ComparisonFunc.Greater,
                        SBCComparisonFunc.NotEqual => RHI.ComparisonFunc.NotEqual,
                        SBCComparisonFunc.GreaterEqual => RHI.ComparisonFunc.GreaterEqual,
                        SBCComparisonFunc.Always => RHI.ComparisonFunc.Always,
                        _ => throw new NotImplementedException(),
                    };
                    static RHI.StencilOp ConvertStencilOp(SBCStencilOp op) => op switch
                    {
                        SBCStencilOp.Keep => RHI.StencilOp.Keep,
                        SBCStencilOp.Zero => RHI.StencilOp.Zero,
                        SBCStencilOp.Replace => RHI.StencilOp.Replace,
                        SBCStencilOp.IncrementSaturation => RHI.StencilOp.IncrementSaturation,
                        SBCStencilOp.DecrementSaturation => RHI.StencilOp.DecrementSaturation,
                        SBCStencilOp.Invert => RHI.StencilOp.Invert,
                        SBCStencilOp.Increment => RHI.StencilOp.Increment,
                        SBCStencilOp.Decrement => RHI.StencilOp.Decrement,
                        _ => throw new NotImplementedException(),
                    };
                    static RHI.Blend ConvertBlendSource(SBCBlendSource blendSource) => blendSource switch
                    {
                        SBCBlendSource.Zero => RHI.Blend.Zero,
                        SBCBlendSource.One => RHI.Blend.One,
                        SBCBlendSource.SourceColor => RHI.Blend.SourceColor,
                        SBCBlendSource.InverseSourceColor => RHI.Blend.InverseSourceColor,
                        SBCBlendSource.SourceAlpha => RHI.Blend.SourceAlpha,
                        SBCBlendSource.InverseSourceAlpha => RHI.Blend.InverseSourceAlpha,
                        SBCBlendSource.DestinationAlpha => RHI.Blend.DestinationAlpha,
                        SBCBlendSource.InverseDestinationAlpha => RHI.Blend.InverseDestinationAlpha,
                        SBCBlendSource.DestinationColor => RHI.Blend.DestinationColor,
                        SBCBlendSource.InverseDestinationColor => RHI.Blend.InverseDestinationColor,
                        SBCBlendSource.SourceAlphaSaturate => RHI.Blend.SourceAlphaSaturate,
                        SBCBlendSource.BlendFactor => RHI.Blend.BlendFactor,
                        SBCBlendSource.InverseBlendFactor => RHI.Blend.InverseBlendFactor,
                        SBCBlendSource.Source1Color => RHI.Blend.Source1Color,
                        SBCBlendSource.InverseSource1Color => RHI.Blend.InverseSource1Color,
                        SBCBlendSource.Source1Alpha => RHI.Blend.Source1Alpha,
                        SBCBlendSource.InverseSource1Alpha => RHI.Blend.InverseSource1Alpha,
                        SBCBlendSource.AlphaFactor => RHI.Blend.AlphaFactor,
                        SBCBlendSource.InverseAlphaFactor => RHI.Blend.InverseAlphaFactor,
                        _ => throw new NotImplementedException(),
                    };
                    static RHI.BlendOp ConvertBlendOp(SBCBlendOp op) => op switch
                    {
                        SBCBlendOp.Add => RHI.BlendOp.Add,
                        SBCBlendOp.Subtract => RHI.BlendOp.Subtract,
                        SBCBlendOp.ReverseSubtract => RHI.BlendOp.ReverseSubtract,
                        SBCBlendOp.Minimum => RHI.BlendOp.Minimum,
                        SBCBlendOp.Maximum => RHI.BlendOp.Maximum,
                        _ => throw new NotImplementedException(),
                    };
                }

                shaderData.UpdateAssetData(shader, outputProperties, remapTable.ToFrozenDictionary(), propertyBlockSize, headerBlockSize, graphicsPipeline);
                return;

                [DoesNotReturn]
                void ThrowException(string message, params object?[] args)
                {
                    shaderData.UpdateAssetFailed(shader);

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
                EngLog.Assets.Error(ex, "Failed to load shader: {name}", sourcePath);
            }
#endif
        }

        private readonly record struct PropertySortDummy(int SourceIndex, string Name, PsSortDummyType Type, PsSortDummyUsage Usage) : IComparable<PropertySortDummy>
        {
            public int CompareTo(PropertySortDummy other)
            {
                int x = 0;

                if ((x = Usage.CompareTo(other.Usage)) != 0) return x;
                if ((x = Type.CompareTo(other.Type)) != 0) return x;
                return Name.CompareTo(other.Name, StringComparison.Ordinal);
            }
        }

        private enum PsSortDummyType : byte
        {
            Property = 0,
            Struct,
            Resource
        }

        private enum PsSortDummyUsage : byte
        {
            Global = 0,
            Constants,
            Property,
            Resource
        }
    }

    public struct SBCHeader
    {
        public uint Header;
        public uint Version;

        public SBCTarget Targets;
        public SBCStages Stages;

        public const uint ConstHeader = 0x20434253;
        public const uint ConstVersion = 1;
    }

    public struct SBCRasterizer
    {
        public SBCFillMode FillMode;
        public SBCCullMode CullMode;
        public bool FrontCounterClockwise;
        public int DepthBias;
        public float DepthBiasClamp;
        public float SlopeScaledDepthBias;
        public bool DepthClipEnable;
        public bool ConservativeRaster;
    }

    public struct SBCDepthStencil
    {
        public bool DepthEnable;
        public SBCDepthWriteMask WriteMask;
        public SBCComparisonFunc DepthFunc;
        public bool StencilEnable;
        public byte StencilReadMask;
        public byte StencilWriteMask;
        public SBCDepthStencilFace FrontFace;
        public SBCDepthStencilFace BackFace;
    }

    public struct SBCDepthStencilFace
    {
        public SBCStencilOp Fail;
        public SBCStencilOp DepthFail;
        public SBCStencilOp Pass;
        public SBCComparisonFunc Func;
    }

    public struct SBCBlend
    {
        public bool AlphaToCoverageEnable;
        public bool IndependentBlendEnable;
    }

    public struct SBCRenderTargetBlend
    {
        public bool BlendEnable;
        public SBCBlendSource Source;
        public SBCBlendSource Destination;
        public SBCBlendOp Operation;
        public SBCBlendSource SourceAlpha;
        public SBCBlendSource DestinationAlpha;
        public SBCBlendOp OperationAlpha;
        public byte WriteMask;
    }

    public struct SBCAttributeGlobal
    {
        public bool HasCustomName;
    }

    public struct SBCAttributeProperty
    {
        public SBCPropertyDefault Default;
        public bool HasCustomName;
    }

    public struct SBCAttributeSampled
    {
        public bool HasCustomName;
    }

    public struct SBCInputElement
    {
        public SBCInputSemantic Semantic;
        public byte SemanticIndex;

        public SBCInputFormat Format;

        public byte InputSlot;
        public ushort ByteOffset;

        public SBCInputClassification InputSlotClass;
    }

    public struct SBCStaticSampler
    {
        public SBCSamplerFilter Filter;
        public SBCSamplerAddressMode AddressModeU;
        public SBCSamplerAddressMode AddressModeV;
        public SBCSamplerAddressMode AddressModeW;
        public byte MaxAnisotropy;
        public float MipLODBias;
        public float MinLOD;
        public float MaxLOD;
        public SBCSamplerBorder Border;
    }

    public enum SBCTarget : byte
    {
        None = 0,

        Direct3D12 = 1 << 0,
        Vulkan = 1 << 1,
    }

    public enum SBCStages : byte
    {
        None = 0,

        Vertex = 1 << 0,
        Pixel = 1 << 1,
    }

    public enum SBCPrimitiveTopology : byte
    {
        Triangle = 0,
        Line,
        Point,
        Patch
    }

    public enum SBCFillMode : byte
    {
        Solid = 0,
        Wireframe
    }

    public enum SBCCullMode : byte
    {
        None = 0,
        Back,
        Front,
    }

    public enum SBCDepthWriteMask
    {
        None = 0,
        All
    }

    public enum SBCComparisonFunc : byte
    {
        None = 0,
        Never,
        Less,
        Equal,
        LessEqual,
        Greater,
        NotEqual,
        GreaterEqual,
        Always
    }

    public enum SBCStencilOp : byte
    {
        Keep = 0,
        Zero,
        Replace,
        IncrementSaturation,
        DecrementSaturation,
        Invert,
        Increment,
        Decrement
    }

    public enum SBCBlendSource : byte
    {
        Zero = 0,
        One,
        SourceColor,
        InverseSourceColor,
        SourceAlpha,
        InverseSourceAlpha,
        DestinationAlpha,
        InverseDestinationAlpha,
        DestinationColor,
        InverseDestinationColor,
        SourceAlphaSaturate,
        BlendFactor,
        InverseBlendFactor,
        Source1Color,
        InverseSource1Color,
        Source1Alpha,
        InverseSource1Alpha,
        AlphaFactor,
        InverseAlphaFactor
    }

    public enum SBCBlendOp : byte
    {
        Add = 0,
        Subtract,
        ReverseSubtract,
        Minimum,
        Maximum,
    }

    public enum SBCResourceType : byte
    {
        Texture1D,
        Texture2D,
        Texture3D,
        TextureCube,
        ConstantBuffer,
        StructuredBuffer,
        ByteAddressBuffer,
        SamplerState
    }

    public enum SBCResourceFlags : byte
    {
        None = 0,

        Constants = 1 << 0,
        Display = 1 << 1,
        Global = 1 << 2,
        Property = 1 << 3,
        Sampled = 1 << 4,
    }

    public enum SBCPropertyDisplay : byte
    {
        Default = 0,
        Color
    }

    public enum SBCPropertyDefault : byte
    {
        NumOne = 0,
        NumZero,
        NumIdentity,

        TexWhite,
        TexBlack,
        TexMask,
        TexNormal
    }

    public enum SBCPropertyFlags : byte
    {
        None = 0,

        Display = 1 << 0,
        Global = 1 << 1,
        Property = 1 << 2,
        HasParent = 1 << 3
    }

    public enum SBCValueGeneric : byte
    {
        Single,
        Double,
        Int,
        UInt
    }

    public enum SBCInputSemantic : byte
    {
        Position = 0,
        Texcoord = 8,
        Color = 16,
        Normal = 24,
        Tangent = 32,
        Bitangnet = 48,
        BlendIndices = 64,
        BlendWeight = 72,
        PositionT = 80,
        PSize = 88,
        Fog = 96,
        TessFactor = 104
    }

    public enum SBCInputFormat : byte
    {
        Float1 = 0,
        Float2,
        Float3,
        Float4,

        UInt1,
        UInt2,
        UInt3,
        UInt4,

        Byte4
    }

    public enum SBCInputClassification : byte
    {
        Vertex = 0,
        Instance
    }

    public enum SBCSamplerFilter : byte
    {
        Point = 0,
        MinMagPointMipLinear,
        MinPointMagLinearMipPoint,
        MinPointMagMipLinear,
        MinLinearMagMipPoint,
        MinLinearMagPointMipLinear,
        MinMagLinearMipPoint,
        Linear,
        MinMagAnisotropicMipPoint,
    }

    public enum SBCSamplerAddressMode : byte
    {
        Repeat = 0,
        Mirror,
        ClampToEdge,
        ClampToBorder
    }

    public enum SBCSamplerBorder : byte
    {
        TransparentBlack = 0,
        OpaqueBlack,
        OpaqueWhite,
        OpaqueBlackUInt,
        OpaqueWhiteUInt
    }
}
