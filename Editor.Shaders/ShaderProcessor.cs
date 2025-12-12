using CommunityToolkit.HighPerformance;
using Editor.Shaders.Attributes;
using Editor.Shaders.Data;
using Editor.Shaders.Processors;
using Primary.Common;
using Serilog;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.Dxc;

namespace Editor.Shaders
{
    public sealed class ShaderProcessor : IDisposable
    {
        private readonly ILogger _logger;

        private readonly ShaderAttributeSettings _settings;
        private readonly IDxcCompiler3 _compiler;

        private Dictionary<int, int> _valueRefTable;

        private List<FunctionData> _functions;
        private List<ResourceData> _resources;
        private List<PropertyData> _properties;
        private List<StructData> _structs;
        private List<StaticSamplerData> _staticSamplers;

        private string? _propertySourceTemplate;
        private string? _processedSource;

        private bool _generatePropertiesInHeader;

        private bool _disposedValue;

        public ShaderProcessor(ILogger logger, ShaderAttributeSettings settings)
        {
            _logger = logger;

            _settings = settings;
            _compiler = Dxc.CreateDxcCompiler<IDxcCompiler3>();

            _valueRefTable = new Dictionary<int, int>();

            _functions = new List<FunctionData>();
            _resources = new List<ResourceData>();
            _properties = new List<PropertyData>();
            _structs = new List<StructData>();
            _staticSamplers = new List<StaticSamplerData>();

            _propertySourceTemplate = "__HEADER_CB.RAW_$PNAME$";
            _processedSource = null;

            _generatePropertiesInHeader = true;
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

            //2. parse the new source

            SourceParser parser = new SourceParser(this, source, fileName);
            bool r = parser.Parse();

            if (!r)
            {
                return null;
            }

            //3. generate final source
            
            SourceAugmenter generator = new SourceAugmenter(this, fileName);
            source = generator.Augment(source)!;

            if (source == null)
            {
                return null;
            }

            _processedSource = source;

            //4. compile for targets
            ShaderCompileStage stages = CountAllStages();
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

                    if (!CompileForTarget(ref source, presetArgs, bytecodes, target, stages, ref offset))
                        return null;
                }
            }

            return new ShaderProcesserResult(MakeBytecodeTarget(args.Targets, stages), bytecodes, includedFiles);
        }

        private bool CompileForTarget(ref string source, string[] initialArgs, ShaderBytecode[] bytecodes, ShaderCompileTarget target, ShaderCompileStage stages, ref int offset)
        {
            for (int i = 0; i < 2; i++)
            {
                ShaderCompileStage stage = (ShaderCompileStage)(1 << i);
                if (FlagUtility.HasFlag(stages, stage))
                {
                    string entryPoint = _functions.Find((x) => stage switch
                    {
                        ShaderCompileStage.Vertex => Array.Exists(x.Attributes, (x) => x.Signature is AttributeVertex),
                        ShaderCompileStage.Pixel => Array.Exists(x.Attributes, (x) => x.Signature is AttributePixel),
                        _ => throw new NotImplementedException()
                    }).Name;

                    string[] newArgs = initialArgs.Concat([

                        "-E", entryPoint,
                        "-T", stage switch {
                            ShaderCompileStage.Vertex => "vs_6_6",
                            ShaderCompileStage.Pixel => "ps_6_6",
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
                        bytecodes[offset++] = new ShaderBytecode(MakeBytecodeTarget(target, stage), bytecodeBlob.AsBytes());
                    }
                }
            }

            return true;
        }

        private ShaderCompileStage CountAllStages()
        {
            ShaderCompileStage stages = ShaderCompileStage.None;
            foreach (ref readonly FunctionData function in Functions)
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

        internal AttributeSignature? FindAttributeSignature(ReadOnlySpan<char> name)
        {
            _settings.TryGetSignature(name.ToString(), out AttributeSignature? signature);
            return signature;
        }

        internal int FindRefIndexFor(int code)
        {
            if (_valueRefTable.TryGetValue(code, out int idx))
                return idx;
            return -1;
        }

        internal bool HasValueRef(int code) => _valueRefTable.ContainsKey(code);

        internal void AddNewValueRef(int code) => _valueRefTable.Add(code, _structs.Count);

        internal void AddFunction(FunctionData data) => _functions.Add(data);
        internal void AddResource(ResourceData data) => _resources.Add(data);
        internal void AddProperty(PropertyData data) => _properties.Add(data);
        internal void AddStruct(StructData data) => _structs.Add(data);
        internal void AddStaticSampler(StaticSamplerData data) => _staticSamplers.Add(data);

        public ref readonly StructData GetRefSource(ValueDataRef dataRef)
        {
            if (dataRef.Generic != ValueGeneric.Custom)
                return ref Unsafe.NullRef<StructData>();

            return ref Structs[dataRef.Index];
        }

        public int CalculateSize(ValueDataRef generic)
        {
            if (generic.Generic == ValueGeneric.Custom)
            {
                ref readonly StructData @struct = ref GetRefSource(generic);
                Debug.Assert(!Unsafe.IsNullRef(in @struct));

                int size = 0;
                foreach (ref readonly VariableData variable in @struct.Variables.AsSpan())
                {
                    size += CalculateSize(variable.Generic);
                }

                return size;
            }
            else
            {
                return generic.Generic switch
                {
                    ValueGeneric.Float => sizeof(float),
                    ValueGeneric.Double => sizeof(double),
                    ValueGeneric.UInt => sizeof(uint),
                    ValueGeneric.Int => sizeof(int),
                    _ => throw new NotSupportedException()
                } * generic.Rows * generic.Columns;
            }
        }

        internal ILogger Logger => _logger;

        public ReadOnlySpan<FunctionData> Functions => _functions.AsSpan();
        public ReadOnlySpan<ResourceData> Resources => _resources.AsSpan();
        public ReadOnlySpan<PropertyData> Properties => _properties.AsSpan();
        public ReadOnlySpan<StructData> Structs => _structs.AsSpan();
        public ReadOnlySpan<StaticSamplerData> StaticSamplers => _staticSamplers.AsSpan();

        public string? PropertySourceTemplate { get => _propertySourceTemplate; internal set => _propertySourceTemplate = value; }
        public string? ProcessedSource => _processedSource;

        public bool GeneratePropertiesInHeader { get => _generatePropertiesInHeader; internal set => _generatePropertiesInHeader = value; }

        private static ushort MakeBytecodeTarget(ShaderCompileTarget target, ShaderCompileStage stage) => (ushort)(((ushort)target) | (((ushort)stage) << 2));

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
    }
}
