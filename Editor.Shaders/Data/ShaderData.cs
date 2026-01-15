using CommunityToolkit.HighPerformance;
using Editor.Shaders.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Shaders.Data
{
    public sealed class ShaderData
    {
        private Dictionary<int, int> _valueRefTable;

        private List<FunctionData> _functions;
        private List<ResourceData> _resources;
        private List<PropertyData> _properties;
        private List<StructData> _structs;
        private List<StaticSamplerData> _staticSamplers;

        private Dictionary<string, ReferenceIndex> _referenceDict;

        private string? _propertySourceTemplate;
        private string? _processedSource;

        private bool _generatePropertiesInHeader;

        private bool _areConstantsSeparated;
        private int _headerBytesize;

        private int _kernelCount;

        internal ShaderData()
        {
            _valueRefTable = new Dictionary<int, int>();

            _functions = new List<FunctionData>();
            _resources = new List<ResourceData>();
            _properties = new List<PropertyData>();
            _structs = new List<StructData>();
            _staticSamplers = new List<StaticSamplerData>();

            _referenceDict = new Dictionary<string, ReferenceIndex>();

            _propertySourceTemplate = "__HEADER_CB.RAW_$PNAME$";
            _processedSource = null;

            _generatePropertiesInHeader = true;

            _areConstantsSeparated = false;
            _headerBytesize = 0;

            _kernelCount = 0;
        }

        internal int FindRefIndexFor(int code)
        {
            if (_valueRefTable.TryGetValue(code, out int idx))
                return idx;
            return -1;
        }

        internal bool HasValueRef(int code) => _valueRefTable.ContainsKey(code);

        internal void AddNewValueRef(int code) => _valueRefTable.Add(code, _structs.Count);

        internal void AddFunction(FunctionData data)
        {
            _referenceDict.Add(data.Name, new ReferenceIndex(ReferenceType.Function, _functions.Count));
            _functions.Add(data);
        }

        internal void AddResource(ResourceData data)
        {
            _referenceDict.Add(data.Name, new ReferenceIndex(ReferenceType.Resource, _resources.Count));
            _resources.Add(data);
        }
        internal void AddProperty(PropertyData data)
        {
            if (data.Generic.IsSpecified)
                _referenceDict.Add(data.Name, new ReferenceIndex(ReferenceType.Property, _properties.Count));
            else
            {
                IterateVariables(data.Name, data.Generic);
                void IterateVariables(string name, ValueDataRef generic)
                {
                    _referenceDict.Add(name, new ReferenceIndex(ReferenceType.Property, _properties.Count));
                    
                    if (generic.Generic == ValueGeneric.Custom)
                    {
                        foreach (ref readonly VariableData varData in GetRefSource(generic).Variables.AsSpan())
                        {
                            IterateVariables($"{name}.{varData.Name}", varData.Generic);
                        }
                    }
                }
            }

            _properties.Add(data);
        }
        internal void AddStruct(StructData data) => _structs.Add(data);
        internal void AddStaticSampler(StaticSamplerData data) => _staticSamplers.Add(data);

        public ref readonly StructData GetRefSource(ValueDataRef dataRef)
        {
            if (dataRef.Generic != ValueGeneric.Custom)
                return ref Unsafe.NullRef<StructData>();

            return ref Structs[dataRef.Index];
        }

        public bool TryFindReference(string referenceName, out ReferenceIndex index)
        {
            return _referenceDict.TryGetValue(referenceName, out index);
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

        public ReadOnlySpan<FunctionData> Functions => _functions.AsSpan();
        public ReadOnlySpan<ResourceData> Resources => _resources.AsSpan();
        public ReadOnlySpan<PropertyData> Properties => _properties.AsSpan();
        public ReadOnlySpan<StructData> Structs => _structs.AsSpan();
        public ReadOnlySpan<StaticSamplerData> StaticSamplers => _staticSamplers.AsSpan();

        public bool HasAnyReferences => _referenceDict.Count > 0;

        public string? PropertySourceTemplate { get => _propertySourceTemplate; internal set => _propertySourceTemplate = value; }
        public string? ProcessedSource { get => _processedSource; internal set => _processedSource = value; }

        public bool GeneratePropertiesInHeader { get => _generatePropertiesInHeader; internal set => _generatePropertiesInHeader = value; }

        public bool AreConstantsSeparated { get => _areConstantsSeparated; internal set => _areConstantsSeparated = value; }
        public int HeaderBytesize { get => _headerBytesize; internal set => _headerBytesize = value; }

        public int KernelCount { get => _kernelCount; internal set => _kernelCount = value; }
    }
}
