using CommunityToolkit.HighPerformance;
using Editor.Shaders.Attributes;
using Editor.Shaders.Data;
using Editor.Shaders.Processors;
using Primary.Common;
using Serilog;
using SharpGen.Runtime;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vortice.Dxc;

namespace Editor.Shaders
{
    public sealed class ShaderProcessor : IDisposable
    {
        private readonly ILogger _logger;

        private readonly ShaderAttributeSettings _settings;
        private readonly IDxcCompiler3 _compiler;

        private bool _disposedValue;

        public ShaderProcessor(ILogger logger, ShaderAttributeSettings settings)
        {
            _logger = logger;

            _settings = settings;
            _compiler = Dxc.CreateDxcCompiler<IDxcCompiler3>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _compiler.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ShaderProcesserResult? Process(ShaderProcessorArgs args)
        {
            string source = args.InputSource;
            string fileName = args.SourceFileName ?? "hlsl.hlsl";

            string[] includedFiles = Array.Empty<string>();

            //1. preprocess source
            {
                using (ShaderIncludeHandler includeHandler = new ShaderIncludeHandler(args.IncludeDirectories))
                using (IDxcResult result = _compiler.Compile(source, s_preprocessPreset, includeHandler))
                {
                    string errors = result.GetErrors();
                    if (errors.Length > 0)
                    {
                        throw new ShaderCompileException(errors); //temp
                    }

                    using IDxcBlob blob = result.GetResult();
                    unsafe
                    {
                        //ANYTHING to avoid using Marshal
                        source = new string((sbyte*)blob.BufferPointer);
                    }

                    includedFiles = includeHandler.GetReadFiles();
                }
            }

            ShaderData data = new ShaderData();

            //2. parse the new source

            SourceParser parser = new SourceParser(this, data, source, fileName);
            bool r = parser.Parse();

            if (!r)
            {
                return null;
            }

            //3. generate final source

            SourceAugmenter generator = new SourceAugmenter(this, data, fileName);
            source = generator.Augment(source, -1)!;

            if (source == null)
            {
                return null;
            }

            data.ProcessedSource = source;

            //4. compile for targets
            ShaderCompileStage stages = CountAllStages(data);
            ShaderBytecode[] bytecodes = new ShaderBytecode[int.PopCount((int)args.Targets) * int.PopCount((int)stages)];

            int offset = 0;
            for (int i = 0; i < 2; i++)
            {
                ShaderCompileTarget target = (ShaderCompileTarget)(1 << i);
                if (FlagUtility.HasFlag(args.Targets, target))
                {
                    string[] presetArgs = target switch
                    {
                        ShaderCompileTarget.Direct3D12 => s_d3d12Preset,
                        ShaderCompileTarget.Vulkan => s_vulkanPreset,
                        _ => throw new NotImplementedException()
                    };

                    if (!CompileForTarget(ref source, data, presetArgs, bytecodes, target, stages, ref offset, 0))
                        return null;
                }
            }

            return new ShaderProcesserResult(MakeBytecodeTarget(args.Targets, stages, 0), bytecodes, includedFiles, data);
        }

        public ShaderProcesserResult? ProcessCompute(ComputeShaderProcessorArgs args)
        {
            string source = args.InputSource;
            string fileName = args.SourceFileName ?? "hlsl.hlsl";

            string[] includedFiles = Array.Empty<string>();

            //1. preprocess source
            {
                using (ShaderIncludeHandler includeHandler = new ShaderIncludeHandler(args.IncludeDirectories))
                using (IDxcResult result = _compiler.Compile(source, s_preprocessPreset, includeHandler))
                {
                    string errors = result.GetErrors();
                    if (errors.Length > 0)
                    {
                        throw new ShaderCompileException(errors); //temp
                    }

                    using IDxcBlob blob = result.GetResult();
                    unsafe
                    {
                        //ANYTHING to avoid using Marshal
                        source = new string((sbyte*)blob.BufferPointer);
                    }

                    includedFiles = includeHandler.GetReadFiles();
                }
            }

            ShaderData data = new ShaderData();

            //2. parse the new source

            SourceParser parser = new SourceParser(this, data, source, fileName);
            bool r = parser.Parse();

            if (!r)
            {
                return null;
            }

            //3. generate and compile for each kernel
            data.KernelCount = CountKernels(data);
            ShaderBytecode[] bytecodes = new ShaderBytecode[int.PopCount((int)args.Targets) * data.KernelCount];

            int offset = 0;
            for (int j = 0; j < data.KernelCount; j++)
            {
                int functionIndex = 0;
                {
                    int remaining = j;

                    foreach (ref readonly FunctionData function in data.Functions)
                    {
                        if (Array.Exists(function.Attributes, (x) => x.Signature is AttributeKernel))
                        {
                            if (remaining > 0)
                                --remaining;
                            else
                                break;
                        }

                        ++functionIndex;
                    }
                }

                //4.1. generate final source

                SourceAugmenter generator = new SourceAugmenter(this, data, fileName);
                source = generator.Augment(source, functionIndex)!;

                if (source == null)
                {
                    return null;
                }

                data.ProcessedSource = source;

                //4.2. compile for targets

                for (int i = 0; i < 2; i++)
                {
                    ShaderCompileTarget target = (ShaderCompileTarget)(1 << i);
                    if (FlagUtility.HasFlag(args.Targets, target))
                    {
                        string[] presetArgs = target switch
                        {
                            ShaderCompileTarget.Direct3D12 => s_d3d12Preset,
                            ShaderCompileTarget.Vulkan => s_vulkanPreset,
                            _ => throw new NotImplementedException()
                        };

                        if (!CompileForTarget(ref source, data, presetArgs, bytecodes, target, ShaderCompileStage.Compute, ref offset, j))
                            return null;
                    }
                }
            }

            return new ShaderProcesserResult(MakeBytecodeTarget(args.Targets, ShaderCompileStage.Compute, 0), bytecodes, includedFiles, data);
        }

        private bool CompileForTarget(ref string source, ShaderData data, string[] initialArgs, ShaderBytecode[] bytecodes, ShaderCompileTarget target, ShaderCompileStage stages, ref int offset, int kernelSkipIndex)
        {
            for (int i = 0; i < 3; i++)
            {
                ShaderCompileStage stage = (ShaderCompileStage)(1 << i);
                if (FlagUtility.HasFlag(stages, stage))
                {
                    int startIndex = stage == ShaderCompileStage.Compute ? kernelSkipIndex : 0;

                    int index = 0;
                    {
                        int startIndexCounter = startIndex;
                        foreach (ref readonly FunctionData function in data.Functions)
                        {
                            if (stage switch
                            {
                                ShaderCompileStage.Vertex => Array.Exists(function.Attributes, (x) => x.Signature is AttributeVertex),
                                ShaderCompileStage.Pixel => Array.Exists(function.Attributes, (x) => x.Signature is AttributePixel),
                                ShaderCompileStage.Compute => Array.Exists(function.Attributes, (x) => x.Signature is AttributeKernel),
                                _ => throw new NotImplementedException()
                            })
                            {
                                if (startIndexCounter > 0)
                                    --startIndexCounter;
                                else
                                    break;
                            }

                            ++index;
                        }
                    }

                    string entryPoint = data.Functions[index].Name;

                    string[] newArgs = initialArgs.Concat([

                        "-E", entryPoint,
                        "-T", stage switch {
                            ShaderCompileStage.Vertex => "vs_6_6",
                            ShaderCompileStage.Pixel => "ps_6_6",
                            ShaderCompileStage.Compute => "cs_6_6",
                            _ => throw new NotImplementedException()
                        }

                        ]).ToArray();

                    using IDxcResult result = _compiler.Compile(source, newArgs, null!/*HACK: This sucks but cant do much since nullable is enabled*/);

                    string errors = result.GetErrors();
                    if (errors != string.Empty)
                    {
                        Logger.Error("Encountered error compiling for stage (target: {t}, stage: {s})\n{err}", target, stage, errors);
                        return false;
                    }

                    using IDxcBlob bytecodeBlob = result.GetResult();
                    if (target == ShaderCompileTarget.Vulkan)
                    {
                        throw new NotImplementedException("TODO: Vulkan bytecode is not supported fully yet");
                    }
                    else
                    {
                        bytecodes[offset++] = new ShaderBytecode(MakeBytecodeTarget(target, stage, startIndex), bytecodeBlob.AsBytes());
                    }
                }
            }

            return true;
        }

        private ShaderCompileStage CountAllStages(ShaderData data)
        {
            ShaderCompileStage stages = ShaderCompileStage.None;
            foreach (ref readonly FunctionData function in data.Functions)
            {
                foreach (ref readonly AttributeData attribute in function.Attributes.AsSpan())
                {
                    if (attribute.Signature is AttributeVertex)
                        stages |= ShaderCompileStage.Vertex;
                    else if (attribute.Signature is AttributePixel)
                        stages |= ShaderCompileStage.Pixel;
                }
            }

            return stages;
        }

        private int CountKernels(ShaderData data)
        {
            int count = 0;
            foreach (ref readonly FunctionData function in data.Functions)
            {
                if (Array.Exists(function.Attributes, (x) => x.Signature is AttributeKernel))
                    count++;
            }

            return count;
        }

        internal AttributeSignature? FindAttributeSignature(ReadOnlySpan<char> name)
        {
            _settings.TryGetSignature(name.ToString(), out AttributeSignature? signature);
            return signature;
        }

        internal ILogger Logger => _logger;

        private static ushort MakeBytecodeTarget(ShaderCompileTarget target, ShaderCompileStage stage, int index) => (ushort)(((ushort)target) | (((ushort)stage) << 8) | (((ushort)index) << 3));

        private static string[] s_preprocessPreset = [
            "-HV", "2021",
            "-WX",
            "-P"
            ];

        private static string[] s_vulkanPreset = [
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

        private static string[] s_d3d12Preset = [
            "-HV", "2021",
            "-WX",
#if DEBUG
            "-Zi",
#endif
            "-O3"
        ];

        public const int MaxConstantsBufferSize = 128; //Vulkan enforced limit
    }
}
