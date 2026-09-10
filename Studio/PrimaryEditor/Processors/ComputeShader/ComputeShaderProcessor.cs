using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Editor.Shaders;
using Editor.Shaders.Attributes;
using Editor.Shaders.Data;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Assets.Types;
using Primary.Common;
using Primary.RHI;
using PrimaryEditor.Assets;
using PrimaryEditor.Utility;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PrimaryEditor.Processors.ComputeShader
{
    public static class ComputeShaderProcessor
    {
        public static ShaderProcesserResult Execute(ComputeShaderConfiguration shader, ShaderCompileTarget compileTarget, Stream stream)
        {
            IAssetIdProvider idProvider = shader.IdProvider!;

            if (!idProvider.TryGetLocalPathForId(shader.DefaultId, out string? filePath))
            {
                throw new Exception($"Failed to get path for id: {shader.DefaultId}");
            }

            string? sourceIn = FilesystemManager.ReadAllText(filePath);
            if (sourceIn == null)
            {
                throw new Exception($"Failed to read source: {filePath}");
            }

            Editor.Shaders.ShaderProcessor processor = new Editor.Shaders.ShaderProcessor(EdLog.Assets, ShaderAttributeSettings.Compute);
            ShaderProcesserResult? resultNullable = processor.ProcessCompute(new ComputeShaderProcessorArgs
            {
                InputSource = sourceIn,
                SourceFileName = filePath,

                IncludeDirectories = shader.IncludeDirectories,

                Targets = compileTarget
            });

            if (!resultNullable.HasValue)
            {
                throw new Exception($"Failed to process compute shader: {filePath}");
            }

            ShaderProcesserResult result = resultNullable.Value;

            using BinaryWriter bw = new BinaryWriter(stream, Encoding.UTF8, true);

            Dictionary<ReferenceIndex, ShPropertyStages> dataStageUsageDict = CreateStageUsageDictionary(result.Data);

            WriteHeader(bw, ref result, result.Data);

            int kernelIndex = 0;
            foreach (FunctionData function in result.Data.Functions)
            {
                int indexOfKernel = Array.FindIndex(function.Attributes, static (x) => x.Signature is AttributeKernel);
                if (indexOfKernel != -1)
                {
                    WriteKernel(bw, Array.Find(function.Attributes, static (x) => x.Signature is AttributeNumThreads), ref result, result.Data);
                    bw.Write(function.Name);

                    WriteDescription(bw, shader, result.Data);
                    WriteResourceList(bw, result.Data, dataStageUsageDict);
                    WriteRawPropertyList(bw, result.Data);
                    WriteStaticSamplers(bw, result.Data);

                    ++kernelIndex;
                }
            }

            WriteBytecodeOffsetBlock(bw, ref result);
            WriteBytecode(bw, ref result);

            return result;
        }

        private static void WriteHeader(BinaryWriter bw, ref readonly ShaderProcesserResult result, ShaderData data)
        {
            CBCTarget target = CBCTarget.None;

            if (Flags.HasFlag(result.Targets, ShaderCompileTarget.Direct3D12))
                target |= CBCTarget.Direct3D12;
            if (Flags.HasFlag(result.Targets, ShaderCompileTarget.Vulkan))
                target |= CBCTarget.Vulkan;

            CBCHeader header = new CBCHeader
            {
                Header = CBCHeader.ConstHeader,
                Version = CBCHeader.ConstVersion,

                Targets = target,

                KernelCount = (ushort)result.Data.KernelCount
            };

            bw.Write(header);
        }

        private static void WriteKernel(BinaryWriter bw, AttributeData numThreads, ref readonly ShaderProcesserResult result, ShaderData data)
        {
            CBCKernelFlags flags = CBCKernelFlags.None;

            if (!data.GeneratePropertiesInHeader)
                flags |= CBCKernelFlags.ExternalProperties;
            if (data.AreConstantsSeparated)
                flags |= CBCKernelFlags.HeaderIsBuffer;

            CBCKernel kernel = new CBCKernel
            {
                Flags = flags,
                HeaderSize = (ushort)data.HeaderBytesize,

                ThreadSizeX = (ushort)numThreads.GetVariable<int>("X"),
                ThreadSizeY = (ushort)numThreads.GetVariable<int>("Y"),
                ThreadSizeZ = (ushort)numThreads.GetVariable<int>("Z"),
            };

            bw.Write(kernel);
        }

        private static void WriteDescription(BinaryWriter bw, ComputeShaderConfiguration shader, ShaderData data)
        {
            int expectedConstantsSize = 0;
            int idx = data.Resources.FindIndex((x) => x.Type == ResourceType.ConstantBuffer && Array.Exists(x.Attributes, (y) => y.Signature is AttributeConstants));
            if (idx != -1)
            {
                ref readonly ResourceData resData = ref data.Resources[idx];
                ref readonly StructData @struct = ref data.GetRefSource(resData.Value);

                Checking.Assert(!Unsafe.IsNullRef(in @struct));
                expectedConstantsSize = EstimateStructSize(in @struct);

                int EstimateStructSize(ref readonly StructData @struct)
                {
                    int size = 0;
                    foreach (ref readonly VariableData variable in @struct.Variables.AsSpan())
                    {
                        ValueDataRef generic = variable.Generic;
                        if (generic.Generic == ValueGeneric.Custom)
                        {
                            ref readonly StructData varStruct = ref data.GetRefSource(generic);
                            Debug.Assert(!Unsafe.IsNullRef(in varStruct));

                            size += EstimateStructSize(in varStruct);
                        }
                        else
                        {
                            size += generic.Generic switch
                            {
                                ValueGeneric.Float => sizeof(float),
                                ValueGeneric.Double => sizeof(double),
                                ValueGeneric.UInt => sizeof(uint),
                                ValueGeneric.Int => sizeof(int),
                                _ => throw new NotSupportedException()
                            } * generic.Rows * generic.Columns;
                        }
                    }

                    return size;
                }

                if (expectedConstantsSize > 128)
                    throw new Exception($"Constants size is larger then 128 bytes (actual: {expectedConstantsSize}) (TODO: Add custom exception)"/*TODO: Add custom exception*/);
            }

            bw.Write((byte)expectedConstantsSize);
        }

        private static void WriteResourceList(BinaryWriter bw, ShaderData data, Dictionary<ReferenceIndex, ShPropertyStages> usageDict)
        {
            int count = 0;

            int i = 0;
            foreach (ref readonly ResourceData resource in data.Resources)
            {
                if (usageDict.ContainsKey(new ReferenceIndex(ReferenceType.Resource, i++)))
                    count++;
            }

            bw.Write((ushort)count);

            i = 0;
            foreach (ref readonly ResourceData resource in data.Resources)
            {
                if (!usageDict.TryGetValue(new ReferenceIndex(ReferenceType.Resource, i++), out ShPropertyStages stages))
                    continue;

                bw.Write(resource.Type switch
                {
                    ResourceType.Texture1D => CBCResourceType.Texture1D,
                    ResourceType.Texture2D => CBCResourceType.Texture2D,
                    ResourceType.Texture3D => CBCResourceType.Texture3D,
                    ResourceType.TextureCube => CBCResourceType.TextureCube,
                    ResourceType.ConstantBuffer => CBCResourceType.ConstantBuffer,
                    ResourceType.StructuredBuffer => CBCResourceType.StructuredBuffer,
                    ResourceType.ByteAddressBuffer => CBCResourceType.ByteAddressBuffer,
                    ResourceType.SamplerState => CBCResourceType.SamplerState,
                    _ => throw new NotSupportedException()
                });

                long backup = bw.BaseStream.Position;

                bw.Write(CBCResourceFlags.None);
                bw.Write(resource.Name);

                AttributeData attributeData = new AttributeData();
                CBCResourceFlags flags = CBCResourceFlags.None;

                if (resource.IsReadWrite)
                    flags |= CBCResourceFlags.IsReadWrite;

                if ((attributeData = Array.Find(resource.Attributes, static (x) => x.Signature is AttributeConstants)).Signature != null)
                {
                    flags |= CBCResourceFlags.Constants;
                }
                if ((attributeData = Array.Find(resource.Attributes, static (x) => x.Signature is AttributeDisplay)).Signature != null && attributeData.Data != null)
                {
                    flags |= CBCResourceFlags.Display;
                    bw.Write(attributeData.GetVariable<PropertyDisplay>("Display") switch
                    {
                        PropertyDisplay.Default => CBCPropertyDisplay.Default,
                        PropertyDisplay.Color => CBCPropertyDisplay.Color,
                        _ => throw new NotImplementedException()
                    });
                }
                if ((attributeData = Array.Find(resource.Attributes, static (x) => x.Signature is AttributeGlobal)).Signature != null)
                {
                    flags |= CBCResourceFlags.Global;

                    string? customName = attributeData.GetVariable<string>("Name");
                    bw.Write(new CBCAttributeGlobal
                    {
                        HasCustomName = customName != null
                    });

                    if (customName != null)
                        bw.Write(customName);
                }
                if ((attributeData = Array.Find(resource.Attributes, static (x) => x.Signature is AttributeProperty)).Signature != null)
                {
                    flags |= CBCResourceFlags.Property;

                    string? customName = attributeData.GetVariable<string>("Name");
                    bw.Write(new CBCAttributeProperty
                    {
                        Default = attributeData.GetVariable<PropertyDefault>("Default") switch
                        {
                            PropertyDefault.NumOne => CBCPropertyDefault.NumOne,
                            PropertyDefault.NumZero => CBCPropertyDefault.NumZero,
                            PropertyDefault.NumIdentity => CBCPropertyDefault.NumIdentity,
                            PropertyDefault.TexWhite => CBCPropertyDefault.TexWhite,
                            PropertyDefault.TexBlack => CBCPropertyDefault.TexBlack,
                            PropertyDefault.TexMask => CBCPropertyDefault.TexMask,
                            PropertyDefault.TexNormal => CBCPropertyDefault.TexNormal,
                            _ => throw new NotImplementedException()
                        },
                        HasCustomName = customName != null
                    });

                    if (customName != null)
                        bw.Write(customName);
                }
                if ((attributeData = Array.Find(resource.Attributes, static (x) => x.Signature is AttributeSampled)).Signature != null)
                {
                    flags |= CBCResourceFlags.Sampled;

                    string? customName = attributeData.GetVariable<string>("Sampler");
                    bw.Write(new CBCAttributeSampled
                    {
                        HasCustomName = customName != null
                    });

                    if (customName != null)
                        bw.Write(customName);
                }

                long current = bw.BaseStream.Position;

                bw.BaseStream.Seek(backup, SeekOrigin.Begin);
                bw.Write(flags);
                bw.BaseStream.Seek(current, SeekOrigin.Begin);
            }
        }

        private static void WriteRawPropertyList(BinaryWriter bw, ShaderData data)
        {
            List<ValueTuple<VariableData, bool>> variables = new List<ValueTuple<VariableData, bool>>();
            foreach (ref readonly PropertyData resource in data.Properties)
            {
                ValueDataRef generic = resource.Generic;
                Checking.Assert(generic.IsSpecified);

                variables.Add((new VariableData(resource.Name, resource.Attributes, generic, null), false));

                if (generic.Generic == ValueGeneric.Custom)
                {
                    ref readonly StructData @struct = ref data.GetRefSource(generic);
                    AppendSubStructMembers(in @struct, variables);
                }
            }

            bw.Write((ushort)variables.Count);

            int localByteOffset = 0;
            int globalByteOffset = 0;

            foreach (ref readonly ValueTuple<VariableData, bool> tuple in variables.AsSpan())
            {
                VariableData variable = tuple.Item1;

                ValueDataRef generic = variable.Generic;
                Checking.Assert(generic.IsSpecified);

                int size = data.CalculateSize(generic);

                if (generic.Generic == ValueGeneric.Custom)
                {
                    bw.Write((ushort)((1 << 15) | size));
                }
                else
                {
                    bw.Write((ushort)((int)(generic.Generic switch
                    {
                        ValueGeneric.Float => CBCValueGeneric.Single,
                        ValueGeneric.Double => CBCValueGeneric.Double,
                        ValueGeneric.Int => CBCValueGeneric.Int,
                        ValueGeneric.UInt => CBCValueGeneric.UInt,
                        _ => throw new NotImplementedException(),
                    }) | (generic.Rows << 12) | (generic.Columns << 9)));
                }

                long backup = bw.BaseStream.Position;

                bw.Write(ushort.MaxValue);
                bw.Write(CBCPropertyFlags.None);

                bw.Write(variable.Name);

                AttributeData attributeData = new AttributeData();
                CBCPropertyFlags flags = CBCPropertyFlags.None;

                if (tuple.Item2)
                    flags |= CBCPropertyFlags.HasParent;

                if ((attributeData = Array.Find(variable.Attributes, static (x) => x.Signature is AttributeDisplay)).Signature != null && attributeData.Data != null)
                {
                    flags |= CBCPropertyFlags.Display;
                    bw.Write(attributeData.GetVariable<PropertyDisplay>("Display") switch
                    {
                        PropertyDisplay.Default => CBCPropertyDisplay.Default,
                        PropertyDisplay.Color => CBCPropertyDisplay.Color,
                        _ => throw new NotImplementedException()
                    });
                }
                if ((attributeData = Array.Find(variable.Attributes, static (x) => x.Signature is AttributeGlobal)).Signature != null)
                {
                    flags |= CBCPropertyFlags.Global;

                    string? customName = attributeData.GetVariable<string>("Name");
                    bw.Write(new CBCAttributeGlobal
                    {
                        HasCustomName = customName != null
                    });

                    if (customName != null)
                        bw.Write(customName);
                }
                if ((attributeData = Array.Find(variable.Attributes, static (x) => x.Signature is AttributeProperty)).Signature != null)
                {
                    flags |= CBCPropertyFlags.Property;

                    string? customName = attributeData.GetVariable<string>("Name");
                    bw.Write(new CBCAttributeProperty
                    {
                        Default = attributeData.GetVariable<PropertyDefault>("Default") switch
                        {
                            PropertyDefault.NumOne => CBCPropertyDefault.NumOne,
                            PropertyDefault.NumZero => CBCPropertyDefault.NumZero,
                            PropertyDefault.NumIdentity => CBCPropertyDefault.NumIdentity,
                            PropertyDefault.TexWhite => CBCPropertyDefault.TexWhite,
                            PropertyDefault.TexBlack => CBCPropertyDefault.TexBlack,
                            PropertyDefault.TexMask => CBCPropertyDefault.TexMask,
                            PropertyDefault.TexNormal => CBCPropertyDefault.TexNormal,
                            _ => throw new NotImplementedException()
                        },
                        HasCustomName = customName != null
                    });

                    if (customName != null)
                        bw.Write(customName);
                }

                long current = bw.BaseStream.Position;

                bw.BaseStream.Seek(backup, SeekOrigin.Begin);
                bw.Write((ushort)(Flags.HasFlag(flags, CBCPropertyFlags.Global) ? globalByteOffset : localByteOffset));
                bw.Write(flags);
                bw.BaseStream.Seek(current, SeekOrigin.Begin);

                if (Flags.HasFlag(flags, CBCPropertyFlags.Global))
                    globalByteOffset += size;
                else
                    localByteOffset += size;
            }

            void AppendSubStructMembers(ref readonly StructData @struct, List<ValueTuple<VariableData, bool>> variables)
            {
                foreach (ref readonly VariableData variable in @struct.Variables.AsSpan())
                {
                    ValueDataRef generic = variable.Generic;
                    Checking.Assert(generic.IsSpecified);

                    variables.Add((variable, true));

                    if (generic.Generic == ValueGeneric.Custom)
                    {
                        ref readonly StructData childStruct = ref data.GetRefSource(generic);
                        AppendSubStructMembers(in @struct, variables);
                    }
                }
            }
        }

        private static void WriteStaticSamplers(BinaryWriter bw, ShaderData data)
        {
            bw.Write((byte)data.StaticSamplers.Length);
            if (!data.StaticSamplers.IsEmpty)
            {
                foreach (ref readonly StaticSamplerData samplerData in data.StaticSamplers)
                {
                    bw.Write(new CBCStaticSampler
                    {
                        Min = TranslateFilter(samplerData.Min),
                        Mag = TranslateFilter(samplerData.Mag),
                        Mip = TranslateFilter(samplerData.Mip),
                        Reduction = samplerData.Reduction switch
                        {
                            SamplerReductionType.Standard => CBCSamplerReduction.Standard,
                            _ => throw new NotImplementedException(),
                        },
                        AddressModeU = TranslateSAM(samplerData.AddressModeU),
                        AddressModeV = TranslateSAM(samplerData.AddressModeV),
                        AddressModeW = TranslateSAM(samplerData.AddressModeW),
                        MaxAnisotropy = (byte)Math.Clamp(samplerData.MaxAnisotropy, 1, 16),
                        MipLODBias = samplerData.MipLODBias,
                        MinLOD = samplerData.MinLOD,
                        MaxLOD = samplerData.MaxLOD,
                        Border = samplerData.Border switch
                        {
                            SamplerBorder.TransparentBlack => CBCSamplerBorder.TransparentBlack,
                            SamplerBorder.OpaqueBlack => CBCSamplerBorder.OpaqueBlack,
                            SamplerBorder.OpaqueWhite => CBCSamplerBorder.OpaqueWhite,
                            SamplerBorder.OpaqueBlackUInt => CBCSamplerBorder.OpaqueBlackUInt,
                            SamplerBorder.OpaqueWhiteUInt => CBCSamplerBorder.OpaqueWhiteUInt,
                            _ => throw new NotImplementedException(),
                        }
                    });
                }
            }

            static CBCSamplerFilter TranslateFilter(SamplerFilter filter) => filter switch
            {
                SamplerFilter.Linear => CBCSamplerFilter.Linear,
                SamplerFilter.Point => CBCSamplerFilter.Point,
                _ => throw new NotImplementedException(),
            };

            static CBCSamplerAddressMode TranslateSAM(SamplerAddressMode addressMode) => addressMode switch
            {
                SamplerAddressMode.Repeat => CBCSamplerAddressMode.Repeat,
                SamplerAddressMode.Mirror => CBCSamplerAddressMode.Mirror,
                SamplerAddressMode.Clamp => CBCSamplerAddressMode.Clamp,
                SamplerAddressMode.Border => CBCSamplerAddressMode.Border,
                _ => throw new NotImplementedException(),
            };
        }

        private static void WriteBytecodeOffsetBlock(BinaryWriter bw, ref readonly ShaderProcesserResult result)
        {
            int currentOffset = (int)(bw.BaseStream.Position + Unsafe.SizeOf<int>() * 2 * result.Bytecodes.Length);

            result.Bytecodes.Sort((x, y) => x.TargetData.CompareTo(y.TargetData));
            foreach (ShaderBytecode bytecode in result.Bytecodes)
            {
                bw.Write(currentOffset);
                bw.Write(bytecode.Bytes.Length);

                currentOffset += bytecode.Bytes.Length;
            }
        }

        private static void WriteBytecode(BinaryWriter bw, ref readonly ShaderProcesserResult result)
        {
            foreach (ShaderBytecode bytecode in result.Bytecodes)
            {
                bw.Write(bytecode.Bytes);
            }
        }

        private static Dictionary<ReferenceIndex, ShPropertyStages> CreateStageUsageDictionary(ShaderData data)
        {
            Dictionary<ReferenceIndex, ShPropertyStages> dict = new Dictionary<ReferenceIndex, ShPropertyStages>();

            foreach (FunctionData function in data.Functions)
            {
                if (Array.Exists(function.Attributes, (x) => x.Signature is AttributeKernel))
                {
                    TravelForStageRecursive(ShPropertyStages.ComputeShading, function.IncludeData);
                }
            }

            void TravelForStageRecursive(ShPropertyStages stage, FunctionIncludeData includes)
            {
                foreach (ref readonly ReferenceIndex index in includes.Indices)
                {
                    switch (index.Type)
                    {
                        case ReferenceType.Function: TravelForStageRecursive(stage, data.Functions[index.Index].IncludeData); break;
                        default:
                            {
                                ref ShPropertyStages stages = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, index, out bool exists);
                                if (exists)
                                    stages |= stage;
                                else
                                    stages = stage;

                                break;
                            }
                    }
                }
            }

            return dict;
        }
    }
}
