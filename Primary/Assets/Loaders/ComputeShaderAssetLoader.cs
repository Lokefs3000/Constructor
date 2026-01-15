using CommunityToolkit.HighPerformance;
using K4os.Compression.LZ4.Streams;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Rendering.Assets;
using Primary.RHI2;
using Primary.Utility;
using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace Primary.Assets.Loaders
{
    internal sealed class ComputeShaderAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new ComputeShaderAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not ComputeShaderAssetData shaderData)
                throw new ArgumentException(nameof(assetData));

            return new ComputeShaderAsset(shaderData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not ComputeShaderAsset shader)
                throw new ArgumentException(nameof(asset));
            if (assetData is not ComputeShaderAssetData shaderData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                using Stream stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom)!;

                CBCHeader header = CommunityToolkit.HighPerformance.StreamExtensions.Read<CBCHeader>(stream);

                if (header.Header != CBCHeader.ConstHeader)
                    ThrowException("Invalid header present in file: {header} ({utf8})", header.Header, Encoding.UTF8.GetString(MemoryMarshal.Cast<uint, byte>(new ReadOnlySpan<uint>(in header.Header))));
                if (header.Version != CBCHeader.ConstVersion)
                    ThrowException("Invalid version present in file: {version}", header.Version);

                //HACK: Implement dynamic switching
                if (!FlagUtility.HasFlag(header.Targets, CBCTarget.Direct3D12))
                    ThrowException("Compute shader does not have a target for current api: {api}", CBCTarget.Direct3D12);

                using BinaryReader br = new BinaryReader(stream);

                Dictionary<FastStringHash, ComputeShaderKernel> kernels = new Dictionary<FastStringHash, ComputeShaderKernel>();

                List<ShaderProperty> properties = new List<ShaderProperty>();
                ShaderResource[] resources = Array.Empty<ShaderResource>();

                Dictionary<int, string> customNameDict = new Dictionary<int, string>();
                Dictionary<int, string> samplerDict = new Dictionary<int, string>();

                ShaderProperty[] outputProperties = Array.Empty<ShaderProperty>();
                Dictionary<int, int> remapTable = new Dictionary<int, int>();

                for (int kernelIndex = 0; kernelIndex < header.KernelCount; kernelIndex++)
                {
                    CBCKernel kernel = stream.Read<CBCKernel>();
                    string kernelName = br.ReadString();

                    ShHeaderFlags headerFlags = ShHeaderFlags.None;

                    if (FlagUtility.HasFlag(kernel.Flags, CBCKernelFlags.ExternalProperties))
                        headerFlags |= ShHeaderFlags.ExternalProperties;
                    if (FlagUtility.HasFlag(kernel.Flags, CBCKernelFlags.HeaderIsBuffer))
                        headerFlags |= ShHeaderFlags.HeaderIsBuffer;

                    int expectedConstantsSize = stream.ReadByte();

                    properties.Clear();
                    Array.Clear(resources);

                    customNameDict.Clear();
                    samplerDict.Clear();

                    //resources
                    {
                        ushort resourceCount = stream.Read<ushort>();
                        if (resourceCount > 0)
                        {
                            resources = new ShaderResource[resourceCount];

                            for (int i = 0; i < resourceCount; i++)
                            {
                                CBCResourceType type = stream.Read<CBCResourceType>();
                                CBCResourceFlags flags = stream.Read<CBCResourceFlags>();

                                ShResourceType resourceType = ShResourceType.Unknown;
                                switch (type)
                                {
                                    case CBCResourceType.Texture1D: resourceType = ShResourceType.Texture1D; break;
                                    case CBCResourceType.Texture2D: resourceType = ShResourceType.Texture2D; break;
                                    case CBCResourceType.Texture3D: resourceType = ShResourceType.Texture3D; break;
                                    case CBCResourceType.TextureCube: resourceType = ShResourceType.TextureCube; break;
                                    case CBCResourceType.ConstantBuffer: resourceType = ShResourceType.ConstantBuffer; break;
                                    case CBCResourceType.StructuredBuffer: resourceType = ShResourceType.StructuredBuffer; break;
                                    case CBCResourceType.ByteAddressBuffer: resourceType = ShResourceType.ByteAddressBuffer; break;
                                    case CBCResourceType.SamplerState: resourceType = ShResourceType.SamplerState; break;
                                    default: ThrowException("Unexpected resource type value: {t} ({v})", type, (int)type); break;
                                }

                                ShResourceFlags resourceFlags = ShResourceFlags.None;
                                if (FlagUtility.HasFlag(flags, CBCResourceFlags.IsReadWrite))
                                    resourceFlags |= ShResourceFlags.ReadWrite;
                                if ((flags & ~CBCResourceFlags.IsReadWrite) > 0)
                                    resourceFlags |= ShResourceFlags.Property;

                                string name = br.ReadString();

                                resources[i] = new ShaderResource(name, resourceType, ShPropertyStages.ComputeShading, resourceFlags);

                                //Must be a property as there are only property-based attributes that can be assigned to a resource
                                if (FlagUtility.HasFlag(resourceFlags, ShResourceFlags.Property))
                                {
                                    ShPropertyFlags propFlags = ShPropertyFlags.Property;
                                    ShPropertyDisplay propDisplay = ShPropertyDisplay.Default;
                                    ShPropertyDefault propDefault = ShPropertyDefault.TexWhite;

                                    string? customName = null;
                                    object? auxilary = null;

                                    if (FlagUtility.HasFlag(flags, CBCResourceFlags.IsReadWrite))
                                        propFlags |= ShPropertyFlags.ReadWrite;
                                    if (FlagUtility.HasFlag(flags, CBCResourceFlags.Constants))
                                        propFlags |= ShPropertyFlags.Constants;
                                    if (FlagUtility.HasFlag(flags, CBCResourceFlags.Display))
                                    {
                                        CBCPropertyDisplay read = stream.Read<CBCPropertyDisplay>();
                                        switch (read)
                                        {
                                            case CBCPropertyDisplay.Default: propDisplay = ShPropertyDisplay.Default; break;
                                            case CBCPropertyDisplay.Color: propDisplay = ShPropertyDisplay.Color; break;
                                            default: ThrowException("Unexpected resource property display value: {t} ({v})", read, (int)read); break;
                                        }
                                    }
                                    if (FlagUtility.HasFlag(flags, CBCResourceFlags.Global))
                                    {
                                        propFlags |= ShPropertyFlags.Global;

                                        CBCAttributeGlobal attribute = stream.Read<CBCAttributeGlobal>();
                                        if (attribute.HasCustomName)
                                            customName = br.ReadString();
                                    }
                                    if (FlagUtility.HasFlag(flags, CBCResourceFlags.Property))
                                    {
                                        CBCAttributeProperty attribute = stream.Read<CBCAttributeProperty>();
                                        propDefault = attribute.Default switch
                                        {
                                            CBCPropertyDefault.NumOne => ShPropertyDefault.NumOne,
                                            CBCPropertyDefault.NumZero => ShPropertyDefault.NumZero,
                                            CBCPropertyDefault.NumIdentity => ShPropertyDefault.NumIdentity,
                                            CBCPropertyDefault.TexWhite => ShPropertyDefault.TexWhite,
                                            CBCPropertyDefault.TexBlack => ShPropertyDefault.TexBlack,
                                            CBCPropertyDefault.TexNormal => ShPropertyDefault.TexNormal,
                                            CBCPropertyDefault.TexMask => ShPropertyDefault.TexMask,
                                            _ => throw new NotImplementedException()
                                        };

                                        if (attribute.HasCustomName)
                                            customName = br.ReadString();
                                    }
                                    if (FlagUtility.HasFlag(flags, CBCResourceFlags.Sampled))
                                    {
                                        propFlags |= ShPropertyFlags.Sampled;

                                        CBCAttributeSampled attribute = stream.Read<CBCAttributeSampled>();
                                        if (attribute.HasCustomName)
                                            auxilary = br.ReadString();
                                        else
                                            auxilary = name + "_Sampler";
                                    }

                                    properties.Add(new ShaderProperty(name, (ushort)i, ushort.MaxValue, ushort.MaxValue, type switch
                                    {
                                        CBCResourceType.Texture1D => ShPropertyType.Texture,
                                        CBCResourceType.Texture2D => ShPropertyType.Texture,
                                        CBCResourceType.Texture3D => ShPropertyType.Texture,
                                        CBCResourceType.TextureCube => ShPropertyType.Texture,
                                        CBCResourceType.ConstantBuffer => ShPropertyType.Buffer,
                                        CBCResourceType.StructuredBuffer => ShPropertyType.Buffer,
                                        CBCResourceType.ByteAddressBuffer => ShPropertyType.Buffer,
                                        _ => throw new InvalidOperationException()
                                    }, propDefault, ShPropertyStages.ComputeShading, propFlags, propDisplay));

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
                                    CBCValueGeneric generic = (CBCValueGeneric)(size & 0xf);
                                    int rows = (size >> 12) & 0xf;
                                    int columns = (size >> 9) & 0xf;

                                    switch (generic)
                                    {
                                        case CBCValueGeneric.Single:
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
                                        case CBCValueGeneric.Double:
                                            size = (ushort)(sizeof(double) * rows * columns);
                                            type = ShPropertyType.Double;
                                            break;
                                        case CBCValueGeneric.Int:
                                            size = (ushort)(sizeof(int) * rows * columns);
                                            type = ShPropertyType.Int32;
                                            break;
                                        case CBCValueGeneric.UInt:
                                            size = (ushort)(sizeof(uint) * rows * columns);
                                            type = ShPropertyType.UInt32;
                                            break;
                                    }
                                }

                                Debug.Assert(type > ShPropertyType.Texture);

                                ushort byteOffset = stream.Read<ushort>();
                                CBCPropertyFlags flags = stream.Read<CBCPropertyFlags>();
                                string name = br.ReadString();

                                ShPropertyFlags propFlags = ShPropertyFlags.None;
                                ShPropertyDisplay propDisplay = ShPropertyDisplay.Default;
                                ShPropertyDefault propDefault = ShPropertyDefault.NumIdentity;

                                string? customName = null;

                                if (FlagUtility.HasFlag(flags, CBCPropertyFlags.HasParent))
                                    propFlags |= ShPropertyFlags.HasParent;

                                if (FlagUtility.HasFlag(flags, CBCPropertyFlags.Display))
                                {
                                    propDisplay = stream.Read<CBCPropertyDisplay>() switch
                                    {
                                        CBCPropertyDisplay.Default => ShPropertyDisplay.Default,
                                        CBCPropertyDisplay.Color => ShPropertyDisplay.Color,
                                        _ => throw new NotImplementedException(),
                                    };
                                }
                                if (FlagUtility.HasFlag(flags, CBCPropertyFlags.Global))
                                {
                                    flags |= CBCPropertyFlags.Global;

                                    CBCAttributeGlobal attribute = stream.Read<CBCAttributeGlobal>();
                                    if (attribute.HasCustomName)
                                        customName = br.ReadString();
                                }
                                if (FlagUtility.HasFlag(flags, CBCPropertyFlags.Property))
                                {
                                    flags |= CBCPropertyFlags.Property;

                                    CBCAttributeProperty attribute = stream.Read<CBCAttributeProperty>();
                                    propDefault = attribute.Default switch
                                    {
                                        CBCPropertyDefault.NumOne => ShPropertyDefault.NumOne,
                                        CBCPropertyDefault.NumZero => ShPropertyDefault.NumZero,
                                        CBCPropertyDefault.NumIdentity => ShPropertyDefault.NumIdentity,
                                        CBCPropertyDefault.TexWhite => ShPropertyDefault.TexWhite,
                                        CBCPropertyDefault.TexBlack => ShPropertyDefault.TexBlack,
                                        CBCPropertyDefault.TexNormal => ShPropertyDefault.TexNormal,
                                        CBCPropertyDefault.TexMask => ShPropertyDefault.TexMask,
                                        _ => throw new NotImplementedException()
                                    };

                                    if (attribute.HasCustomName)
                                        customName = br.ReadString();
                                }

                                properties.Add(new ShaderProperty(name, byteOffset, size, ushort.MaxValue, type, propDefault, ShPropertyStages.ComputeShading, propFlags, propDisplay));

                                if (customName != null)
                                    customNameDict[(1 << 15) | i] = customName;
                            }
                        }
                    }

                    Array.Clear(outputProperties);
                    remapTable.Clear();

                    int propertyBlockSize = 0;
                    int headerBlockSize = 0;

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

                        if (!FlagUtility.HasFlag(kernel.Flags, CBCKernelFlags.ExternalProperties))
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

                    RHIGPImmutableSampler[] staticSamplers = Array.Empty<RHIGPImmutableSampler>();

                    //static samplers
                    {
                        byte count = (byte)stream.ReadByte();
                        if (count > 0)
                        {
                            staticSamplers = new RHIGPImmutableSampler[count];
                            for (int i = 0; i < count; i++)
                            {
                                CBCStaticSampler staticSampler = stream.Read<CBCStaticSampler>();
                                staticSamplers[i] = new RHIGPImmutableSampler
                                {
                                    Min = TranslateFilter(staticSampler.Min),
                                    Mag = TranslateFilter(staticSampler.Mag),
                                    Mip = TranslateFilter(staticSampler.Mip),
                                    ReductionType = staticSampler.Reduction switch
                                    {
                                        CBCSamplerReduction.Standard => RHIReductionType.Standard,
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
                                        CBCSamplerBorder.TransparentBlack => RHISamplerBorder.TransparentBlack,
                                        CBCSamplerBorder.OpaqueBlack => RHISamplerBorder.OpaqueBlack,
                                        CBCSamplerBorder.OpaqueWhite => RHISamplerBorder.OpaqueWhite,
                                        CBCSamplerBorder.OpaqueBlackUInt => RHISamplerBorder.OpaqueBlackUInt,
                                        CBCSamplerBorder.OpaqueWhiteUInt => RHISamplerBorder.OpaqueWhiteUInt,
                                        _ => throw new NotImplementedException(),
                                    }
                                };
                            }
                        }

                        static RHIFilterType TranslateFilter(CBCSamplerFilter filter) => filter switch
                        {
                            CBCSamplerFilter.Linear => RHIFilterType.Linear,
                            CBCSamplerFilter.Point => RHIFilterType.Point,
                            _ => throw new NotImplementedException(),
                        };

                        static RHITextureAddressMode TranslateSAM(CBCSamplerAddressMode addressMode) => addressMode switch
                        {
                            CBCSamplerAddressMode.Repeat => RHITextureAddressMode.Repeat,
                            CBCSamplerAddressMode.Mirror => RHITextureAddressMode.Mirror,
                            CBCSamplerAddressMode.Clamp => RHITextureAddressMode.Clamp,
                            CBCSamplerAddressMode.Border => RHITextureAddressMode.Border,
                            _ => throw new NotImplementedException(),
                        };
                    }

                    RHIComputePipelineBytecode bytecode = new RHIComputePipelineBytecode();

                    //bytecode
                    {
                        int totalBytecodeCount = int.PopCount((int)header.Targets) * header.KernelCount;

                        //TODO: dynamic switching.. again
                        int baseOffset = int.PopCount(header.KernelCount * int.TrailingZeroCount((int)CBCTarget.Direct3D12));
                        Debug.Assert(baseOffset + header.KernelCount <= totalBytecodeCount);

                        long startOffset = stream.Position;

                        stream.Seek(startOffset + (baseOffset + int.TrailingZeroCount((int)header.KernelCount) * 2) * sizeof(int), SeekOrigin.Begin);

                        int offset = stream.Read<int>();
                        int length = stream.Read<int>();

                        stream.Seek(offset, SeekOrigin.Begin);

                        byte[] array = new byte[length];
                        stream.ReadExactly(array);

                        bytecode.Compute = array;
                    }

                    RHIComputePipeline? pipeline = null;

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

                        bool uniqueCb = FlagUtility.HasFlag(kernel.Flags, CBCKernelFlags.HeaderIsBuffer);

                        RHIComputePipelineDescription description = new RHIComputePipelineDescription
                        {
                            ImmutableSamplers = staticSamplers,

                            Expected32BitConstants = expectedConstantsSize / sizeof(uint),

                            Header32BitConstants = kernel.HeaderSize / sizeof(uint),
                            UseBufferForHeader = uniqueCb
                        };

                        RHIDevice device = RHIDevice.Instance ?? throw new NullReferenceException();
                        pipeline = device.CreateComputePipeline(description, bytecode, sourcePath);
                    }

                    KernelThreadSize threadSize = new KernelThreadSize(kernel.ThreadSizeX, kernel.ThreadSizeY, kernel.ThreadSizeZ);

                    kernels.Add(kernelName, new ComputeShaderKernel(shader, threadSize, outputProperties, remapTable.ToFrozenDictionary(), propertyBlockSize, headerBlockSize, headerFlags, pipeline));
                }

                shaderData.UpdateAssetData(shader, kernels.ToFrozenDictionary());

                [DoesNotReturn]
                void ThrowException(string message, params object?[] args)
                {
                    shaderData.UpdateAssetFailed(shader);

                    EngLog.Assets.Error("[a:{path}]: " + message, [sourcePath, .. args]);
                    throw new Exception("Unexpected error");
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                modelData.UpdateAssetFailed(model);
                EngLog.Assets.Error(ex, "Failed to load compute shader: {name}", sourcePath);
            }
#else
            finally
            {

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

    public struct CBCHeader
    {
        public uint Header;
        public uint Version;

        public CBCTarget Targets;
       
        public ushort KernelCount;

        public const uint ConstHeader = 0x20434243;
        public const uint ConstVersion = 1;
    }

    public struct CBCKernel
    {
        public CBCKernelFlags Flags;
        public ushort HeaderSize;

        public ushort ThreadSizeX;
        public ushort ThreadSizeY;
        public ushort ThreadSizeZ;
    }

    public struct CBCAttributeGlobal
    {
        public bool HasCustomName;
    }

    public struct CBCAttributeProperty
    {
        public CBCPropertyDefault Default;
        public bool HasCustomName;
    }

    public struct CBCAttributeSampled
    {
        public bool HasCustomName;
    }

    public struct CBCStaticSampler
    {
        public CBCSamplerFilter Min;
        public CBCSamplerFilter Mag;
        public CBCSamplerFilter Mip;
        public CBCSamplerReduction Reduction;
        public CBCSamplerAddressMode AddressModeU;
        public CBCSamplerAddressMode AddressModeV;
        public CBCSamplerAddressMode AddressModeW;
        public byte MaxAnisotropy;
        public float MipLODBias;
        public float MinLOD;
        public float MaxLOD;
        public CBCSamplerBorder Border;
    }

    public enum CBCTarget : byte
    {
        None = 0,

        Direct3D12 = 1 << 0,
        Vulkan = 1 << 1
    }

    public enum CBCKernelFlags : byte
    {
        None = 0,

        ExternalProperties = 1 << 0,
        HeaderIsBuffer = 1 << 1,
    }

    public enum CBCResourceType : byte
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

    public enum CBCResourceFlags : byte
    {
        None = 0,

        Constants = 1 << 0,
        Display = 1 << 1,
        Global = 1 << 2,
        Property = 1 << 3,
        Sampled = 1 << 4,
        IsReadWrite = 1 << 5
    }

    public enum CBCPropertyDisplay : byte
    {
        Default = 0,
        Color
    }

    public enum CBCPropertyDefault : byte
    {
        NumOne = 0,
        NumZero,
        NumIdentity,

        TexWhite,
        TexBlack,
        TexMask,
        TexNormal
    }

    public enum CBCPropertyFlags : byte
    {
        None = 0,

        Display = 1 << 0,
        Global = 1 << 1,
        Property = 1 << 2,
        HasParent = 1 << 3
    }

    public enum CBCValueGeneric : byte
    {
        Single,
        Double,
        Int,
        UInt
    }

    public enum CBCSamplerFilter : byte
    {
        Linear = 0,
        Point
    }

    public enum CBCSamplerReduction : byte
    {
        Standard = 0,
    }

    public enum CBCSamplerAddressMode : byte
    {
        Repeat = 0,
        Mirror,
        Clamp,
        Border
    }

    public enum CBCSamplerBorder : byte
    {
        TransparentBlack = 0,
        OpaqueBlack,
        OpaqueWhite,
        OpaqueBlackUInt,
        OpaqueWhiteUInt
    }
}
