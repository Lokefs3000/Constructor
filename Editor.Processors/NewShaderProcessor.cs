using CommunityToolkit.HighPerformance;
using Editor.Shaders;
using Editor.Shaders.Attributes;
using Editor.Shaders.Data;
using Primary.Rendering2.Assets.Loaders;
using Primary.Common;
using Serilog;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using RHI = Primary.RHI;

namespace Editor.Processors
{
    public sealed class NewShaderProcessor
    {
        public ShaderProcesserResult? Execute(in NewShaderProcessorArgs args)
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

            Editor.Shaders.ShaderProcessor processor = new Editor.Shaders.ShaderProcessor(args.Logger, ShaderAttributeSettings.Default);
            ShaderProcesserResult? resultNullable = processor.Process(new Editor.Shaders.ShaderProcessorArgs
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

            WriteHeader(bw, ref result, processor);
            WriteDescription(bw, in args, processor);
            WriteResourceList(bw, processor);
            WriteRawPropertyList(bw, processor);
            WriteInputLayout(bw, processor);
            WriteStaticSamplers(bw, processor);
            WriteBytecodeOffsetBlock(bw, ref result, in args);
            WriteBytecode(bw, ref result);

            return result;
        }

        private void WriteHeader(BinaryWriter bw, ref readonly ShaderProcesserResult result, Editor.Shaders.ShaderProcessor processor)
        {
            SBCTarget target = SBCTarget.None;

            if (FlagUtility.HasFlag(result.Targets, ShaderCompileTarget.Direct3D12))
                target |= SBCTarget.Direct3D12;
            if (FlagUtility.HasFlag(result.Targets, ShaderCompileTarget.Vulkan))
                target |= SBCTarget.Vulkan;

            SBCStages stages = SBCStages.None;

            if (FlagUtility.HasFlag(result.Stages, ShaderCompileStage.Vertex))
                stages |= SBCStages.Vertex;
            if (FlagUtility.HasFlag(result.Stages, ShaderCompileStage.Pixel))
                stages |= SBCStages.Pixel;

            SBCHeaderFlags flags = SBCHeaderFlags.None;

            if (!processor.GeneratePropertiesInHeader)
                flags |= SBCHeaderFlags.ExternalProperties;
            if (processor.AreConstantsSeparated)
                flags |= SBCHeaderFlags.HeaderIsBuffer;

            SBCHeader header = new SBCHeader
            {
                Header = SBCHeader.ConstHeader,
                Version = SBCHeader.ConstVersion,

                Targets = target,
                Stages = stages,

                Flags = flags,
                HeaderSize = (ushort)processor.HeaderBytesize
            };

            bw.Write(header);
        }

        private void WriteDescription(BinaryWriter bw, ref readonly NewShaderProcessorArgs args, Editor.Shaders.ShaderProcessor processor)
        {
            int expectedConstantsSize = 0;
            int idx = processor.Resources.FindIndex((x) => x.Type == ResourceType.ConstantBuffer && Array.Exists(x.Attributes, (y) => y.Signature is AttributeConstants));
            if (idx != -1)
            {
                ref readonly ResourceData data = ref processor.Resources[idx];
                ref readonly StructData @struct = ref processor.GetRefSource(data.Value);

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
                            ref readonly StructData varStruct = ref processor.GetRefSource(generic);
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

            bw.Write(args.TopologyType);
            bw.Write((byte)expectedConstantsSize);

            bw.Write(args.Rasterizer);
            bw.Write(args.DepthStencil);

            bw.Write(args.Blend);
            bw.Write((byte)args.Blends.Length);

            foreach (ref readonly SBCRenderTargetBlend rtBlend in args.Blends.AsSpan())
            {
                bw.Write(rtBlend);
            }
        }

        private void WriteResourceList(BinaryWriter bw, Editor.Shaders.ShaderProcessor processor)
        {
            bw.Write((ushort)processor.Resources.Length);

            foreach (ref readonly ResourceData resource in processor.Resources)
            {
                bw.Write(resource.Type switch
                {
                    ResourceType.Texture1D => SBCResourceType.Texture1D,
                    ResourceType.Texture2D => SBCResourceType.Texture2D,
                    ResourceType.Texture3D => SBCResourceType.Texture3D,
                    ResourceType.TextureCube => SBCResourceType.TextureCube,
                    ResourceType.ConstantBuffer => SBCResourceType.ConstantBuffer,
                    ResourceType.StructuredBuffer => SBCResourceType.StructuredBuffer,
                    ResourceType.ByteAddressBuffer => SBCResourceType.ByteAddressBuffer,
                    ResourceType.SamplerState => SBCResourceType.SamplerState,
                    _ => throw new NotSupportedException()
                });

                long backup = bw.BaseStream.Position;

                bw.Write(SBCResourceFlags.None);
                bw.Write(resource.Name);

                AttributeData attributeData = new AttributeData();
                SBCResourceFlags flags = SBCResourceFlags.None;

                if ((attributeData = Array.Find(resource.Attributes, static (x) => x.Signature is AttributeConstants)).Signature != null)
                {
                    flags |= SBCResourceFlags.Constants;
                }
                if ((attributeData = Array.Find(resource.Attributes, static (x) => x.Signature is AttributeDisplay)).Signature != null && attributeData.Data != null)
                {
                    flags |= SBCResourceFlags.Display;
                    bw.Write(attributeData.GetVariable<PropertyDisplay>("Display") switch
                    {
                        PropertyDisplay.Default => SBCPropertyDisplay.Default,
                        PropertyDisplay.Color => SBCPropertyDisplay.Color,
                        _ => throw new NotImplementedException()
                    });
                }
                if ((attributeData = Array.Find(resource.Attributes, static (x) => x.Signature is AttributeGlobal)).Signature != null)
                {
                    flags |= SBCResourceFlags.Global;

                    string? customName = attributeData.GetVariable<string>("Name");
                    bw.Write(new SBCAttributeGlobal
                    {
                        HasCustomName = customName != null
                    });

                    if (customName != null)
                        bw.Write(customName);
                }
                if ((attributeData = Array.Find(resource.Attributes, static (x) => x.Signature is AttributeProperty)).Signature != null)
                {
                    flags |= SBCResourceFlags.Property;

                    string? customName = attributeData.GetVariable<string>("Name");
                    bw.Write(new SBCAttributeProperty
                    {
                        Default = attributeData.GetVariable<PropertyDefault>("Default") switch
                        {
                            PropertyDefault.NumOne => SBCPropertyDefault.NumOne,
                            PropertyDefault.NumZero => SBCPropertyDefault.NumZero,
                            PropertyDefault.NumIdentity => SBCPropertyDefault.NumIdentity,
                            PropertyDefault.TexWhite => SBCPropertyDefault.TexWhite,
                            PropertyDefault.TexBlack => SBCPropertyDefault.TexBlack,
                            PropertyDefault.TexMask => SBCPropertyDefault.TexMask,
                            PropertyDefault.TexNormal => SBCPropertyDefault.TexNormal,
                            _ => throw new NotImplementedException()
                        },
                        HasCustomName = customName != null
                    });

                    if (customName != null)
                        bw.Write(customName);
                }
                if ((attributeData = Array.Find(resource.Attributes, static (x) => x.Signature is AttributeSampled)).Signature != null)
                {
                    flags |= SBCResourceFlags.Sampled;

                    string? customName = attributeData.GetVariable<string>("Sampler");
                    bw.Write(new SBCAttributeSampled
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

        private void WriteRawPropertyList(BinaryWriter bw, Editor.Shaders.ShaderProcessor processor)
        {
            List<ValueTuple<VariableData, bool>> variables = new List<ValueTuple<VariableData, bool>>();
            foreach (ref readonly PropertyData resource in processor.Properties)
            {
                ValueDataRef generic = resource.Generic;
                Checking.Assert(generic.IsSpecified);

                variables.Add((new VariableData(resource.Name, resource.Attributes, generic, null), false));

                if (generic.Generic == ValueGeneric.Custom)
                {
                    ref readonly StructData @struct = ref processor.GetRefSource(generic);
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

                int size = processor.CalculateSize(generic);

                if (generic.Generic == ValueGeneric.Custom)
                {
                    bw.Write((ushort)((1 << 15) | size));
                }
                else
                {
                    bw.Write((ushort)((int)(generic.Generic switch
                    {
                        ValueGeneric.Float => SBCValueGeneric.Single,
                        ValueGeneric.Double => SBCValueGeneric.Double,
                        ValueGeneric.Int => SBCValueGeneric.Int,
                        ValueGeneric.UInt => SBCValueGeneric.UInt,
                        _ => throw new NotImplementedException(),
                    }) | (generic.Rows << 12) | (generic.Columns << 9)));
                }

                long backup = bw.BaseStream.Position;

                bw.Write(ushort.MaxValue);
                bw.Write(SBCPropertyFlags.None);

                bw.Write(variable.Name);

                AttributeData attributeData = new AttributeData();
                SBCPropertyFlags flags = SBCPropertyFlags.None;

                if (tuple.Item2)
                    flags |= SBCPropertyFlags.HasParent;

                if ((attributeData = Array.Find(variable.Attributes, static (x) => x.Signature is AttributeDisplay)).Signature != null && attributeData.Data != null)
                {
                    flags |= SBCPropertyFlags.Display;
                    bw.Write(attributeData.GetVariable<PropertyDisplay>("Display") switch
                    {
                        PropertyDisplay.Default => SBCPropertyDisplay.Default,
                        PropertyDisplay.Color => SBCPropertyDisplay.Color,
                        _ => throw new NotImplementedException()
                    });
                }
                if ((attributeData = Array.Find(variable.Attributes, static (x) => x.Signature is AttributeGlobal)).Signature != null)
                {
                    flags |= SBCPropertyFlags.Global;

                    string? customName = attributeData.GetVariable<string>("Name");
                    bw.Write(new SBCAttributeGlobal
                    {
                        HasCustomName = customName != null
                    });

                    if (customName != null)
                        bw.Write(customName);
                }
                if ((attributeData = Array.Find(variable.Attributes, static (x) => x.Signature is AttributeProperty)).Signature != null)
                {
                    flags |= SBCPropertyFlags.Property;

                    string? customName = attributeData.GetVariable<string>("Name");
                    bw.Write(new SBCAttributeProperty
                    {
                        Default = attributeData.GetVariable<PropertyDefault>("Default") switch
                        {
                            PropertyDefault.NumOne => SBCPropertyDefault.NumOne,
                            PropertyDefault.NumZero => SBCPropertyDefault.NumZero,
                            PropertyDefault.NumIdentity => SBCPropertyDefault.NumIdentity,
                            PropertyDefault.TexWhite => SBCPropertyDefault.TexWhite,
                            PropertyDefault.TexBlack => SBCPropertyDefault.TexBlack,
                            PropertyDefault.TexMask => SBCPropertyDefault.TexMask,
                            PropertyDefault.TexNormal => SBCPropertyDefault.TexNormal,
                            _ => throw new NotImplementedException()
                        },
                        HasCustomName = customName != null
                    });

                    if (customName != null)
                        bw.Write(customName);
                }

                long current = bw.BaseStream.Position;

                bw.BaseStream.Seek(backup, SeekOrigin.Begin);
                bw.Write((ushort)(FlagUtility.HasFlag(flags, SBCPropertyFlags.Global) ? globalByteOffset : localByteOffset));
                bw.Write(flags);
                bw.BaseStream.Seek(current, SeekOrigin.Begin);

                if (FlagUtility.HasFlag(flags, SBCPropertyFlags.Global))
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
                        ref readonly StructData childStruct = ref processor.GetRefSource(generic);
                        AppendSubStructMembers(in @struct, variables);
                    }
                }
            }
        }

        private void WriteInputLayout(BinaryWriter bw, Editor.Shaders.ShaderProcessor processor)
        {
            int idx = processor.Functions.FindIndex((x) => Array.Exists(x.Attributes, (y) => y.Signature is AttributeVertex));
            Checking.Assert(idx != -1);

            ref readonly FunctionData vertexEntry = ref processor.Functions[idx];
            int structData = Array.FindIndex(vertexEntry.Arguments, (x) => x.Generic.Generic == ValueGeneric.Custom);

            if (structData != -1)
            {
                ref readonly StructData @struct = ref processor.GetRefSource(vertexEntry.Arguments[structData].Generic);
                if (@struct.Variables.Length > 0)
                {
                    using RentedArray<SBCInputElement> inputElements = RentedArray<SBCInputElement>.Rent(@struct.Variables.Length);
                    int actualValid = 0;

                    int byteOffset = 0;
                    for (int i = 0; i < @struct.Variables.Length; i++)
                    {
                        ref readonly VariableData variable = ref @struct.Variables[i];

                        ValueDataRef generic = variable.Generic;
                        Checking.Assert(generic.IsSpecified && generic.Generic != ValueGeneric.Custom);
                        Checking.Assert(variable.Semantic.HasValue);

                        VarSemantic semantic = variable.Semantic.Value;
                        if (semantic.Semantic >= SemanticName.SV_InstanceId)
                            continue;

                        SBCInputElement stagingElement = new SBCInputElement
                        {
                            Semantic = semantic.Semantic switch
                            {
                                SemanticName.Position => SBCInputSemantic.Position,
                                SemanticName.Texcoord => SBCInputSemantic.Texcoord,
                                SemanticName.Color => SBCInputSemantic.Color,
                                SemanticName.Normal => SBCInputSemantic.Normal,
                                SemanticName.Tangent => SBCInputSemantic.Tangent,
                                //SemanticName.Bitangnet => SBCInputSemantic.Bitangnet,
                                SemanticName.BlendIndices => SBCInputSemantic.BlendIndices,
                                SemanticName.BlendWeight => SBCInputSemantic.BlendWeight,
                                SemanticName.PositionT => SBCInputSemantic.PositionT,
                                SemanticName.PSize => SBCInputSemantic.PSize,
                                SemanticName.Fog => SBCInputSemantic.Fog,
                                SemanticName.TessFactor => SBCInputSemantic.TessFactor,
                                _ => throw new NotImplementedException()
                            },
                            SemanticIndex = (byte)semantic.Index,
                            Format = (SBCInputFormat)((int)(generic.Generic switch
                            {
                                ValueGeneric.Float => SBCInputFormat.Float1,
                                ValueGeneric.UInt => SBCInputFormat.UInt1,
                                _ => throw new NotImplementedException()
                            }) + Math.Max(generic.Rows - 1, 0)),

                            InputSlot = 0,
                            ByteOffset = ushort.MaxValue,

                            InputSlotClass = SBCInputClassification.Vertex
                        };

                        byteOffset += generic.Generic switch
                        {
                            ValueGeneric.Float => sizeof(float),
                            ValueGeneric.UInt => sizeof(uint),
                            _ => throw new NotImplementedException()
                        } * generic.Rows;

                        inputElements[i] = stagingElement;
                        actualValid++;
                    }

                    if (actualValid > 0)
                    {
                        for (int i = 0; i < vertexEntry.Attributes.Length; i++)
                        {
                            AttributeData layoutData = vertexEntry.Attributes[i];
                            if (layoutData.Signature is AttributeIALayout and not null)
                            {
                                string elementName = layoutData.GetVariable<string>("Name")!;
                                idx = @struct.Variables.FindIndex((x) => x.Name == elementName);

                                if (idx == -1)
                                    throw new Exception($"No input layout variable with name: {elementName} found (TODO: Add custom exception)"/*TODO: Add custom exception*/);

                                ref SBCInputElement inputElement = ref inputElements[idx];

                                if (layoutData.TryGetVariable("Offset", out int offset))
                                    inputElement.ByteOffset = (ushort)offset;

                                if (layoutData.TryGetVariable("Slot", out int slot))
                                    inputElement.InputSlot = Math.Min((byte)slot, (byte)8);

                                if (layoutData.TryGetVariable("Class", out RHI.InputClassification inputClass))
                                    inputElement.InputSlotClass = inputClass switch
                                    {
                                        RHI.InputClassification.Vertex => SBCInputClassification.Vertex,
                                        RHI.InputClassification.Instance => SBCInputClassification.Instance,
                                        _ => throw new NotImplementedException()
                                    };

                                if (layoutData.TryGetVariable("Format", out RHI.InputElementFormat format))
                                    inputElement.Format = format switch
                                    {
                                        RHI.InputElementFormat.Float1 => SBCInputFormat.Float1,
                                        RHI.InputElementFormat.Float2 => SBCInputFormat.Float2,
                                        RHI.InputElementFormat.Float3 => SBCInputFormat.Float3,
                                        RHI.InputElementFormat.Float4 => SBCInputFormat.Float4,
                                        RHI.InputElementFormat.UInt1 => SBCInputFormat.UInt1,
                                        RHI.InputElementFormat.UInt2 => SBCInputFormat.UInt2,
                                        RHI.InputElementFormat.UInt3 => SBCInputFormat.UInt3,
                                        RHI.InputElementFormat.UInt4 => SBCInputFormat.UInt4,
                                        RHI.InputElementFormat.Byte4 => SBCInputFormat.Byte4,
                                        _ => throw new NotImplementedException()
                                    };
                            }
                        }

                        bw.Write((byte)actualValid);
                        for (int i = 0; i < @struct.Variables.Length; i++)
                        {
                            ref readonly VariableData variable = ref @struct.Variables[i];

                            VarSemantic semantic = variable.Semantic!.Value;
                            if (semantic.Semantic >= SemanticName.SV_InstanceId)
                                continue;

                            bw.Write(inputElements[i]);
                        }
                    }
                    else
                        bw.Write((byte)0);
                }
                else
                    bw.Write((byte)0);
            }
            else
                bw.Write((byte)0);

        }

        private void WriteStaticSamplers(BinaryWriter bw, Editor.Shaders.ShaderProcessor processor)
        {
            bw.Write((byte)processor.StaticSamplers.Length);
            if (!processor.StaticSamplers.IsEmpty)
            {
                foreach (ref readonly StaticSamplerData samplerData in processor.StaticSamplers)
                {
                    bw.Write(new SBCStaticSampler
                    {
                        Filter = samplerData.Filter switch
                        {
                            SamplerFilter.Point => SBCSamplerFilter.Point,
                            SamplerFilter.MinMagPointMipLinear => SBCSamplerFilter.MinMagPointMipLinear,
                            SamplerFilter.MinPointMagLinearMipPoint => SBCSamplerFilter.MinPointMagLinearMipPoint,
                            SamplerFilter.MinPointMagMipLinear => SBCSamplerFilter.MinPointMagMipLinear,
                            SamplerFilter.MinLinearMagMipPoint => SBCSamplerFilter.MinLinearMagMipPoint,
                            SamplerFilter.MinLinearMagPointMipLinear => SBCSamplerFilter.MinLinearMagPointMipLinear,
                            SamplerFilter.MinMagLinearMipPoint => SBCSamplerFilter.MinMagLinearMipPoint,
                            SamplerFilter.Linear => SBCSamplerFilter.Linear,
                            SamplerFilter.MinMagAnisotropicMipPoint => SBCSamplerFilter.MinMagAnisotropicMipPoint,
                            _ => throw new NotImplementedException()
                        },
                        AddressModeU = TranslabeSAM(samplerData.AddressModeU),
                        AddressModeV = TranslabeSAM(samplerData.AddressModeV),
                        AddressModeW = TranslabeSAM(samplerData.AddressModeW),
                        MaxAnisotropy = (byte)Math.Clamp(samplerData.MaxAnisotropy, 1, 16),
                        MipLODBias = samplerData.MipLODBias,
                        MinLOD = samplerData.MinLOD,
                        MaxLOD = samplerData.MaxLOD,
                        Border = samplerData.Border switch
                        {
                            SamplerBorder.TransparentBlack => SBCSamplerBorder.TransparentBlack,
                            SamplerBorder.OpaqueBlack => SBCSamplerBorder.OpaqueBlack,
                            SamplerBorder.OpaqueWhite => SBCSamplerBorder.OpaqueWhite,
                            SamplerBorder.OpaqueBlackUInt => SBCSamplerBorder.OpaqueBlackUInt,
                            SamplerBorder.OpaqueWhiteUInt => SBCSamplerBorder.OpaqueWhiteUInt,
                            _ => throw new NotImplementedException(),
                        }
                    });
                }
            }

            static SBCSamplerAddressMode TranslabeSAM(SamplerAddressMode addressMode) => addressMode switch
            {
                SamplerAddressMode.Repeat => SBCSamplerAddressMode.Repeat,
                SamplerAddressMode.Mirror => SBCSamplerAddressMode.Mirror,
                SamplerAddressMode.ClampToEdge => SBCSamplerAddressMode.ClampToEdge,
                SamplerAddressMode.ClampToBorder => SBCSamplerAddressMode.ClampToBorder,
                _ => throw new NotImplementedException(),
            };
        }

        private void WriteBytecodeOffsetBlock(BinaryWriter bw, ref readonly ShaderProcesserResult result, ref readonly NewShaderProcessorArgs args)
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
    }

    public struct NewShaderProcessorArgs
    {
        public string SourceFilepath;
        public string OutputFilepath;

        public string[] IncludeDirectories;

        public ILogger Logger;

        public ShaderCompileTarget Targets;

        public SBCPrimitiveTopology TopologyType;
        public SBCRasterizer Rasterizer;
        public SBCDepthStencil DepthStencil;
        public SBCBlend Blend;
        public SBCRenderTargetBlend[] Blends;
    }

    public struct SPRasterizer
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

    public struct SPDepthStencil
    {
        public bool DepthEnable;
        public SBCDepthWriteMask WriteMask;
        public SBCComparisonFunc DepthFunc;
        public bool StencilEnable;
        public byte StencilReadMask;
        public byte StencilWriteMask;
        public SPDepthStencilFace FrontFace;
        public SPDepthStencilFace BackFace;
    }

    public struct SPDepthStencilFace
    {
        public SBCStencilOp Fail;
        public SBCStencilOp DepthFail;
        public SBCStencilOp Pass;
        public SBCComparisonFunc Func;
    }

    public struct SPBlend
    {
        public bool AlphaToCoverageEnable;
        public bool IndependentBlendEnable;
        public SPRenderTargetBlend[] Blends;
    }

    public struct SPRenderTargetBlend
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
}
