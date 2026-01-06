using CommunityToolkit.HighPerformance;
using Editor.Shaders.Attributes;
using Editor.Shaders.Data;
using Primary.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace Editor.Shaders.Processors
{
    internal ref struct SourceAugmenter
    {
        private readonly ShaderProcessor _processor;

        private bool _encounteredErrors;

        private string _diagFile;

        internal SourceAugmenter(ShaderProcessor processor, string sourceFileName)
        {
            _processor = processor;

            _encounteredErrors = false;

            _diagFile = sourceFileName;
        }

        internal string? Augment(string orignalSource)
        {
            using RentedArray<char> augment = RentedArray<char>.Rent(orignalSource.Length);
            orignalSource.CopyTo(augment.Span);

            HeaderFeatures features = HeaderFeatures.None;
            bool areConstantsSolo = false;
            int headerBytesize = 0;

            int variableCount = CountTotalVariablesUsed();
            using RentedArray<RawVariableData> variableDatas = RentedArray<RawVariableData>.Rent(variableCount);

            FillAndSortVariables(variableDatas.Span, out features);
            CalculateHeaderBinding(variableDatas.Span, out headerBytesize, out areConstantsSolo);

            StringBuilder sb = new StringBuilder();

            RemoveOldDirectives(augment.Span);
            RegenerateStructs(sb);
            RegenerateStaticSamplers(sb);
            GenerateNewHeader(sb, features, headerBytesize, areConstantsSolo, variableDatas.Span, out int additionalOffset);

            sb.Append(augment.Span);
            string str = AppendBindlessResources(sb, sb.ToString(), additionalOffset, areConstantsSolo, variableDatas.Span);

            _processor.AreConstantsSeparated = areConstantsSolo;
            _processor.HeaderBytesize = headerBytesize;

            return _encounteredErrors ? null : str;
        }

        private void RemoveOldDirectives(Span<char> source)
        {
            foreach (ref readonly FunctionData function in _processor.Functions)
            {
                foreach (ref readonly AttributeData attribute in function.Attributes.AsSpan())
                {
                    for (int i = attribute.DeclerationRange.Start; i < attribute.DeclerationRange.End; i++)
                    {
                        if (!char.IsWhiteSpace(source[i]))
                            source[i] = ' ';
                    }
                }
            }

            foreach (ref readonly ResourceData resource in _processor.Resources)
            {
                for (int i = resource.DeclerationRange.Start; i < resource.DeclerationRange.End; i++)
                {
                    if (!char.IsWhiteSpace(source[i]))
                        source[i] = ' ';
                }

                foreach (ref readonly AttributeData attribute in resource.Attributes.AsSpan())
                {
                    for (int i = attribute.DeclerationRange.Start; i < attribute.DeclerationRange.End; i++)
                    {
                        if (!char.IsWhiteSpace(source[i]))
                            source[i] = ' ';
                    }
                }
            }

            foreach (ref readonly PropertyData property in _processor.Properties)
            {
                //for (int i = property.DeclerationRange.Start; i < property.DeclerationRange.End; i++)
                //{
                //    if (!char.IsWhiteSpace(source[i]))
                //        source[i] = ' ';
                //}

                foreach (ref readonly AttributeData attribute in property.Attributes.AsSpan())
                {
                    for (int i = attribute.DeclerationRange.Start; i < attribute.DeclerationRange.End; i++)
                    {
                        if (!char.IsWhiteSpace(source[i]))
                            source[i] = ' ';
                    }
                }
            }

            foreach (ref readonly StructData @struct in _processor.Structs)
            {
                for (int i = @struct.DeclerationRange.Start; i < @struct.DeclerationRange.End; i++)
                {
                    if (!char.IsWhiteSpace(source[i]))
                        source[i] = ' ';
                }

                foreach (ref readonly AttributeData attribute in @struct.Attributes.AsSpan())
                {
                    for (int i = attribute.DeclerationRange.Start; i < attribute.DeclerationRange.End; i++)
                    {
                        if (!char.IsWhiteSpace(source[i]))
                            source[i] = ' ';
                    }
                }
            }

            foreach (ref readonly StaticSamplerData staticSampler in _processor.StaticSamplers)
            {
                //bool doClear = false;
                //for (int i = staticSampler.DeclerationRange.Start; i < staticSampler.DeclerationRange.End; i++)
                //{
                //    if (doClear)
                //    {
                //        if (!char.IsWhiteSpace(source[i]))
                //            source[i] = ' ';
                //    }
                //    else if (source[i] == ':' || source[i] == '{')
                //    {
                //        doClear = true;
                //        i--;
                //    }
                //}

                for (int i = staticSampler.DeclerationRange.Start; i < staticSampler.DeclerationRange.End; i++)
                {
                    if (!char.IsWhiteSpace(source[i]))
                        source[i] = ' ';
                }

                foreach (ref readonly AttributeData attribute in staticSampler.Attributes.AsSpan())
                {
                    for (int i = attribute.DeclerationRange.Start; i < attribute.DeclerationRange.End; i++)
                    {
                        if (!char.IsWhiteSpace(source[i]))
                            source[i] = ' ';
                    }
                }
            }
        }

        private void RegenerateStructs(StringBuilder sb)
        {
            foreach (ref readonly StructData @struct in _processor.Structs)
            {
                sb.Append("struct ");
                sb.AppendLine(@struct.Name);
                sb.AppendLine("{");

                foreach (ref readonly VariableData varData in @struct.Variables.AsSpan())
                {
                    sb.Append("    ");
                    SerializeGeneric(sb, varData.Generic, varData.Name);
                    sb.Append(' ');
                    sb.Append(varData.Name);

                    if (varData.Semantic != null)
                    {
                        VarSemantic semantic = varData.Semantic.Value;

                        sb.Append(" : ");
                        sb.Append(semantic.Semantic);

                        if (semantic.Index > 0)
                            sb.Append(semantic.Index);
                    }

                    sb.AppendLine(";");
                }

                sb.AppendLine("};");
                sb.AppendLine();
            }
        }

        private void RegenerateStaticSamplers(StringBuilder sb)
        {
            int i = 0;
            foreach (ref readonly StaticSamplerData staticSampler in _processor.StaticSamplers)
            {
                sb.Append("SamplerState ");
                sb.Append(staticSampler.Name);
                sb.Append(" : register(s");
                sb.Append(i++);
                sb.AppendLine(");");
            }

            sb.AppendLine();
        }

        private void GenerateNewHeader(StringBuilder sb, HeaderFeatures features, int headerBytesize, bool areConstantsSolo, Span<RawVariableData> span, out int additionalOffset)
        {
            additionalOffset = -1;

            {
                sb.AppendLine("struct __BG_HEADER");
                sb.AppendLine("{");

                foreach (ref readonly RawVariableData varData in span)
                {
                    sb.Append("    ");

                    switch (varData.Usage)
                    {
                        case RawVariableUsage.Global:
                        case RawVariableUsage.Property:
                        case RawVariableUsage.Resource:
                            {
                                switch (varData.Type)
                                {
                                    case RawVariableDataType.Struct:
                                        {
                                            ValueDataRef generic = varData.Generic;
                                            if (generic.Generic == ValueGeneric.Custom)
                                            {
                                                ref readonly StructData @struct = ref _processor.GetRefSource(generic);
                                                if (Unsafe.IsNullRef(in @struct))
                                                {
                                                    ReportErrorMessage("Failed to find struct for global data: {d}", varData.Name);
                                                    return;
                                                }

                                                sb.Append(@struct.Name);
                                            }
                                            else
                                            {
                                                sb.Append(s_genericVarNames[(int)generic.Generic]);
                                                if (generic.Rows > 1)
                                                {
                                                    sb.Append(generic.Rows);
                                                    if (generic.Columns > 1)
                                                    {
                                                        sb.Append('x');
                                                        sb.Append(generic.Columns);
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                    case RawVariableDataType.Resource:
                                        {
                                            sb.Append("uint");
                                            break;
                                        }
                                    case RawVariableDataType.Property:
                                        {
                                            if (_processor.GeneratePropertiesInHeader)
                                            {
                                                SerializeGeneric(sb, varData.Generic, varData.Name);
                                            }
                                            break;
                                        }
                                }

                                break;
                            }
                        case RawVariableUsage.Constants:
                            {
                                if (areConstantsSolo)
                                    continue;

                                ValueDataRef generic = varData.Generic;
                                if (generic.Generic == ValueGeneric.Custom)
                                {
                                    ref readonly StructData @struct = ref _processor.GetRefSource(generic);
                                    if (Unsafe.IsNullRef(in @struct))
                                    {
                                        ReportErrorMessage("Failed to find struct for global data: {d}", varData.Name);
                                        return;
                                    }

                                    sb.Append(@struct.Name);
                                }
                                else
                                {
                                    sb.Append(s_genericVarNames[(int)generic.Generic]);
                                    if (generic.Rows > 1)
                                    {
                                        sb.Append(generic.Rows);
                                        if (generic.Columns > 1)
                                        {
                                            sb.Append('x');
                                            sb.Append(generic.Columns);
                                        }
                                    }
                                }

                                break;
                            }
                    }

                    sb.Append(' ');
                    if (varData.Usage == RawVariableUsage.Constants)
                        sb.Append("CON_Raw");
                    else
                    {
                        sb.Append(varData.Usage == RawVariableUsage.Property ? "RAW_" : "IDX_");
                        sb.Append(varData.Name);
                    }
                    sb.AppendLine(";");
                }

                sb.AppendLine("};");
                sb.AppendLine();
            }

            if (areConstantsSolo)
            {
                int constantsIndex = span.FindIndex((x) => x.Usage == RawVariableUsage.Constants);
                ref readonly RawVariableData varData = ref span[constantsIndex];

                sb.AppendLine("ConstantBuffer<__BG_HEADER> __HEADER_CB : register(b1);");
                sb.AppendLine();

                sb.AppendLine("#ifdef __spirv__");
                sb.AppendLine("[[vk::push_constant]]");
                sb.AppendLine("#endif");
                sb.Append("ConstantBuffer<");

                ValueDataRef generic = varData.Generic;
                if (generic.Generic == ValueGeneric.Custom)
                {
                    ref readonly StructData @struct = ref _processor.GetRefSource(generic);
                    if (Unsafe.IsNullRef(in @struct))
                    {
                        ReportErrorMessage("Failed to find struct for global data: {d}", varData.Name);
                        return;
                    }

                    sb.Append(@struct.Name);
                }
                else
                {
                    sb.Append(s_genericVarNames[(int)generic.Generic]);
                    if (generic.Rows > 1)
                    {
                        sb.Append(generic.Rows);
                        if (generic.Columns > 1)
                        {
                            sb.Append('x');
                            sb.Append(generic.Columns);
                        }
                    }
                }

                sb.Append("> ");
                sb.Append(varData.Name);
                sb.AppendLine(" : register(b0);");
            }
            else
            {
                sb.AppendLine("#ifdef __spirv__");
                sb.AppendLine("[[vk::push_constant]]");
                sb.AppendLine("#endif");
                sb.AppendLine("ConstantBuffer<__BG_HEADER> __HEADER_CB : register(b0);");
            }

            sb.AppendLine();
            additionalOffset = sb.Length;
        }

        private string AppendBindlessResources(StringBuilder sb, string @string, int additionalOffset, bool areConstantsSolo, Span<RawVariableData> span)
        {
            StringBuilder sb2 = new StringBuilder();

            Dictionary<string, RawVariableData> existing = new Dictionary<string, RawVariableData>();
            for (int i = 0; i < span.Length; i++)
            {
                existing[span[i].Name] = span[i];
            }

            HashSet<string> found = new HashSet<string>();
            foreach (ref readonly FunctionData function in _processor.Functions)
            {
                found.Clear();
                ReadOnlySpan<char> interestingPortion = @string.AsSpan().Slice(function.BodyRange.Start + additionalOffset, function.BodyRange.End - function.BodyRange.Start);
                SearchForVariables(interestingPortion);

                if (_processor.PropertySourceTemplate != null)
                {
                    if (Array.Exists(function.Attributes, (x) => x.Signature is AttributePixel))
                    {
                        sb2.Clear();
                        foreach (ref readonly RawVariableData varData in span)
                        {
                            if (varData.Type == RawVariableDataType.Property)
                            {
                                string template = ResolvePropertyTemplate(_processor.PropertySourceTemplate, in varData, in function);
                                SearchForVariables(template);

                                sb2.Append(varData.Name);
                                sb2.Append(" = ");
                                sb2.Append(template);
                                sb2.Append(';');
                            }
                        }
                    }
                }

                sb.Clear();
                if (found.Count > 0)
                {
                    foreach (string foundVar in found)
                    {
                        ref readonly RawVariableData varData = ref CollectionsMarshal.GetValueRefOrNullRef(existing, foundVar);
                        Debug.Assert(!Unsafe.IsNullRef(in varData));

                        if (varData.Usage == RawVariableUsage.Constants)
                        {
                            if (areConstantsSolo)
                                continue;

                            SerializeGeneric(sb, varData.Generic, varData.Name);
                            sb.Append(varData.Name);
                            sb.Append(" = __HEADER_CB.CON_");
                            sb.Append(varData.Name);
                            sb.Append(';');
                        }
                        else if (varData.Usage != RawVariableUsage.Property)
                        {
                            ref readonly ResourceData resourceData = ref _processor.Resources[varData.Index];

                            sb.Append(resourceData.Type.ToString());
                            if (varData.Generic.IsSpecified)
                            {
                                sb.Append('<');
                                SerializeGeneric(sb, varData.Generic, varData.Name);
                                sb.Append('>');
                            }
                            sb.Append(' ');
                            sb.Append(varData.Name);
                            sb.Append(" = (");
                            sb.Append(resourceData.Type.ToString());
                            if (varData.Generic.IsSpecified)
                            {
                                sb.Append('<');
                                SerializeGeneric(sb, varData.Generic, varData.Name);
                                sb.Append('>');
                            }
                            sb.Append(resourceData.Type == ResourceType.SamplerState ? ")SamplerDescriptorHeap[__HEADER_CB.IDX_" : ")ResourceDescriptorHeap[__HEADER_CB.IDX_");
                            sb.Append(varData.Name);
                            sb.Append("];");
                        }
                    }
                }

                if (sb2.Length > 0)
                {
                    sb.Append(sb2);
                }

                if (sb.Length > 0)
                {
                    @string = @string.Insert(function.BodyRange.Start + additionalOffset - 1, sb.ToString());
                    additionalOffset += sb.Length;
                }
            }

            return @string;

            void SearchForVariables(ReadOnlySpan<char> interestingPortion)
            {
                foreach (var kvp in existing)
                {
                    string varName = kvp.Key;

                    int index = interestingPortion.IndexOf(varName, StringComparison.Ordinal);
                    ReadOnlySpan<char> subsection = interestingPortion;

                    while (index > -1)
                    {
                        subsection = subsection.Slice(index);
                        index = 0;

                        if (varName.Length == subsection.Length || !char.IsLetterOrDigit(subsection[varName.Length]))
                        {
                            found.Add(varName);
                        }

                        subsection = subsection.Slice(1);
                        index = subsection.IndexOf(varName, StringComparison.Ordinal);
                    }
                }
            }
        }

        private int CountTotalVariablesUsed() => _processor.Resources.Length + _processor.Properties.Length;
        private void FillAndSortVariables(Span<RawVariableData> variableDatas, out HeaderFeatures features)
        {
            features = HeaderFeatures.None;

            Dictionary<string, int> bindGroupIndices = new Dictionary<string, int>();

            int offset = 0;
            int i = 0;

            foreach (ref readonly ResourceData resource in _processor.Resources)
            {
                RawVariableUsage usage = RawVariableUsage.Resource;
                int bindGroup = 0;

                foreach (ref readonly AttributeData attribute in resource.Attributes.AsSpan())
                {
                    if (attribute.Signature is AttributeGlobal)
                    {
                        usage = RawVariableUsage.Global;
                        bindGroup = -1;

                        features |= HeaderFeatures.HasGlobals;
                    }
                    else if (attribute.Signature is AttributeConstants)
                    {
                        usage = RawVariableUsage.Constants;

                        features |= HeaderFeatures.HasConstants;
                    }
                    else if (attribute.Signature is AttributeBindGroup bg)
                    {
                        if (!bindGroupIndices.TryGetValue(bg.Name, out bindGroup))
                        {
                            bindGroup = bindGroupIndices.Count + 1;
                            bindGroupIndices.Add(bg.Name, bindGroup);
                        }
                    }
                }

                variableDatas[offset++] = new RawVariableData(RawVariableDataType.Resource, usage, i++, resource.Name, bindGroup, resource.Value);
            }

            i = 0;

            foreach (ref readonly PropertyData property in _processor.Properties)
            {
                RawVariableUsage usage = RawVariableUsage.Property;
                int bindGroup = 0;

                foreach (ref readonly AttributeData attribute in property.Attributes.AsSpan())
                {
                    if (attribute.Signature is AttributeGlobal)
                    {
                        usage = RawVariableUsage.Global;
                        bindGroup = -1;

                        features |= HeaderFeatures.HasGlobals;
                    }
                    else if (attribute.Signature is AttributeBindGroup bg)
                    {
                        if (!bindGroupIndices.TryGetValue(bg.Name, out bindGroup))
                        {
                            bindGroup = bindGroupIndices.Count + 1;
                            bindGroupIndices.Add(bg.Name, bindGroup);
                        }
                    }
                }

                if (usage == RawVariableUsage.Property)
                    features |= HeaderFeatures.HasProperties;
                variableDatas[offset++] = new RawVariableData(RawVariableDataType.Property, usage, i++, property.Name, bindGroup, property.Generic);
            }

            variableDatas.Sort();
        }

        private void CalculateHeaderBinding(Span<RawVariableData> variableDatas, out int headerBytesize, out bool areConstantsSolo)
        {
            headerBytesize = 0;
            areConstantsSolo = false;

            int constantsSize = 0;

            foreach (ref readonly ResourceData resource in _processor.Resources)
            {
                AttributeData data = Array.Find(resource.Attributes, (x) => x.Signature is AttributeConstants);
                if (data != default)
                {
                    constantsSize += _processor.CalculateSize(resource.Value);
                }
                else
                {
                    headerBytesize += sizeof(uint);
                }
            }

            if (_processor.GeneratePropertiesInHeader)
            {
                foreach (ref readonly PropertyData property in _processor.Properties)
                {
                    headerBytesize += _processor.CalculateSize(property.Generic);
                }
            }

            //a separate buffer costs 4 bytes i believe
            if (headerBytesize + constantsSize > ShaderProcessor.MaxConstantsBufferSize - 4)
            {
                areConstantsSolo = true;
            }
            else
            {
                headerBytesize += constantsSize;
                areConstantsSolo = false;
            }
        }

        private string ResolvePropertyTemplate(string template, ref readonly RawVariableData varData, ref readonly FunctionData function)
        {
            //TODO: improve this with "PooledStringBuilder" in append mode

            string copy = template;

            int index = 0;
            while ((index = copy.IndexOf('$')) != -1)
            {
                int last = copy.IndexOf('$', index + 1);
                if (last == -1)
                {
                    ReportErrorMessage("Property source template macro at {i} is not properly terminated", index);
                    return string.Empty;
                }

                ReadOnlySpan<char> subsection = copy.AsSpan().Slice(index + 1, last - index - 1);
                if (subsection.Equals("GENERIC", StringComparison.OrdinalIgnoreCase))
                {
                    copy = copy.Remove(index, subsection.Length + 2);
                    copy = copy.Insert(index, SerializeGenericAsString(varData.Generic, varData.Name));
                }
                else if (subsection.Equals("SV_INSTANCEID", StringComparison.OrdinalIgnoreCase))
                {
                    string? path = SearchForSemanticPath(_processor, SemanticName.SV_InstanceId, function.Arguments);
                    if (path == null)
                    {
                        ReportErrorMessage("Failed to find a variable with a \"SV_InstanceID\" semantic decleration in function {f} input arguments.", function.Name);
                        return string.Empty;
                    }

                    copy = copy.Remove(index, subsection.Length + 2);
                    copy = copy.Insert(index, path);
                }
                else if (subsection.Equals("PNAME", StringComparison.OrdinalIgnoreCase))
                {
                    copy = copy.Remove(index, subsection.Length + 2);
                    copy = copy.Insert(index, varData.Name);
                }
            }

            return copy;

            static string? SearchForSemanticPath(ShaderProcessor processor, SemanticName semanticName, Span<VariableData> variables, string? previousPath = null)
            {
                foreach (ref readonly VariableData data in variables)
                {
                    if (data.Semantic.HasValue && data.Semantic.Value.Semantic == semanticName)
                    {
                        return previousPath + data.Name;
                    }
                    else if (data.Generic.Generic == ValueGeneric.Custom)
                    {
                        ref readonly StructData @struct = ref processor.GetRefSource(data.Generic);
                        if (Unsafe.IsNullRef(in @struct))
                            continue;

                        string? ret = SearchForSemanticPath(processor, semanticName, @struct.Variables, data.Name + ".");
                        if (ret != null)
                            return ret;
                    }
                }

                return null;
            }
        }

        private void SerializeGeneric(StringBuilder sb, ValueDataRef generic, in string name)
        {
            if (generic.IsSpecified)
            {
                if (generic.Generic == ValueGeneric.Custom)
                {
                    ref readonly StructData @struct = ref _processor.GetRefSource(generic);
                    if (Unsafe.IsNullRef(in @struct))
                    {
                        ReportErrorMessage("Failed to find struct for global data: {d}", name);
                        return;
                    }

                    sb.Append(@struct.Name);
                }
                else
                {
                    sb.Append(s_genericVarNames[(int)generic.Generic]);
                    if (generic.Rows > 1)
                    {
                        sb.Append(generic.Rows);
                        if (generic.Columns > 1)
                        {
                            sb.Append('x');
                            sb.Append(generic.Columns);
                        }
                    }
                }
            }
        }

        private string SerializeGenericAsString(ValueDataRef generic, in string name)
        {
            if (generic.IsSpecified)
            {
                if (generic.Generic == ValueGeneric.Custom)
                {
                    ref readonly StructData @struct = ref _processor.GetRefSource(generic);
                    if (Unsafe.IsNullRef(in @struct))
                    {
                        ReportErrorMessage("Failed to find struct for global data: {d}", name);
                        return string.Empty;
                    }

                    return @struct.Name;
                }
                else
                {
                    if (generic.Rows > 1)
                    {
                        if (generic.Columns > 1)
                        {
                            return $"{s_genericVarNames[(int)generic.Generic]}{generic.Rows}x{generic.Columns}";
                        }
                        else
                        {
                            return $"{s_genericVarNames[(int)generic.Generic]}{generic.Rows}";
                        }
                    }
                    else
                        return s_genericVarNames[(int)generic.Generic];
                }
            }

            return string.Empty;
        }

        private void ReportErrorMessage(string message, params object?[]? objects)
        {
            if (objects != null && objects.Length > 0)
                _processor.Logger.Error("[{f}] " + message, [_diagFile, .. objects]);
            else
                _processor.Logger.Error("[{f}] " + message, _diagFile);
        }

        private static readonly string[] s_genericVarNames = [
            "float",
            "double",
            "int",
            "uint"
            ];

        private readonly record struct RawVariableData(RawVariableDataType Type, RawVariableUsage Usage, int Index, string Name, int BindGroup, ValueDataRef Generic) : IComparable<RawVariableData>
        {
            public int CompareTo(RawVariableData other)
            {
                int x = 0;

                if ((x = BindGroup.CompareTo(other.BindGroup)) != 0) return x;
                if ((x = Usage.CompareTo(other.Usage)) != 0) return x;
                if ((x = Type.CompareTo(other.Type)) != 0) return x;
                return Name.CompareTo(other.Name, StringComparison.Ordinal);
            }
        }

        private enum RawVariableDataType : byte
        {
            Property,
            Struct,
            Resource
        }

        private enum RawVariableUsage : byte
        {
            Global = 0,
            Constants,
            Property,
            Resource,
        }

        private enum HeaderFeatures : byte
        {
            None = 0,

            HasGlobals = 1 << 0,
            HasConstants = 1 << 1,
            HasProperties = 1 << 2,
        }
    }
}
