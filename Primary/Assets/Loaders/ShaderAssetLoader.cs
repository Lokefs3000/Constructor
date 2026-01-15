using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.RHI2;
using Primary.Utility;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Assets.Loaders
{
    internal sealed class ShaderAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new ShaderAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not ShaderAssetData materialData)
                throw new ArgumentException(nameof(assetData));

            return new ShaderAsset(materialData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not ShaderAsset shader)
                throw new ArgumentException(nameof(asset));
            if (assetData is not ShaderAssetData shaderData)
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

                ShHeaderFlags headerFlags = ShHeaderFlags.None;

                if (FlagUtility.HasFlag(header.Flags, SBCHeaderFlags.ExternalProperties))
                    headerFlags |= ShHeaderFlags.ExternalProperties;
                if (FlagUtility.HasFlag(header.Flags, SBCHeaderFlags.HeaderIsBuffer))
                    headerFlags |= ShHeaderFlags.HeaderIsBuffer;

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
                Dictionary<int, string> samplerDict = new Dictionary<int, string>();

                //resources
                {
                    ushort resourceCount = stream.Read<ushort>();
                    if (resourceCount > 0)
                    {
                        resources = new ShaderResource[resourceCount];

                        for (int i = 0; i < resourceCount; i++)
                        {
                            SBCResourceType type = stream.Read<SBCResourceType>();
                            SBCStages stages = stream.Read<SBCStages>();
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

                            ShPropertyStages resourceStages = ShPropertyStages.None;
                            if (FlagUtility.HasFlag(stages, SBCStages.Vertex))
                                resourceStages = ShPropertyStages.VertexShading;
                            if (FlagUtility.HasFlag(stages, SBCStages.Pixel))
                                resourceStages = resourceStages == ShPropertyStages.VertexShading ? ShPropertyStages.AllShading : ShPropertyStages.PixelShading;

                            ShResourceFlags resourceFlags = ShResourceFlags.None;
                            if (FlagUtility.HasFlag(flags, SBCResourceFlags.IsReadWrite))
                                resourceFlags |= ShResourceFlags.ReadWrite;
                            if ((flags & ~SBCResourceFlags.IsReadWrite) > 0)
                                resourceFlags |= ShResourceFlags.Property;

                            string name = br.ReadString();

                            resources[i] = new ShaderResource(name, resourceType, resourceStages, resourceFlags);

                            //Must be a property as there are only property-based attributes that can be assigned to a resource
                            if (FlagUtility.HasFlag(resourceFlags, ShResourceFlags.Property))
                            {
                                ShPropertyFlags propFlags = ShPropertyFlags.Property;
                                ShPropertyDisplay propDisplay = ShPropertyDisplay.Default;
                                ShPropertyDefault propDefault = ShPropertyDefault.TexWhite;

                                string? customName = null;
                                object? auxilary = null;

                                if (FlagUtility.HasFlag(flags, SBCResourceFlags.IsReadWrite))
                                    propFlags |= ShPropertyFlags.ReadWrite;
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

                                properties.Add(new ShaderProperty(name, (ushort)i, ushort.MaxValue, ushort.MaxValue, type switch
                                {
                                    SBCResourceType.Texture1D => ShPropertyType.Texture,
                                    SBCResourceType.Texture2D => ShPropertyType.Texture,
                                    SBCResourceType.Texture3D => ShPropertyType.Texture,
                                    SBCResourceType.TextureCube => ShPropertyType.Texture,
                                    SBCResourceType.ConstantBuffer => ShPropertyType.Buffer,
                                    SBCResourceType.StructuredBuffer => ShPropertyType.Buffer,
                                    SBCResourceType.ByteAddressBuffer => ShPropertyType.Buffer,
                                    _ => throw new InvalidOperationException()
                                }, propDefault, resourceStages, propFlags, propDisplay));

                                if (customName != null)
                                    customNameDict[i] = customName;
                                if (auxilary is string and not null)
                                    samplerDict[i] = (string)auxilary;
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

                            properties.Add(new ShaderProperty(name, byteOffset, size, ushort.MaxValue, type, propDefault, ShPropertyStages.AllShading, propFlags, propDisplay));

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
                            ref ShaderProperty property = ref span[i];
                            if (FlagUtility.HasEither(property.Flags, ShPropertyFlags.HasParent | ShPropertyFlags.Constants))
                                continue;

                            actualUnique++;

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

                    if (!FlagUtility.HasFlag(header.Flags, SBCHeaderFlags.ExternalProperties))
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

                        bool isRawResource = ((dummy.SourceIndex >> 31) & 0x1) > 0;

                        if (isRawResource)
                        {
                            ref ShaderResource resource = ref resources[sourceIndex];

                            headerBlockSize += sizeof(uint);

                            outputProperties[outputIdx++] = new ShaderProperty(resource.Name, (ushort)propIndex++, sizeof(uint), ushort.MaxValue, resource.Type switch
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
                            }, ShPropertyDefault.TexWhite, ShPropertyStages.AllShading, ShPropertyFlags.None, ShPropertyDisplay.Default);
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

                                ushort childIndex = ushort.MaxValue;
                                if (samplerDict.TryGetValue(sourceIndex, out string? samplerName))
                                {
                                    int find = dummiesSpan.FindIndex((x) => x.Name == samplerName);
                                    if (find == -1)
                                        EngLog.Assets.Warning("Failed to find sampler ({n}) associated with texture: ({t})", samplerName, property.Name);
                                    else
                                        childIndex = (ushort)find;
                                }

                                outputProperties[outputIdx++] = new ShaderProperty(property.Name, (ushort)propIndex++, sizeof(uint), childIndex, property.Type, property.Default, property.Stages, property.Flags, property.Display);
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

                                outputProperties[outputIdx++] = new ShaderProperty(property.Name, (ushort)byteOffset, property.ByteWidth, ushort.MaxValue, property.Type, property.Default, property.Stages, property.Flags | ShPropertyFlags.Property, property.Display);

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
                                        outputProperties[outputIdx++] = new ShaderProperty(path, (ushort)byteOffset, subProperty.ByteWidth, ushort.MaxValue, subProperty.Type, subProperty.Default, subProperty.Stages, subProperty.Flags, subProperty.Display);

                                        customNameDict.TryGetValue(sourceIndex | (1 << 15), out customName);
                                        remapTable.Add((customName ?? path).GetDjb2HashCode(), outputIdx - 1);
                                    }
                                    else
                                    {
                                        string path = $"{@namespace}.{subProperty.Name}";
                                        outputProperties[outputIdx++] = new ShaderProperty(path, (ushort)byteOffset, subProperty.ByteWidth, ushort.MaxValue, subProperty.Type, subProperty.Default, subProperty.Stages, subProperty.Flags, subProperty.Display);

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

                RHIGPInputElement[] inputElements = Array.Empty<RHIGPInputElement>();

                //input layout
                {
                    byte count = (byte)stream.ReadByte();
                    if (count > 0)
                    {
                        inputElements = new RHIGPInputElement[count];
                        for (int i = 0; i < count; i++)
                        {
                            SBCInputElement inputElement = stream.Read<SBCInputElement>();
                            inputElements[i] = new RHIGPInputElement
                            {
                                Semantic = inputElement.Semantic switch
                                {
                                    SBCInputSemantic.Position => RHIElementSemantic.Position,
                                    SBCInputSemantic.Texcoord => RHIElementSemantic.Texcoord,
                                    SBCInputSemantic.Color => RHIElementSemantic.Color,
                                    SBCInputSemantic.Normal => RHIElementSemantic.Normal,
                                    SBCInputSemantic.Tangent => RHIElementSemantic.Tangent,
                                    SBCInputSemantic.BlendIndices => RHIElementSemantic.BlendIndices,
                                    SBCInputSemantic.BlendWeight => RHIElementSemantic.BlendWeight,
                                    SBCInputSemantic.PositionT => RHIElementSemantic.PositionT,
                                    SBCInputSemantic.PSize => RHIElementSemantic.PSize,
                                    SBCInputSemantic.Fog => RHIElementSemantic.Fog,
                                    SBCInputSemantic.TessFactor => RHIElementSemantic.TessFactor,
                                    _ => throw new NotImplementedException(),
                                },
                                SemanticIndex = inputElement.SemanticIndex,
                                Format = inputElement.Format switch
                                {
                                    SBCInputFormat.Float1 => RHIElementFormat.Single1,
                                    SBCInputFormat.Float2 => RHIElementFormat.Single2,
                                    SBCInputFormat.Float3 => RHIElementFormat.Single3,
                                    SBCInputFormat.Float4 => RHIElementFormat.Single4,
                                    SBCInputFormat.UInt1 => RHIElementFormat.UInt1,
                                    SBCInputFormat.UInt2 => RHIElementFormat.UInt2,
                                    SBCInputFormat.UInt3 => RHIElementFormat.UInt3,
                                    SBCInputFormat.UInt4 => RHIElementFormat.UInt4,
                                    SBCInputFormat.Byte4 => RHIElementFormat.Byte4,
                                    _ => throw new NotImplementedException(),
                                },
                                InputSlot = inputElement.InputSlot,
                                ByteOffset = inputElement.ByteOffset == ushort.MaxValue ? -1 : inputElement.ByteOffset,
                                InputSlotClass = inputElement.InputSlotClass switch
                                {
                                    SBCInputClassification.Vertex => RHIInputClass.PerVertex,
                                    SBCInputClassification.Instance => RHIInputClass.PerInstance,
                                    _ => throw new NotImplementedException(),
                                },
                                InstanceDataStepRate = 0
                            };
                        }
                    }
                }

                RHIGPImmutableSampler[] staticSamplers = Array.Empty<RHIGPImmutableSampler>();

                //static samplers
                {
                    byte count = (byte)stream.ReadByte();
                    if (count > 0)
                    {
                        staticSamplers = new RHIGPImmutableSampler[count];
                        for (int i = 0; i < count; i++)
                        {
                            SBCStaticSampler staticSampler = stream.Read<SBCStaticSampler>();
                            staticSamplers[i] = new RHIGPImmutableSampler
                            {
                                Min = TranslateFilter(staticSampler.Min),
                                Mag = TranslateFilter(staticSampler.Mag),
                                Mip = TranslateFilter(staticSampler.Mip),
                                ReductionType = staticSampler.Reduction switch
                                {
                                    SBCSamplerReduction.Standard => RHIReductionType.Standard,
                                    _ => throw new NotImplementedException(),
                                },
                                AddressModeU = TranslateSAM(staticSampler.AddressModeU),
                                AddressModeV = TranslateSAM(staticSampler.AddressModeV),
                                AddressModeW = TranslateSAM(staticSampler.AddressModeW),
                                MaxAnisotropy = staticSampler.MaxAnisotropy,
                                MipLODBias = staticSampler.MipLODBias,
                                MinLOD = staticSampler.MinLOD,
                                MaxLOD = staticSampler.MaxLOD,
                                Border = staticSampler.Border switch
                                {
                                    SBCSamplerBorder.TransparentBlack => RHISamplerBorder.TransparentBlack,
                                    SBCSamplerBorder.OpaqueBlack => RHISamplerBorder.OpaqueBlack,
                                    SBCSamplerBorder.OpaqueWhite => RHISamplerBorder.OpaqueWhite,
                                    SBCSamplerBorder.OpaqueBlackUInt => RHISamplerBorder.OpaqueBlackUInt,
                                    SBCSamplerBorder.OpaqueWhiteUInt => RHISamplerBorder.OpaqueWhiteUInt,
                                    _ => throw new NotImplementedException(),
                                }
                            };
                        }
                    }

                    static RHIFilterType TranslateFilter(SBCSamplerFilter filter) => filter switch
                    {
                        SBCSamplerFilter.Linear => RHIFilterType.Linear,
                        SBCSamplerFilter.Point => RHIFilterType.Point,
                        _ => throw new NotImplementedException(),
                    };

                    static RHITextureAddressMode TranslateSAM(SBCSamplerAddressMode addressMode) => addressMode switch
                    {
                        SBCSamplerAddressMode.Repeat => RHITextureAddressMode.Repeat,
                        SBCSamplerAddressMode.Mirror => RHITextureAddressMode.Mirror,
                        SBCSamplerAddressMode.Clamp => RHITextureAddressMode.Clamp,
                        SBCSamplerAddressMode.Border => RHITextureAddressMode.Border,
                        _ => throw new NotImplementedException(),
                    };
                }

                RHIGraphicsPipelineBytecode pipelineBytecode = new RHIGraphicsPipelineBytecode();

                //bytecode
                {
                    int totalStages = int.PopCount((int)header.Stages);
                    int totalBytecodeCount = int.PopCount((int)header.Targets) * totalStages;

                    //TODO: dynamic switching.. again
                    int baseOffset = int.PopCount(totalStages * int.TrailingZeroCount((int)SBCTarget.Direct3D12));
                    Debug.Assert(baseOffset + totalStages == totalBytecodeCount);

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

                RHIGraphicsPipeline? graphicsPipeline = null;

                //rhi
                {
                    int propertiesValCount = 0;
                    for (int i = 0; i < outputProperties.Length; i++)
                    {
                        ref ShaderProperty property = ref outputProperties[i];
                        if (!FlagUtility.HasFlag(property.Flags, ShPropertyFlags.HasParent))
                        {
                            propertiesValCount += property.ByteWidth;
                        }
                    }

                    //propertiesValCount /= sizeof(uint);
                    //bool uniqueCb = (propertiesValCount + expectedConstantsSize / sizeof(uint)) > 32;
                    bool uniqueCb = FlagUtility.HasFlag(header.Flags, SBCHeaderFlags.HeaderIsBuffer);

                    RHIGraphicsPipelineDescription pipelineDescription = new RHIGraphicsPipelineDescription
                    {
                        Rasterizer = new RHIGPRasterizer
                        {
                            Fill = rasterizer.FillMode switch
                            {
                                SBCFillMode.Solid => RHIFillMode.Solid,
                                SBCFillMode.Wireframe => RHIFillMode.Wireframe,
                                _ => throw new NotImplementedException(),
                            },
                            Cull = rasterizer.CullMode switch
                            {
                                SBCCullMode.None => RHICullMode.None,
                                SBCCullMode.Back => RHICullMode.Back,
                                SBCCullMode.Front => RHICullMode.Front,
                                _ => throw new NotImplementedException(),
                            },
                            FrontCounterClockwise = rasterizer.FrontCounterClockwise,
                            DepthBias = rasterizer.DepthBias,
                            DepthBiasClamp = rasterizer.DepthBiasClamp,
                            SlopeScaledDepthBias = rasterizer.SlopeScaledDepthBias,
                            DepthClipEnabled = rasterizer.DepthClipEnable,
                            ConservativeRaster = rasterizer.ConservativeRaster,
                        },

                        DepthStencil = new RHIGPDepthStencil
                        {
                            DepthEnabled = depthStencil.DepthEnable,
                            DepthWriteMask = (RHIDepthWriteMask)depthStencil.WriteMask,
                            DepthFunction = ConvertComparisonFunc(depthStencil.DepthFunc),
                            StencilEnabled = depthStencil.StencilEnable,
                            StencilReadMask = depthStencil.StencilReadMask,
                            StencilWriteMask = depthStencil.StencilWriteMask,

                            FrontFace = ConvertFace(depthStencil.FrontFace),
                            BackFace = ConvertFace(depthStencil.BackFace),
                        },

                        PrimitiveTopologyType = topology switch
                        {
                            SBCPrimitiveTopology.Triangle => RHIPrimitiveTopologyType.Triangle,
                            SBCPrimitiveTopology.Line => RHIPrimitiveTopologyType.Line,
                            SBCPrimitiveTopology.Point => RHIPrimitiveTopologyType.Point,
                            SBCPrimitiveTopology.Patch => RHIPrimitiveTopologyType.Patch,
                            _ => throw new NotImplementedException(),
                        },

                        Blend = new RHIGPBlend
                        {
                            AlphaToCoverageEnabled = blend.AlphaToCoverageEnable,
                            IndependentBlendEnabled = blend.IndependentBlendEnable,

                            RenderTargets = rtBlends.Length == 0 ? Array.Empty<RHIGPBlendRenderTarget>() : ConvertRTBlends(rtBlends),
                        },

                        InputElements = inputElements,
                        ImmutableSamplers = staticSamplers,

                        Expected32BitConstants = expectedConstantsSize / sizeof(uint),

                        Header32BitConstants = header.HeaderSize / sizeof(uint),
                        UseBufferForHeader = uniqueCb,
                    };

                    RHIDevice device = RHIDevice.Instance ?? throw new NullReferenceException();
                    graphicsPipeline = device.CreateGraphicsPipeline(pipelineDescription, pipelineBytecode, sourcePath);

                    static RHIGPStencilFace ConvertFace(SBCDepthStencilFace face) => new RHIGPStencilFace
                    {
                        FailOp = ConvertStencilOp(face.Fail),
                        DepthFailOp = ConvertStencilOp(face.DepthFail),
                        PassOp = ConvertStencilOp(face.Pass),
                        Function = ConvertComparisonFunc(face.Func),
                    };
                    static RHIGPBlendRenderTarget[] ConvertRTBlends(SBCRenderTargetBlend[] rtBlends)
                    {
                        RHIGPBlendRenderTarget[] array = new RHIGPBlendRenderTarget[rtBlends.Length];
                        for (int i = 0; i < array.Length; i++)
                        {
                            ref SBCRenderTargetBlend data = ref rtBlends[i];
                            array[i] = new RHIGPBlendRenderTarget
                            {
                                BlendEnabled = data.BlendEnable,
                                SourceBlend = ConvertBlendSource(data.Source),
                                DestinationBlend = ConvertBlendSource(data.Destination),
                                BlendOperation = ConvertBlendOp(data.Operation),
                                SourceBlendAlpha = ConvertBlendSource(data.SourceAlpha),
                                DestinationBlendAlpha = ConvertBlendSource(data.DestinationAlpha),
                                BlendOperationAlpha = ConvertBlendOp(data.OperationAlpha),
                                WriteMask = data.WriteMask
                            };
                        }

                        return array;
                    }

                    static RHIComparisonFunction ConvertComparisonFunc(SBCComparisonFunc func) => func switch
                    {
                        SBCComparisonFunc.None => RHIComparisonFunction.None,
                        SBCComparisonFunc.Never => RHIComparisonFunction.Never,
                        SBCComparisonFunc.Less => RHIComparisonFunction.Less,
                        SBCComparisonFunc.Equal => RHIComparisonFunction.Equal,
                        SBCComparisonFunc.LessEqual => RHIComparisonFunction.LessEqual,
                        SBCComparisonFunc.Greater => RHIComparisonFunction.Greater,
                        SBCComparisonFunc.NotEqual => RHIComparisonFunction.NotEqual,
                        SBCComparisonFunc.GreaterEqual => RHIComparisonFunction.GreaterEqual,
                        SBCComparisonFunc.Always => RHIComparisonFunction.Always,
                        _ => throw new NotImplementedException(),
                    };
                    static RHIStencilOperation ConvertStencilOp(SBCStencilOp op) => op switch
                    {
                        SBCStencilOp.Keep => RHIStencilOperation.Keep,
                        SBCStencilOp.Zero => RHIStencilOperation.Zero,
                        SBCStencilOp.Replace => RHIStencilOperation.Replace,
                        SBCStencilOp.IncrementSaturation => RHIStencilOperation.IncrSaturation,
                        SBCStencilOp.DecrementSaturation => RHIStencilOperation.DecrSatuaration,
                        SBCStencilOp.Invert => RHIStencilOperation.Invert,
                        SBCStencilOp.Increment => RHIStencilOperation.Increment,
                        SBCStencilOp.Decrement => RHIStencilOperation.Decrement,
                        _ => throw new NotImplementedException(),
                    };
                    static RHIBlend ConvertBlendSource(SBCBlendSource blendSource) => blendSource switch
                    {
                        SBCBlendSource.Zero => RHIBlend.Zero,
                        SBCBlendSource.One => RHIBlend.One,
                        SBCBlendSource.SourceColor => RHIBlend.SrcColor,
                        SBCBlendSource.InverseSourceColor => RHIBlend.InvSrcColor,
                        SBCBlendSource.SourceAlpha => RHIBlend.SrcAlpha,
                        SBCBlendSource.InverseSourceAlpha => RHIBlend.InvSrcAlpha,
                        SBCBlendSource.DestinationAlpha => RHIBlend.DestAlpha,
                        SBCBlendSource.InverseDestinationAlpha => RHIBlend.InvDestAlpha,
                        SBCBlendSource.DestinationColor => RHIBlend.DestColor,
                        SBCBlendSource.InverseDestinationColor => RHIBlend.InvDestColor,
                        SBCBlendSource.SourceAlphaSaturate => RHIBlend.SrcAlphaSaturate,
                        SBCBlendSource.BlendFactor => RHIBlend.BlendFactor,
                        SBCBlendSource.InverseBlendFactor => RHIBlend.InvBlendFactor,
                        SBCBlendSource.Source1Color => RHIBlend.Src1Color,
                        SBCBlendSource.InverseSource1Color => RHIBlend.InvSrc1Color,
                        SBCBlendSource.Source1Alpha => RHIBlend.Src1Alpha,
                        SBCBlendSource.InverseSource1Alpha => RHIBlend.InvSrc1Alpha,
                        SBCBlendSource.AlphaFactor => RHIBlend.AlphaFactor,
                        SBCBlendSource.InverseAlphaFactor => RHIBlend.InvAlphaFactor,
                        _ => throw new NotImplementedException(),
                    };
                    static RHIBlendOperation ConvertBlendOp(SBCBlendOp op) => op switch
                    {
                        SBCBlendOp.Add => RHIBlendOperation.Add,
                        SBCBlendOp.Subtract => RHIBlendOperation.Subtract,
                        SBCBlendOp.ReverseSubtract => RHIBlendOperation.ReverseSubtract,
                        SBCBlendOp.Minimum => RHIBlendOperation.Minimum,
                        SBCBlendOp.Maximum => RHIBlendOperation.Maximum,
                        _ => throw new NotImplementedException(),
                    };
                }

                shaderData.UpdateAssetData(shader, outputProperties, remapTable.ToFrozenDictionary(), propertyBlockSize, headerBlockSize, headerFlags, graphicsPipeline);
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

        public SBCHeaderFlags Flags;
        public ushort HeaderSize;

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
        public SBCSamplerFilter Min;
        public SBCSamplerFilter Mag;
        public SBCSamplerFilter Mip;
        public SBCSamplerReduction Reduction;
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

    public enum SBCHeaderFlags : byte
    {
        None = 0,

        ExternalProperties = 1 << 0,
        HeaderIsBuffer = 1 << 1
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

    public enum SBCShaderStages : byte
    {
        None = 0,

        VertexShading,
        PixelShading,
        AllShading
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
        IsReadWrite = 1 << 5
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
        Linear = 0,
        Point
    }

    public enum SBCSamplerReduction : byte
    {
        Standard = 0,
    }

    public enum SBCSamplerAddressMode : byte
    {
        Repeat = 0,
        Mirror,
        Clamp,
        Border
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
