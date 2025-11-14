using CommunityToolkit.HighPerformance;
using Editor.Shaders.Attributes;
using Editor.Shaders.Data;
using Editor.Shaders.Processors;
using Serilog;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
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

        public void Process(string source, string[] includeDirs, string? sourceFileName = null)
        {
            //1. preprocess source
            {
                using (ShaderIncludeHandler includeHandler = new ShaderIncludeHandler(includeDirs))
                using (IDxcResult result = _compiler.Compile(source, s_preprocessPreset, includeHandler))
                {
                    string errors = result.GetErrors();
                    if (errors.Length > 0)
                    {
                        throw new Exception(errors); //temp
                    }

                    using IDxcBlob blob = result.GetResult();
                    unsafe
                    {
                        //ANYTHING to avoid using Marshal
                        source = new string((sbyte*)blob.BufferPointer);
                    }
                }
            }

            //2. parse the new source

            SourceParser parser = new SourceParser(this, source, sourceFileName ?? ".hlsl");
            parser.Parse();

            //3. generate final source

            SourceAugmenter generator = new SourceAugmenter(this);
            source = generator.Augment(source);
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

        public ref readonly StructData GetRefSource(ValueDataRef dataRef)
        {
            if (dataRef.Generic != ValueGeneric.Custom)
                return ref Unsafe.NullRef<StructData>();

            return ref Structs[dataRef.Index];
        }

        internal ILogger Logger => _logger;

        public ReadOnlySpan<FunctionData> Functions => _functions.AsSpan();
        public ReadOnlySpan<ResourceData> Resources => _resources.AsSpan();
        public ReadOnlySpan<PropertyData> Properties => _properties.AsSpan();
        public ReadOnlySpan<StructData> Structs => _structs.AsSpan();

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
