using CommunityToolkit.HighPerformance;
using Editor.Interop.Ed;
using Editor.Processors.Shaders;
using Primary.Common;
using Serilog;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Direct3D12.Shader;
using Vortice.Dxc;

using RHI = Primary.RHI;

namespace Editor.Processors
{
    public unsafe class ShaderProcessor : IAssetProcessor
    {
        public string[] ReadFiles = Array.Empty<string>();
        public string? ShaderPath = null;

        public bool Execute(object args_in)
        {
            ShaderProcessorArgs args = (ShaderProcessorArgs)args_in;

            string sourceFile;
            using (FileStream stream = NullableUtility.AlwaysThrowIfNull(FileUtility.TryWaitOpen(args.AbsoluteFilepath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                using PoolArray<byte> pool = ArrayPool<byte>.Shared.Rent((int)stream.Length);
                stream.ReadExactly(pool.AsSpan(0, (int)stream.Length));

                sourceFile = Encoding.UTF8.GetString(pool.AsSpan(0, (int)stream.Length));
            }

            string sourceSearchDir = Path.GetDirectoryName(args.AbsoluteFilepath)!;

            string[] paths = ["", sourceSearchDir, args.ContentSearchDir];
            using IDxcCompiler3 compiler = Dxc.CreateDxcCompiler<IDxcCompiler3>();

            //ShaderPreprocessorResult preprocessorResult = ShaderPreprocessor.Inspect(args.Logger, Path.GetFileName(args.AbsoluteFilepath), sourceFile, paths);

            string processedSource;
            using (ShaderIncludeHandler includeHandler = new ShaderIncludeHandler(paths))
            using (IDxcResult result = compiler.Compile(sourceFile, ["-HV", "2021", "-WX", "-P"], includeHandler))
            {
                string errors = result.GetErrors();
                if (errors.Length > 0)
                {
                    args.Logger?.Error(errors + $" {Environment.StackTrace}");
                    return false;
                }

                using IDxcBlob blob = result.GetResult();
                processedSource = Marshal.PtrToStringUTF8(blob.BufferPointer)!;

                ReadFiles = includeHandler.ReadFiles;
            }

            ShaderParseResult parseResult = new ShaderSourceParser().ParseSource(processedSource, sourceSearchDir, args.ContentSearchDir);

            ShaderPath = parseResult.Path;

            ExceptionUtility.Assert(parseResult.EntryPointVertex != null);
            ExceptionUtility.Assert(parseResult.EntryPointPixel != null);
            ExceptionUtility.Assert(parseResult.Path != null);

            string[] templateArgs = Array.Empty<string>();
            ShaderAPITargets apiTarget = ShaderAPITargets.None;

            switch (args.Target)
            {
                case RHI.GraphicsAPI.Vulkan:
                    {
                        templateArgs = [
                "-spirv",
                "-HV", "2021",
                "-WX",
#if DEBUG
                "-Zi",
                "-fspv-debug=vulkan-with-source",
#endif
                "-O3",
                "-fspv-target-env=vulkan1.3",
                "-fvk-use-dx-layout",
                "-fvk-use-dx-position-w"
            ];

                        apiTarget = ShaderAPITargets.Vulkan;
                        break;
                    }
                case RHI.GraphicsAPI.Direct3D12:
                    {
                        templateArgs = [
                "-HV", "2021",
                "-WX",
#if DEBUG
                "-Zi",
#endif
                "-O3",
            ];

                        apiTarget = ShaderAPITargets.Direct3D12;
                        break;
                    }
            }

            Memory<byte> vs = CompileSource(compiler, parseResult, paths, parseResult.EntryPointVertex!, "vs_6_6", templateArgs, args.Logger);
            if (vs.IsEmpty) return false;

            Memory<byte> ps = CompileSource(compiler, parseResult, paths, parseResult.EntryPointPixel!, "ps_6_6", templateArgs, args.Logger);
            if (ps.IsEmpty) return false;

            using (FileStream stream = FileUtility.TryWaitOpen(args.AbsoluteOutputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                bw.Write(HeaderId);
                bw.Write(HeaderVersion);

                bw.Write(parseResult.Path!);

                WritePipelineDesc(bw, ref args, parseResult);
                WriteSupplementData(bw, parseResult, compiler, paths, args.Logger);

                bw.Write(apiTarget);
                bw.Write(ShaderBytecodeTargets.Vertex | ShaderBytecodeTargets.Pixel);

                bw.Write(apiTarget);

                bw.Write((uint)vs.Length);
                bw.Write(vs.Span);

                bw.Write((uint)ps.Length);
                bw.Write(ps.Span);
            }

            return true;
        }

        private static Memory<byte> CompileSource(IDxcCompiler3 compiler, ShaderParseResult parseResult, string[] includePaths, string entryPoint, string profile, string[] arguments, ILogger logger)
        {
            using (ShaderIncludeHandler includeHandler = new ShaderIncludeHandler(includePaths))
            {
                string[] args = arguments.Concat([
                "-E", entryPoint,
                "-T", profile
            ]).ToArray();

                using IDxcResult result = compiler.Compile(parseResult.OutputSource, args, includeHandler);

                string errors = result.GetErrors();
                if (errors != string.Empty)
                {
                    logger.Error(errors);
                    return Memory<byte>.Empty;
                }

                using IDxcBlob bytecodeBlob = result.GetResult();
                Memory<byte> bytecode = bytecodeBlob.AsBytes();

                if (args.Contains("-spirv"))
                {
                    (nint data, int length) = OptimizeSpirv(MemoryMarshal.Cast<byte, uint>(bytecode.Span));
                    if (data == nint.Zero)
                    {
                        //bad
                    }

                    byte[] resultBuffer = new byte[length];
                    unsafe
                    {
                        fixed (byte* ptr = resultBuffer)
                        {
                            Unsafe.CopyBlockUnaligned(ptr, data.ToPointer(), (uint)length);
                        }

                        NativeMemory.Free(data.ToPointer());
                    }

                    return resultBuffer.AsMemory();
                }
                else
                {
                    return bytecode;
                }
            }
        }

        private static void WritePipelineDesc(BinaryWriter bw, ref ShaderProcessorArgs args, ShaderParseResult result)
        {
            ref ShaderDescriptionArgs desc = ref args.Description;

            bw.Write((byte)desc.FillMode);
            bw.Write((byte)desc.CullMode);
            bw.Write(desc.FrontCounterClockwise);
            bw.Write(desc.DepthBias);
            bw.Write(desc.DepthBiasClamp);
            bw.Write(desc.SlopeScaledDepthBias);
            bw.Write(desc.DepthClipEnable);
            bw.Write(desc.ConservativeRaster);
            bw.Write(desc.DepthEnable);
            bw.Write((byte)desc.DepthWriteMask);
            bw.Write((byte)desc.DepthFunc);
            bw.Write(desc.StencilEnable);
            bw.Write(desc.StencilReadMask);
            bw.Write(desc.StencilWriteMask);
            bw.Write((byte)desc.PrimitiveTopology);
            WriteStencilFace(bw, ref desc.FrontFace);
            WriteStencilFace(bw, ref desc.BackFace);
            bw.Write(desc.AlphaToCoverageEnable);
            bw.Write(desc.IndependentBlendEnable);
            bw.Write(desc.LogicOpEnable);
            bw.Write((byte)desc.LogicOp);
            bw.Write((byte)(result.ConstantsSize / 4));
            bw.Write((byte)args.Blends.Length);
            for (int i = 0; i < args.Blends.Length; i++)
            {
                ref BlendDescriptionArgs blend = ref args.Blends[i];
                bw.Write(blend.BlendEnable);
                bw.Write((byte)blend.SourceBlend);
                bw.Write((byte)blend.DestinationBlend);
                bw.Write((byte)blend.BlendOp);
                bw.Write((byte)blend.SourceBlendAlpha);
                bw.Write((byte)blend.DestinationBlendAlpha);
                bw.Write((byte)blend.BlendOpAlpha);
                bw.Write(blend.RenderTargetWriteMask);
            }
        }

        private static void WriteStencilFace(BinaryWriter bw, ref StencilFaceDescriptionArgs face)
        {
            bw.Write((byte)face.FailOp);
            bw.Write((byte)face.DepthFailOp);
            bw.Write((byte)face.PassOp);
            bw.Write((byte)face.Func);
        }

        private static bool WriteSupplementData(BinaryWriter bw, ShaderParseResult parseResult, IDxcCompiler3 compiler, string[] includePaths, ILogger logger)
        {
            using (ShaderIncludeHandler includeHandler = new ShaderIncludeHandler(includePaths))
            {
                using IDxcUtils utils = Dxc.CreateDxcUtils();
                using IDxcResult result = compiler.Compile(parseResult.OutputSource, [
                    "-HV", "2021",
                "-WX",
                "-T", "vs_6_6",
                "-E", parseResult.EntryPointVertex!,
                ], includeHandler);

                string errs = result.GetErrors();
                if (errs.Length > 0)
                {
                    logger.Error(errs);
                    return false;
                }

                using IDxcBlob reflectionBlob = result.GetOutput(DxcOutKind.Reflection);
                using ID3D12ShaderReflection reflection = utils.CreateReflection<ID3D12ShaderReflection>(reflectionBlob);

                ShaderParameterDescription[] inputParams = reflection.InputParameters;
                int currentByteOffset = 0;

                bw.Write((byte)inputParams.Count((x) => x.SystemValueType == Vortice.Direct3D.SystemValueType.Undefined));
                for (int i = 0; i < inputParams.Length; i++)
                {
                    ShaderParameterDescription description = inputParams[i];
                    if (description.SystemValueType != Vortice.Direct3D.SystemValueType.Undefined)
                        continue;

                    ShaderInputLayout layoutMod = parseResult.InputLayout.Find((x) => x.Name == description.SemanticName);

                    RHI.InputElementDescription elem = new RHI.InputElementDescription();

                    elem.Semantic = (RHI.InputElementSemantic)((int)Enum.Parse<RHI.InputElementSemantic>(description.SemanticName, true) + description.SemanticIndex);

                    elem.Format = layoutMod.Format.GetValueOrDefault();
                    if (!layoutMod.Format.HasValue)
                    {
                        int usageMask = BitOperations.PopCount((uint)description.UsageMask) - 1;
                        if (usageMask == -1)
                            elem.Format = RHI.InputElementFormat.Padding;
                        else
                        {
                            switch (description.ComponentType)
                            {
                                case Vortice.Direct3D.RegisterComponentType.UInt32: elem.Format = (RHI.InputElementFormat)((int)RHI.InputElementFormat.UInt1 + usageMask); break;
                                case Vortice.Direct3D.RegisterComponentType.Float32: elem.Format = (RHI.InputElementFormat)((int)RHI.InputElementFormat.Float1 + usageMask); break;
                                default:
                                    //bad
                                    break;
                            }
                        }
                    }

                    int byteOffset = layoutMod.Offset.GetValueOrDefault(currentByteOffset);

                    elem.InputSlot = layoutMod.Slot.GetValueOrDefault(0);
                    elem.ByteOffset = byteOffset;

                    elem.InputSlotClass = layoutMod.Class.GetValueOrDefault(RHI.InputClassification.Vertex);
                    elem.InstanceDataStepRate = 0;

                    bw.Write(elem);

                    switch (elem.Format)
                    {
                        case RHI.InputElementFormat.Padding: currentByteOffset = Math.Max(currentByteOffset, byteOffset); break;
                        case RHI.InputElementFormat.Float1: currentByteOffset += sizeof(float); break;
                        case RHI.InputElementFormat.Float2: currentByteOffset += sizeof(float) * 2; break;
                        case RHI.InputElementFormat.Float3: currentByteOffset += sizeof(float) * 3; break;
                        case RHI.InputElementFormat.Float4: currentByteOffset += sizeof(float) * 4; break;
                        case RHI.InputElementFormat.UInt1: currentByteOffset += sizeof(uint); break;
                        case RHI.InputElementFormat.UInt2: currentByteOffset += sizeof(uint) * 2; break;
                        case RHI.InputElementFormat.UInt3: currentByteOffset += sizeof(uint) * 3; break;
                        case RHI.InputElementFormat.UInt4: currentByteOffset += sizeof(uint) * 4; break;
                        case RHI.InputElementFormat.Byte4: currentByteOffset += sizeof(byte) * 4; break;
                    }
                }

                bw.Write((byte)parseResult.Variables.Count((x) => !x.IsConstants));
                for (int i = 0; i < parseResult.Variables.Count; i++)
                {
                    ShaderVariable variable = parseResult.Variables[i];
                    if (variable.IsConstants)
                        continue;

                    RHI.BoundResourceDescription description = new RHI.BoundResourceDescription
                    {
                        Index = variable.Index,
                        Type = RHI.ResourceType.Texture
                    };

                    switch (variable.Type)
                    {
                        case ShaderVariableType.ConstantBuffer: description.Type = RHI.ResourceType.ConstantBuffer; break;
                        case ShaderVariableType.StructuredBuffer: description.Type = RHI.ResourceType.ShaderBuffer; break;
                        case ShaderVariableType.RWStructuredBuffer: description.Type = RHI.ResourceType.ShaderBuffer; break;
                        case ShaderVariableType.Texture1D: description.Type = RHI.ResourceType.Texture; break;
                        case ShaderVariableType.Texture1DArray: description.Type = RHI.ResourceType.Texture; break;
                        case ShaderVariableType.Texture2D: description.Type = RHI.ResourceType.Texture; break;
                        case ShaderVariableType.Texture2DArray: description.Type = RHI.ResourceType.Texture; break;
                        case ShaderVariableType.Texture3D: description.Type = RHI.ResourceType.Texture; break;
                        case ShaderVariableType.TextureCube: description.Type = RHI.ResourceType.Texture; break;
                    }

                    bw.Write(description);
                }

                bw.Write((byte)parseResult.ImmutableSamplers.Count);
                for (int i = 0; i < parseResult.ImmutableSamplers.Count; i++)
                {
                    ImmutableSampler sampler = parseResult.ImmutableSamplers[i];

                    bw.Write(sampler.Index);
                    bw.Write(sampler.Name);

                    bw.Write(new RHI.ImmutableSamplerDescription
                    {
                        Filter = sampler.Filter,
                        AddressModeU = sampler.AddressModeU,
                        AddressModeV = sampler.AddressModeV,
                        AddressModeW = sampler.AddressModeW,
                        MaxAnistropy = sampler.MaxAnistropy,
                        MipLODBias = sampler.MipLODBias,
                        MinLOD = sampler.MinLOD,
                        MaxLOD = sampler.MaxLOD,
                    });
                }

                bw.Write((byte)parseResult.Variables.Count((x) => !x.IsConstants));
                int k = 0;
                for (int i = 0; i < parseResult.Variables.Count; i++)
                {
                    ShaderVariable variable = parseResult.Variables[i];
                    if (variable.IsConstants)
                        continue;

                    bw.Write(variable.Type);
                    bw.Write(variable.Name);
                    bw.Write(variable.BindGroup);
                    bw.Write((byte)k++);

                    bw.Write((byte)variable.Attributes.Length);

                    for (int j = 0; j < variable.Attributes.Length; j++)
                    {
                        ref ShaderAttribute attrib = ref variable.Attributes[j];
                        bw.Write((byte)attrib.Type);

                        switch (attrib.Type)
                        {
                            case ShaderAttributeType.Constants: break;
                            case ShaderAttributeType.Property:
                                {
                                    ShaderAttribProperty value = (ShaderAttribProperty)attrib.Value!;
                                    bw.Write(value.Name);
                                    bw.Write((byte)value.Default);

                                    break;
                                }
                        }
                    }
                }
            }

            return true;
        }

        private static (nint data, int length) OptimizeSpirv(Span<uint> binary)
        {
            nint optimizer = EdInterop.SPIRV_CreateOptimize();
            try
            {
                SPIRV_OptimizeOut @out = new()
                {
                    Alloc = &SpirvAlloc,
                    InBinary = (uint*)Unsafe.AsPointer(ref binary.DangerousGetReferenceAt(0)),
                    InSize = (ulong)binary.Length,
                };

                EdInterop.SPIRV_OptRegisterPerfPasses(optimizer);
                if (EdInterop.SPIRV_RunOptimize(optimizer, &@out))
                {
                    return ((nint)@out.OutBinary, (int)@out.OutSize);
                }

                return (nint.Zero, 0);
            }
            finally
            {
                EdInterop.SPIRV_DestroyOptimize(optimizer);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void* SpirvAlloc(ulong size) => NativeMemory.Alloc((nuint)size);

        public const uint HeaderId = 0x204c4243;
        public const uint HeaderVersion = 0;

        private class PipelineDesc
        {
            public RHI.FillMode FillMode { get; set; }
            public RHI.CullMode CullMode { get; set; }
            public bool FrontCounterClockwise { get; set; }
            public int DepthBias { get; set; }
            public float DepthBiasClamp { get; set; }
            public float SlopeScaledDepthBias { get; set; }
            public bool DepthClipEnable { get; set; }
            public bool ConservativeRaster { get; set; }
            public bool DepthEnable { get; set; }
            public RHI.DepthWriteMask DepthWriteMask { get; set; }
            public RHI.ComparisonFunc DepthFunc { get; set; }
            public bool StencilEnable { get; set; }
            public byte StencilReadMask { get; set; }
            public byte StencilWriteMask { get; set; }
            public RHI.PrimitiveTopologyType PrimitiveTopology { get; set; }
            public StencilFace FrontFace { get; set; }
            public StencilFace BackFace { get; set; }
            public bool AlphaToCoverageEnable { get; set; }
            public bool IndependentBlendEnable { get; set; }
            public bool LogicOpEnable { get; set; }
            public RHI.LogicOp LogicOp { get; set; }
            public List<BlendDescription> Blends { get; set; }
        }

        private record struct StencilFace
        {
            public RHI.StencilOp StencilFailOp { get; set; }
            public RHI.StencilOp StencilDepthFailOp { get; set; }
            public RHI.StencilOp StencilPassOp { get; set; }
            public RHI.ComparisonFunc StencilFunc { get; set; }
        }

        private record class BlendDescription
        {
            public bool BlendEnable { get; set; }
            public RHI.Blend SrcBlend { get; set; }
            public RHI.Blend DstBlend { get; set; }
            public RHI.BlendOp BlendOp { get; set; }
            public RHI.Blend SrcBlendAlpha { get; set; }
            public RHI.Blend DstBlendAlpha { get; set; }
            public RHI.BlendOp BlendOpAlpha { get; set; }
            public byte RenderTargetWriteMask { get; set; }
        }

        private record struct ShaderResource
        {
            public ShaderVariableType Type;
            public string Name;
            public byte Index;
        }
    }

    public struct ShaderProcessorArgs
    {
        public string AbsoluteFilepath;
        public string AbsoluteOutputPath;

        public string ContentSearchDir;

        public ILogger? Logger;

        public RHI.GraphicsAPI Target;

        public ShaderDescriptionArgs Description;
        public BlendDescriptionArgs[] Blends;
    }

    public struct ShaderDescriptionArgs
    {
        public RHI.FillMode FillMode;
        public RHI.CullMode CullMode;
        public bool FrontCounterClockwise;
        public int DepthBias;
        public float DepthBiasClamp;
        public float SlopeScaledDepthBias;
        public bool DepthClipEnable;
        public bool ConservativeRaster;
        public bool DepthEnable;
        public RHI.DepthWriteMask DepthWriteMask;
        public RHI.ComparisonFunc DepthFunc;
        public bool StencilEnable;
        public byte StencilReadMask;
        public byte StencilWriteMask;
        public RHI.PrimitiveTopologyType PrimitiveTopology;
        public bool AlphaToCoverageEnable;
        public bool IndependentBlendEnable;
        public bool LogicOpEnable;
        public RHI.LogicOp LogicOp;
        public StencilFaceDescriptionArgs FrontFace;
        public StencilFaceDescriptionArgs BackFace;
    }

    public struct StencilFaceDescriptionArgs
    {
        public RHI.StencilOp FailOp;
        public RHI.StencilOp DepthFailOp;
        public RHI.StencilOp PassOp;
        public RHI.ComparisonFunc Func;
    }

    public struct BlendDescriptionArgs
    {
        public bool BlendEnable;
        public RHI.Blend SourceBlend;
        public RHI.Blend DestinationBlend;
        public RHI.BlendOp BlendOp;
        public RHI.Blend SourceBlendAlpha;
        public RHI.Blend DestinationBlendAlpha;
        public RHI.BlendOp BlendOpAlpha;
        public byte RenderTargetWriteMask;
    }

    internal enum ShaderBytecodeTargets : byte
    {
        None = 0,
        Vertex = 1 << 0,
        Pixel = 1 << 1,
    }

    internal enum ShaderAPITargets : byte
    {
        None = 0,
        Vulkan = 1 << 0,
        Direct3D12 = 1 << 1,
    }
}
