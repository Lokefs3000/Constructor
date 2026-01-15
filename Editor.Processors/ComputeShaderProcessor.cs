using CommunityToolkit.HighPerformance;
using Editor.Shaders;
using Editor.Shaders.Attributes;
using Editor.Shaders.Data;
using Primary.Assets;
using Primary.Assets.Loaders;
using Primary.Common;
using Primary.RHI2;
using Serilog;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.Processors
{
    public sealed class ComputeShaderProcessor
    {
        public ShaderProcesserResult? Execute(in ComputeShaderProcessorArgs args)
        {
            string? sourceIn = FileUtility.TryReadAllText(args.SourceFilepath);
            if (sourceIn == null)
            {
                args.Logger.Error("Failed to read source from file: {f}", args.SourceFilepath);
                return null;
            }

            using Stream? stream = FileUtility.TryWaitOpenNoThrow(args.OutputFilepath, FileMode.Create, FileAccess.Write, FileShare.None);
            if (stream == null)
            {
                args.Logger.Error("Failed to open stream for output path: {f}", args.OutputFilepath);
                return null;
            }

            Editor.Shaders.ShaderProcessor processor = new Editor.Shaders.ShaderProcessor(args.Logger, ShaderAttributeSettings.Compute);
            ShaderProcesserResult? resultNullable = processor.ProcessCompute(new Editor.Shaders.ComputeShaderProcessorArgs
            {
                InputSource = sourceIn,
                SourceFileName = Path.GetFileName(args.SourceFilepath),

                IncludeDirectories = args.IncludeDirectories,

                Targets = args.Targets
            });

            if (!resultNullable.HasValue)
            {
                args.Logger.Error("Failed to process shader: {f}", args.SourceFilepath);
                return null;
            }

            ShaderProcesserResult result = resultNullable.Value;

            using BinaryWriter bw = new BinaryWriter(stream);

            WriteHeader(bw, ref result, result.Data);

            for (int i = 0; i < result.Data.KernelCount; i++)
            {
                HashSet<ReferenceIndex> resourceUsageSet = CreateResourceUsageSet(result.Data, i);

                WriteKernel(bw, result.Data, i);
                WriteDescription(bw, in args, result.Data);
                WriteResourceList(bw, result.Data, resourceUsageSet);
                WriteRawPropertyList(bw, result.Data, resourceUsageSet);
                WriteStaticSamplers(bw, result.Data);
            }

            WriteBytecodeOffsetBlock(bw, ref result, in args);
            WriteBytecode(bw, ref result);

            return result;
        }

        private void WriteHeader(BinaryWriter bw, ref readonly ShaderProcesserResult result, ShaderData data)
        {
            CBCTarget target = CBCTarget.None;

            if (FlagUtility.HasFlag(result.Targets, ShaderCompileTarget.Direct3D12))
                target |= CBCTarget.Direct3D12;
            if (FlagUtility.HasFlag(result.Targets, ShaderCompileTarget.Vulkan))
                target |= CBCTarget.Vulkan;

            CBCHeader header = new CBCHeader
            {
                Header = CBCHeader.ConstHeader,
                Version = CBCHeader.ConstVersion,

                Targets = target,

                KernelCount = (ushort)data.KernelCount,
            };

            bw.Write(header);
        }

        private void WriteKernel(BinaryWriter bw, ShaderData data, int kernelIndex)
        {
            CBCKernelFlags flags = CBCKernelFlags.None;
            if (!data.GeneratePropertiesInHeader)
                flags |= CBCKernelFlags.ExternalProperties;
            if (data.AreConstantsSeparated)
                flags |= CBCKernelFlags.HeaderIsBuffer;

            AttributeData numThreads = default;
            foreach (ref readonly FunctionData function in data.Functions)
            {
                numThreads = Array.Find(function.Attributes, (x) => x.Signature is AttributeNumThreads);
                if (kernelIndex > 0)
                    --kernelIndex;
                else
                    break;
            }

            if (numThreads.Signature == null)
                throw new NullReferenceException();

            CBCKernel kernel = new CBCKernel
            {
                Flags = flags,
                HeaderSize = (ushort)data.HeaderBytesize,

                ThreadSizeX = (ushort)numThreads.GetVariable<int>("X"),
                ThreadSizeY = (ushort)numThreads.GetVariable<int>("Y"),
                ThreadSizeZ = (ushort)numThreads.GetVariable<int>("Z"),
            };

            bw.Write(kernel);

            foreach (ref readonly FunctionData function in data.Functions)
            {
                if (Array.Exists(function.Attributes, (x) => x.Signature is AttributeKernel))
                {
                    if (kernelIndex > 0)
                        --kernelIndex;
                    else
                    {
                        bw.Write(function.Name);
                        break;
                    }
                }
            }
        }

        private void WriteDescription(BinaryWriter bw, ref readonly ComputeShaderProcessorArgs args, ShaderData data)
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

        private void WriteResourceList(BinaryWriter bw, ShaderData data, HashSet<ReferenceIndex> resourceUsageSet)
        {
            int count = 0;

            int i = 0;
            foreach (ref readonly ResourceData resource in data.Resources)
            {
                if (resourceUsageSet.Contains(new ReferenceIndex(ReferenceType.Resource, i++)))
                    count++;
            }

            bw.Write((ushort)count);

            i = 0;
            foreach (ref readonly ResourceData resource in data.Resources)
            {
                if (!resourceUsageSet.Contains(new ReferenceIndex(ReferenceType.Resource, i++)))
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

        private void WriteRawPropertyList(BinaryWriter bw, ShaderData data, HashSet<ReferenceIndex> resourceUsageSet)
        {
            int i = 0;

            List<ValueTuple<VariableData, bool>> variables = new List<ValueTuple<VariableData, bool>>();
            foreach (ref readonly PropertyData resource in data.Properties)
            {
                if (!resourceUsageSet.Contains(new ReferenceIndex(ReferenceType.Property, i++)))
                    continue;

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
                bw.Write((ushort)(FlagUtility.HasFlag(flags, CBCPropertyFlags.Global) ? globalByteOffset : localByteOffset));
                bw.Write(flags);
                bw.BaseStream.Seek(current, SeekOrigin.Begin);

                if (FlagUtility.HasFlag(flags, CBCPropertyFlags.Global))
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

        private void WriteStaticSamplers(BinaryWriter bw, ShaderData data)
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

        private void WriteBytecodeOffsetBlock(BinaryWriter bw, ref readonly ShaderProcesserResult result, ref readonly ComputeShaderProcessorArgs args)
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

        private void WriteBytecode(BinaryWriter bw, ref readonly ShaderProcesserResult result)
        {
            foreach (ShaderBytecode bytecode in result.Bytecodes)
            {
                bw.Write(bytecode.Bytes);
            }
        }

        private static HashSet<ReferenceIndex> CreateResourceUsageSet(ShaderData data, int kernelSkipIndex)
        {
            HashSet<ReferenceIndex> set = new HashSet<ReferenceIndex>();

            foreach (FunctionData function in data.Functions)
            {
                if (Array.Exists(function.Attributes, (x) => x.Signature is AttributeKernel))
                {
                    if (kernelSkipIndex > 0)
                        --kernelSkipIndex;
                    else
                        TravelForFunctionRecursive(function.IncludeData);
                }
            }

            void TravelForFunctionRecursive(FunctionIncludeData includes)
            {
                foreach (ref readonly ReferenceIndex index in includes.Indices)
                {
                    switch (index.Type)
                    {
                        case ReferenceType.Function: TravelForFunctionRecursive(data.Functions[index.Index].IncludeData); break;
                        default: set.Add(index); break;
                    }
                }
            }

            return set;
        }

        private readonly record struct ShaderKernelData(string KernelName);
    }

    public struct ComputeShaderProcessorArgs
    {
        public string SourceFilepath;
        public string OutputFilepath;

        public string[] IncludeDirectories;

        public ILogger Logger;

        public ShaderCompileTarget Targets;
    }
}
