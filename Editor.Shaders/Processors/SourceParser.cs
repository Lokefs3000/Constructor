using CommunityToolkit.HighPerformance;
using Editor.Shaders.Attributes;
using Editor.Shaders.Data;
using Primary.Common;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Processors
{
    internal ref struct SourceParser
    {
        private readonly ShaderProcessor _processor;

        private bool _encounteredErrors;

        private ReadOnlySpan<char> _source;

        private int _line;
        private int _index;
        private int _start;

        private int _diagLine;
        private int _diagIndex;
        private int _diagStart;

        private string _diagFile;

        private Queue<AttributeData> _attributes;

        private HashSet<Type> _signatureReqSet;
        private HashSet<int> _propertySet;

        internal SourceParser(ShaderProcessor processor, ReadOnlySpan<char> source, string sourceFile)
        {
            _processor = processor;

            _encounteredErrors = false;

            _source = source;

            _line = 0;
            _index = 0;
            _start = 0;

            _diagLine = 0;
            _diagIndex = 0;
            _diagStart = 0;

            _diagFile = sourceFile;

            _attributes = new Queue<AttributeData>();

            _signatureReqSet = new HashSet<Type>();
            _propertySet = new HashSet<int>();
        }

        internal bool Parse()
        {
            while (_index < _source.Length)
            {
                char c = Advance();

                switch (c)
                {
                    case '/': MoveUntilOutOfComment(ref _index); break;
                    case '#': ParsePreprocessor(); break;

                    case '[': ParseAttribute(); break;

                    //case '\n': _line++; break;

                    default:
                        {
                            if (char.IsLetter(c))
                                ParseIdentifier();
                            break;
                        }
                }

                _start = _index;
            }

            return !_encounteredErrors;
        }

        private void ParseIdentifier()
        {
            int absoluteStart = _start;

            ReadOnlySpan<char> identifier = ReadGeneralIdentifier();
            int hash = identifier.GetDjb2HashCode();

            IdentifierIntent intent = IdentifierIntent.Unknown;

            if (char.IsWhiteSpace(Peek()))
                SkipWhitespace();

            Range signatureRange = Range.All;
            if (Peek() == '<')
            {
                Advance();
                if (char.IsWhiteSpace(Peek()))
                    SkipWhitespace();

                ReadOnlySpan<char> valueType = ReadGeneralIdentifier();
                intent = IdentifierIntent.Resource;

                if (char.IsWhiteSpace(Peek()))
                    SkipWhitespace();
                if (Advance() != '>')
                    return;

                signatureRange = new Range(_start, _index);
            }

            if (char.IsWhiteSpace(Peek()))
                SkipWhitespace();

            int backup = _index;
            _start = _index;

            ReadOnlySpan<char> name = ReadGeneralIdentifier();

            if (intent == IdentifierIntent.Unknown)
                intent = DecodeIdentifierIntent(absoluteStart, identifier, name);
            switch (intent)
            {
                case IdentifierIntent.Function: ParseFunction(name); break;
                case IdentifierIntent.Resource: ParseResource(identifier, name, signatureRange, absoluteStart); break;
                case IdentifierIntent.Property: ParseProperty(identifier, name, absoluteStart); break;
                case IdentifierIntent.Struct: ParseStruct(name, absoluteStart); break;
                case IdentifierIntent.StaticSampler: ParseStaticSampler(absoluteStart); break;
                default:
                    {
                        _index = backup;
                        break;
                    }
            }
        }

        private void ParseFunction(ReadOnlySpan<char> name)
        {
            AttributeData[] attributes = _attributes.Count > 0 ? _attributes.ToArray() : Array.Empty<AttributeData>();
            _attributes.Clear();

            if (attributes.Length > 0)
            {
                ValidateAttributes(attributes, AttributeUsage.Function);
            }

            using RentedArray<VariableData> variables = RentedArray<VariableData>.Rent(64, true);
            int totalVars = 0;

            SkipUntil('(');
            Advance();

            ValueDataRef dataRef = ValueDataRef.Unspecified;
            while (Peek() != ')')
            {
                _start = _index;

                char c = Advance();
                if (char.IsLetter(c) || c == '_')
                {
                    ReadOnlySpan<char> identifier = ReadGeneralIdentifier();

                    if (dataRef.IsSpecified)
                    {
                        VarSemantic? semantic = null;
                        if (FindAfterWhitespace(':', _index))
                        {
                            while (!char.IsLetter(Advance())) ;

                            _start = _index - 1;
                            ReadOnlySpan<char> semanticName = ReadGeneralIdentifier();

                            semantic = AttemptDecodeSemantic(semanticName);
                        }

                        if (totalVars >= variables.Count)
                        {
                            ReportErrorMessage("Too many arguments in function call!");
                            return;
                        }

                        variables[totalVars++] = new VariableData(identifier.ToString(), Array.Empty<AttributeData>(), dataRef, semantic);
                    }
                    else
                    {
                        dataRef = DecodeValueDataRefFromType(identifier);
                    }
                }
                else if (c == ',')
                {
                    dataRef = ValueDataRef.Unspecified;
                }
            }

            SkipUntil('{');
            Advance();

            int start = _index + 1;

            int depth = 1;
            while (!IsEOF && depth > 0)
            {
                char c = Advance();
                if (c == '/')
                    MoveUntilOutOfComment(ref _index);
                else if (c == '{')
                    depth++;
                else if (c == '}')
                    depth--;
            }

            _processor.AddFunction(new FunctionData(name.ToString(), attributes, totalVars > 0 ? variables.ToArray(0, totalVars) : Array.Empty<VariableData>(), new IndexRange(start, _index)));
        }

        private void ParseResource(ReadOnlySpan<char> identifier, ReadOnlySpan<char> name, Range signatureRange, int absoluteStart)
        {
            AttributeData[] attributes = _attributes.Count > 0 ? _attributes.ToArray() : Array.Empty<AttributeData>();
            _attributes.Clear();

            int djb2 = identifier.GetDjb2HashCode();
            ResourceType type = (ResourceType)Array.FindIndex(s_resourceNameList, (x) => x == djb2);

            if ((int)type == -1)
                return;

            if (attributes.Length > 0)
            {
                ValidateAttributes(attributes, type switch
                {
                    ResourceType.Texture1D => AttributeUsage.Texture1D,
                    ResourceType.Texture2D => AttributeUsage.Texture2D,
                    ResourceType.Texture3D => AttributeUsage.Texture3D,
                    ResourceType.TextureCube => AttributeUsage.TextureCube,
                    ResourceType.ConstantBuffer => AttributeUsage.ConstantBuffer,
                    ResourceType.StructuredBuffer => AttributeUsage.StructuredBuffer,
                    ResourceType.ByteAddressBuffer => AttributeUsage.ByteAddressBuffer,
                    ResourceType.SamplerState => AttributeUsage.SamplerState,
                    _ => throw new NotSupportedException()
                });
            }

            ValueDataRef valueRef = ValueDataRef.Unspecified;
            if (!signatureRange.Equals(Range.All))
            {
                using (BackupIndexState())
                {
                    _start = signatureRange.Start.Value + identifier.Length + 1;
                    _index = _start;

                    while (!char.IsLetter(Peek()))
                        Advance();

                    ReadOnlySpan<char> valueType = ReadGeneralIdentifier();
                    valueRef = DecodeValueDataRefFromType(valueType);
                }
            }
            else
            {
                signatureRange = new Range(absoluteStart, _index);
            }

            if (FindAfterWhitespace(':', _index))
            {
                SkipUntil(';');
            }

            _processor.AddResource(new ResourceData(name.ToString(), type, valueRef, attributes, new IndexRange(signatureRange.Start.Value, _index)));
        }

        private void ParseProperty(ReadOnlySpan<char> identifier, ReadOnlySpan<char> name, int absoluteStart)
        {
            AttributeData[] attributes = _attributes.Count > 0 ? _attributes.ToArray() : Array.Empty<AttributeData>();
            _attributes.Clear();

            if (attributes.Length > 0)
            {
                ValidateAttributes(attributes, AttributeUsage.Property);
            }

            ValueDataRef variable = DecodeValueDataRefFromType(identifier);
            if (variable == ValueDataRef.Unspecified)
                return;

            _processor.AddProperty(new PropertyData(name.ToString(), variable, attributes, new IndexRange(absoluteStart, _index)));
        }

        private void ParseStruct(ReadOnlySpan<char> name, int absoluteStart)
        {
            if (!FindAfterWhitespace('{', _index))
                return;

            SkipUntil('{');
            Advance();

            ValueDataRef currentValue = ValueDataRef.Unspecified;

            using RentedArray<VariableData> tempArray = RentedArray<VariableData>.Rent(64);
            int totalVars = 0;

            do
            {
                char c = Advance();

                if (c == '/')
                    MoveUntilOutOfComment(ref _index);
                else if (c == '[')
                    ParseAttribute();
                else if (char.IsLetter(c))
                {
                    ReadOnlySpan<char> type = ReadGeneralIdentifier();

                    if (currentValue != ValueDataRef.Unspecified)
                    {
                        ReadOnlySpan<char> varName = ReadGeneralIdentifier();

                        VarSemantic? semantic = null;
                        if (FindAfterWhitespace(':', _index))
                        {
                            SkipUntil(':');
                            Advance();
                            SkipWhitespace();

                            _start = _index;
                            semantic = AttemptDecodeSemantic(ReadGeneralIdentifier());
                        }

                        AttributeData[] attributes = _attributes.Count > 0 ? _attributes.ToArray() : Array.Empty<AttributeData>();
                        _attributes.Clear();

                        if (attributes.Length > 0)
                        {
                            ValidateAttributes(attributes, AttributeUsage.Property);
                        }

                        if (totalVars >= tempArray.Count)
                        {
                            ReportErrorMessage("Too many elements in struct");
                            break;
                        }

                        tempArray[totalVars++] = new VariableData(varName.ToString(), attributes, currentValue, semantic);

                        SkipUntil(';');

                        currentValue = ValueDataRef.Unspecified;
                    }
                    else
                    {
                        ValueDataRef tempVDR = DecodeValueDataRefFromType(type);
                        if (tempVDR != ValueDataRef.Unspecified)
                            currentValue = tempVDR;
                    }
                }

                if (!char.IsLetter(c))
                    _start = _index;
            } while (Peek() != '}');

            _processor.AddNewValueRef(name.GetDjb2HashCode());
            _processor.AddStruct(new StructData(name.ToString(), Array.Empty<AttributeData>(), totalVars > 0 ? tempArray.ToArray(0, totalVars) : Array.Empty<VariableData>(), new IndexRange(absoluteStart, _index + 1)));
        }

        private void ParseStaticSampler(int absoluteStart)
        {
            AttributeData[] attributes = _attributes.Count > 0 ? _attributes.ToArray() : Array.Empty<AttributeData>();
            _attributes.Clear();

            if (attributes.Length > 0)
            {
                ValidateAttributes(attributes, AttributeUsage.StaticSampler);
            }

            SkipWhitespace();

            _start = _index;
            ReadOnlySpan<char> name = ReadGeneralIdentifier();

            SamplerTempData tempData = new SamplerTempData();

            if (FindAfterWhitespace(':', _index))
            {
                SkipUntilNextLetter();

                _start = _index;
                ReadOnlySpan<char> preset = ReadGeneralIdentifier();
                int djb2 = preset.GetDjb2HashCode();

                if (!s_presetSamplerStates.TryGetValue(djb2, out tempData))
                {
                    ReportErrorMessage("Unknown static sampler preset: {pr}", preset.ToString());
                    return;
                }
            }

            if (FindAfterWhitespace('{', _index))
            {
                SkipUntilNextLetter();

                _propertySet.Clear();
                while (Peek() != '}')
                {
                    _start = _index;
                    ReadOnlySpan<char> propName = ReadGeneralIdentifier();

                    int djb2 = propName.GetDjb2HashCode();
                    if (_propertySet.Contains(djb2))
                    {
                        ReportErrorMessage("Sampler property already defined: {p}", propName.ToString());
                        return;
                    }

                    if (!FindAfterWhitespace('=', _index))
                    {
                        ReportErrorMessage("Expected assignment operator after property name: {p}", propName.ToString());
                        return;
                    }

                    SkipUntilNextLetter();

                    if (djb2 == s_samplerFilterName)
                    {
                        if (Peek() != '"' || Peek() != '\'')
                        {
                            ReportErrorMessage("Expected string for enum value");
                            return;
                        }

                    _start = _index;
                        ReadOnlySpan<char> value = ReadString(Peek());
                        if (Enum.TryParse(value, false, out SamplerFilter filter))
                            tempData.Filter = filter;
                        else
                        {
                            ReportErrorMessage("Failed to parse sampler filter value: {v}", value.ToString());
                            return;
                        }
                    }
                    else if (djb2 == s_samplerAddressUName || djb2 == s_samplerAddressVName || djb2 == s_samplerAddressWName)
                    {
                        if (Peek() != '"' || Peek() != '\'')
                        {
                            ReportErrorMessage("Expected string for enum value");
                            return;
                        }

                    _start = _index;
                        ReadOnlySpan<char> value = ReadString(Peek());
                        if (Enum.TryParse(value, false, out SamplerAddressMode addressMode))
                        {
                            if (djb2 == s_samplerAddressUName)
                                tempData.AddressModeU = addressMode;
                            else if (djb2 == s_samplerAddressUName)
                                tempData.AddressModeV = addressMode;
                            else
                                tempData.AddressModeW = addressMode;
                        }
                        else
                        {
                            ReportErrorMessage("Failed to parse sampler address mode value: {v}", value.ToString());
                            return;
                        }
                    }
                    else if (djb2 == s_samplerMaxAnisotropyName)
                    {
                        if (!char.IsDigit(Peek()))
                        {
                            ReportErrorMessage("Expected digit for number value");
                            return;
                        }

                    _start = _index;
                        ReadOnlySpan<char> value = ReadNumber(false);
                        if (uint.TryParse(value, CultureInfo.InvariantCulture, out uint result))
                            tempData.MaxAnisotropy = result;
                        else
                        {
                            ReportErrorMessage("Failed to parse max anisotropy value: {v}", value.ToString());
                        }
                    }
                    else if (djb2 == s_samplerMipLODBiasName || djb2 == s_samplerMinLODName || djb2 == s_samplerMaxLODName)
                    {
                        if (!char.IsDigit(Peek()))
                        {
                            ReportErrorMessage("Expected digit for number value");
                            return;
                        }

                    _start = _index;
                        ReadOnlySpan<char> value = ReadNumber(true);
                        if (float.TryParse(value, CultureInfo.InvariantCulture, out float result))
                        {
                            if (djb2 == s_samplerMipLODBiasName)
                                tempData.MipLODBias = result;
                            else if (djb2 == s_samplerMinLODName)
                                tempData.MinLOD = result;
                            else
                                tempData.MaxLOD = result;
                        }
                        else
                        {
                            ReportErrorMessage(djb2 == s_samplerMipLODBiasName ? "Failed to parse mip lod bias value: {v}" : (djb2 == s_samplerMaxLODName ? "Failed to parse max lod value: {v}" : "Failed to parse min lod value: {v}"), value.ToString());
                        }
                    }
                    else if (djb2 == s_samplerBorderName)
                    {
                        if (Peek() != '"' || Peek() != '\'')
                        {
                            ReportErrorMessage("Expected string for enum value");
                            return;
                        }

                    _start = _index;
                        ReadOnlySpan<char> value = ReadString(Peek());
                        if (Enum.TryParse(value, false, out SamplerBorder filter))
                            tempData.Border = filter;
                        else
                        {
                            ReportErrorMessage("Failed to parse sampler border value: {v}", value.ToString());
                            return;
                        }
                    }
                }
            }

            _processor.AddStaticSampler(new StaticSamplerData(name.ToString(), attributes, tempData.Filter, tempData.AddressModeU, tempData.AddressModeV, tempData.AddressModeW, tempData.MaxAnisotropy, tempData.MipLODBias, tempData.MinLOD, tempData.MaxLOD, tempData.Border, new IndexRange(absoluteStart, _index)));
        }

        private void ParseAttribute()
        {
            int absoluteStart = _index - 1;
            _start = _index;

            ReadOnlySpan<char> attributeName = ReadGeneralIdentifier();
            AttributeSignature? signature = _processor.FindAttributeSignature(attributeName);

            if (signature == null)
            {
                ReportErrorMessage("Failed to find attribute with name {n} in current settings", attributeName.ToString());
                return;
            }

            _signatureReqSet.Clear();
            for (int i = 0; i < signature.Incompatible.Length; i++)
            {
                ref readonly AttributeRelation relation = ref signature.Incompatible[i];
                if (relation.Flags == AttributeRelationFlags.Required)
                {
                    _signatureReqSet.Add(relation.Type);
                }
            }

            Type currentType = signature.GetType();
            foreach (AttributeData data in _attributes)
            {
                if (data.Signature == signature)
                {
                    ReportErrorMessage("Attribute: {n} has already been defined", attributeName.ToString());
                    return;
                }

                for (int i = 0; i < data.Signature.Incompatible.Length; i++)
                {
                    ref readonly AttributeRelation relation = ref data.Signature.Incompatible[i];
                    if (relation.Flags == AttributeRelationFlags.Incompatible && relation.Type == currentType)
                    {
                        ReportErrorMessage("Current attribute: {n} is incompatible with previously defined attribute: {p}", attributeName.ToString(), relation.Type.Name);
                        return;
                    }
                }

                _signatureReqSet.Remove(data.Signature.GetType());
            }

            if (_signatureReqSet.Count > 0)
            {
                ReportErrorMessage("Current attribute: {n} requires missing attributes: {att}", attributeName.ToString(), _signatureReqSet.Select((x) => x.Name));
                return;
            }

            AttributeVarData[]? varDatas = null;
            if (FindAfterWhitespace('(', _index))
            {
                using RentedArray<AttributeVarData> tempArray = RentedArray<AttributeVarData>.Rent(signature.Signature.Length, true);
                tempArray.Span.Fill(new AttributeVarData(-1, null));

                ulong bitMask = 0;

                SkipUntil('(');
                Advance();

                int argumentIndex = 0;
                bool isExpectingSeparator = false;
                bool hasChangedArgIndex = false;

                while (Peek() != ')')
                {
                    if (argumentIndex == -1)
                    {
                        ReportErrorMessage("Too many arguments in attribute decleration");
                        return;
                    }

                    _start = _index;

                    char c = Advance();
                    if (isExpectingSeparator)
                    {
                        if (!char.IsWhiteSpace(c))
                        {
                            if (c != ',')
                            {
                                ReportErrorMessage("Expected separator after argument, got: {g}", Peek());
                                return;
                            }

                            isExpectingSeparator = false;

                            if (!hasChangedArgIndex)
                            {
                                if (argumentIndex < signature.Signature.Length)
                                {
                                    while (true)
                                    {
                                        argumentIndex++;

                                        if (argumentIndex >= signature.Signature.Length)
                                        {
                                            argumentIndex = -1;
                                            break;
                                        }

                                        if (((1ul << argumentIndex) & bitMask) == 0)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else
                                    argumentIndex = -1;
                            }
                        }
                    }
                    else
                    {
                        if (char.IsLetter(c))
                        {
                            ReadOnlySpan<char> identifier = ReadGeneralIdentifier();
                            if (identifier.Equals("null", StringComparison.Ordinal))
                            {
                                ref AttributeVariable varData = ref signature.Signature[argumentIndex];
                                bool isNullable = varData.Type == typeof(Nullable<>) || varData.Type == typeof(string);

                                if (isNullable)
                                {
                                    tempArray[argumentIndex] = new AttributeVarData(argumentIndex, null);
                                    bitMask |= 1ul << argumentIndex;

                                    isExpectingSeparator = true;
                                }
                            }
                            else
                            {
                                string findString = identifier.ToString();
                                int findIndex = signature.Signature.FindIndex((x) => x.Name == findString);

                                if (findIndex == -1)
                                {
                                    ReportErrorMessage("Attribute argument name variable found: {n}", findString);
                                    return;
                                }
                                if (((1ul << findIndex) & bitMask) > 0)
                                {
                                    ReportErrorMessage("Attribute argument {n} ({idx}) already provided", findString, findIndex);
                                    return;
                                }
                                if (!FindAfterWhitespace('=', _index))
                                {
                                    ReportErrorMessage("Expected value after attribute argument name: {n}", findString);
                                    return;
                                }

                                argumentIndex = findIndex;
                                hasChangedArgIndex = false;
                            }
                        }
                        else
                        {
                            if (char.IsNumber(c))
                            {
                                ref AttributeVariable varData = ref signature.Signature[argumentIndex];
                                bool isNullable = varData.Type == typeof(Nullable<>);

                                if (s_numericalConverters.TryGetValue(varData.Type, out Func<ReadOnlySpan<char>, object?>? method))
                                {
                                    bool isNull = false;
                                    if (varData.Type.IsAssignableTo(typeof(IFloatingPoint<>)))
                                    {
                                        ReadOnlySpan<char> number = ReadNumber(false);
                                        if (isNullable && Peek() == 'n')
                                        {
                                            ReadOnlySpan<char> identifier = ReadIdentifier();
                                            if (identifier.Equals("null", StringComparison.Ordinal))
                                                isNull = true;
                                            else
                                            {
                                                ReportErrorMessage("Expected valid keyword keyword: {kw}", identifier.ToString());
                                                return;
                                            }
                                        }

                                        object? value = isNull ? null : method(number);
                                        if (value != null || isNull)
                                        {
                                            tempArray[argumentIndex] = new AttributeVarData(argumentIndex, value);
                                            bitMask |= 1ul << argumentIndex;
                                        }
                                        else
                                        {
                                            ReportErrorMessage("Expected not null number");
                                            return;
                                        }
                                    }
                                    else if (varData.Type.IsAssignableTo(typeof(INumber<>)))
                                    {
                                        ReadOnlySpan<char> number = ReadNumber(true);
                                        if (isNullable && Peek() == 'n')
                                        {
                                            ReadOnlySpan<char> identifier = ReadIdentifier();
                                            if (identifier.Equals("null", StringComparison.Ordinal))
                                                isNull = true;
                                            else
                                            {
                                                ReportErrorMessage("Expected valid keyword keyword: {kw}", identifier.ToString());
                                                return;
                                            }
                                        }

                                        object? value = isNull ? null : method(number);
                                        if (value != null || isNull)
                                        {
                                            tempArray[argumentIndex] = new AttributeVarData(argumentIndex, value);
                                            bitMask |= 1ul << argumentIndex;
                                        }
                                        else
                                        {
                                            ReportErrorMessage("Expected not null number");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        using (BackupIndexState())
                                        {
                                            ReportErrorMessage("Expected: {t} type instead of: {str}", varData.Type, ReadGeneralIdentifier().ToString());
                                        }
                                        return;
                                    }

                                    isExpectingSeparator = true;
                                }
                                else
                                {
                                    ReportErrorMessage("No valid numerical converter found for type: {t}", varData.Type);
                                    return;
                                }
                            }
                            else if (c == '"' || c == '\'')
                            {
                                ref AttributeVariable varData = ref signature.Signature[argumentIndex];

                                _start = _index;
                                ReadOnlySpan<char> value = ReadString(c);

                                if (varData.Type == typeof(string))
                                    tempArray[argumentIndex] = new AttributeVarData(argumentIndex, value.ToString());
                                else if (varData.Type.IsAssignableTo(typeof(Enum)) && Enum.TryParse(varData.Type, value, out object? @enum))
                                    tempArray[argumentIndex] = new AttributeVarData(argumentIndex, @enum);
                                else
                                {
                                    ReportErrorMessage("Expected: {t} type instead of: {str}", varData.Type, value.ToString());
                                    return;
                                }

                                bitMask |= 1ul << argumentIndex;
                                isExpectingSeparator = true;
                            }
                        }
                    }

                    hasChangedArgIndex = false;
                }

                varDatas = tempArray.ToArray(0, signature.Signature.Length);
            }

            SkipUntil(']');

            if (varDatas == null)
            {
                varDatas = new AttributeVarData[signature.Signature.Length];
                Array.Fill(varDatas, new AttributeVarData(-1, null));
            }

            for (int i = 0; i < signature.Signature.Length; i++)
            {
                ref readonly AttributeVariable varSignature = ref signature.Signature[i];
                ref readonly AttributeVarData varData = ref varDatas[i];

                if (FlagUtility.HasFlag(varSignature.Flags, AttributeFlags.Required) && varData.SourceIndex == -1)
                {
                    ReportErrorMessage("Attribute required variable: {v} ({idx}) has not been provided", varSignature.Name, i);
                    return;
                }

                if (varData.SourceIndex == -1)
                {
                    varDatas[i] = new AttributeVarData(i, varSignature.Default);
                }
            }

            _attributes.Enqueue(new AttributeData(signature, varDatas, new IndexRange(absoluteStart, _index + 1)));
        }

        private void ParsePreprocessor()
        {
            _start = _index;
            ReadOnlySpan<char> directive = ReadIdentifier();

            if (directive.Equals("line", StringComparison.Ordinal))
            {
                SkipWhitespace();

                _start = _index;
                if (int.TryParse(ReadNumber(false), out int line))
                {
                    if (!FindAfterWhitespace('\n', _index))
                    {
                        SkipWhitespace();
                        if (Peek() == '"' || Peek() == '\'')
                        {
                            _start = _index;
                            _diagFile = ReadString(Advance()).ToString();
                        }
                    }

                    SkipUntil('\n');
                    Advance();

                    _diagLine = line;
                    _diagIndex = 0;
                    _diagStart = 0;
                }
            }
            else if (directive.Equals("pragma", StringComparison.Ordinal))
            {
                SkipWhitespace();

                _start = _index;
                ReadOnlySpan<char> pragma = ReadGeneralIdentifier();
                if (pragma.Equals("property_source", StringComparison.Ordinal))
                {
                    SkipWhitespace();

                    _start = _index;
                    ReadOnlySpan<char> sourceString = ReadString(Advance());

                    _processor.PropertySourceTemplate = sourceString.ToString();
                }
                else if (pragma.Equals("gen_properties_in_header", StringComparison.Ordinal))
                {
                    SkipWhitespace();

                    _start = _index;
                    ReadOnlySpan<char> enabledValue = ReadIdentifier();

                    _processor.GeneratePropertiesInHeader = (enabledValue.Equals("false", StringComparison.Ordinal) || enabledValue.Equals("0", StringComparison.Ordinal));
                }
            }
        }

        [UnscopedRef]
        private IndexBackupStorage BackupIndexState() => new IndexBackupStorage(ref this, _start, _index, _line, _diagStart, _diagIndex, _diagLine);

        private char Peek() => IsEOF ? '\0' : _source.DangerousGetReferenceAt(_index);
        private char PeekPrev() => _index == 0 ? '\0' : _source.DangerousGetReferenceAt(_index - 1);

        private char PeekNext()
        {
            int next = _index + 1;
            return next >= _source.Length ? '\0' : _source.DangerousGetReferenceAt(next);
        }

        private char Advance()
        {
            if (IsEOF)
                return '\0';

            Debug.Assert((uint)_index < _source.Length);
            char c = _source.DangerousGetReferenceAt(_index++);

            if (c == '\n')
            {
                _line++;

                _diagLine++;
                _diagIndex = 0;
            }

            return c;
        }

        private ReadOnlySpan<char> ReadIdentifier()
        {
            while (char.IsLetterOrDigit(Peek()))
            {
                Advance();
            }

            return _source.Slice(_start, _index - _start);
        }

        private ReadOnlySpan<char> ReadGeneralIdentifier()
        {
            while (!IsEOF)
            {
                char c = Peek();
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                    return _source.Slice(_start, _index - _start);

                Advance();
            }

            return ReadOnlySpan<char>.Empty;
        }

        private ReadOnlySpan<char> ReadString(char token = '"')
        {
            bool didStartWithToken = _source[_start] == token;
            if (Peek() == token && PeekPrev() != token)
            {
                Advance();
                didStartWithToken = true;
            }

            while (!IsEOF)
            {
                char c = Peek();
                if (c == token)
                {
                    Advance();

                    int start = didStartWithToken ? _start + 1 : _start;
                    return _source.Slice(start, _index - start - 1);
                }

                Advance();
            }

            return ReadOnlySpan<char>.Empty;
        }

        private ReadOnlySpan<char> ReadNumber(bool isFloating)
        {
            while (!IsEOF)
            {
                char c = Peek();
                if (!(char.IsDigit(c) || (isFloating && c == '.')))
                    return _source.Slice(_start, _index - _start);

                Advance();
            }

            return ReadOnlySpan<char>.Empty;
        }

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(Peek()))
            {
                Advance();
            }
        }

        private void SkipUntilNextLetter()
        {
            while (!char.IsLetter(Peek()))
            {
                Advance();
            }
        }

        private void SkipUntil(char c)
        {
            while (!IsEOF && Peek() != c)
            {
                if (Advance() == '/')
                    MoveUntilOutOfComment(ref _index);
            }
        }

        private IdentifierIntent DecodeIdentifierIntent(int absoluteStart, ReadOnlySpan<char> signature, ReadOnlySpan<char> name)
        {
            if (signature.Equals("struct", StringComparison.Ordinal))
            {
                return IdentifierIntent.Struct;
            }
            else if (signature.Equals("static", StringComparison.Ordinal) && name.Equals("SamplerState", StringComparison.Ordinal))
            {
                return IdentifierIntent.StaticSampler;
            }
            else if (FindAfterWhitespace('(') && Find(')'))
            {
                return IdentifierIntent.Function;
            }
            else if (s_resourceNameList.Contains(signature.GetDjb2HashCode()))
            {
                return IdentifierIntent.Resource;
            }
            else
            {
                int idx = signature.FindIndex(char.IsDigit);
                ReadOnlySpan<char> primitive = idx >= 0 ? signature.Slice(0, idx) : signature;

                if (s_primitiveNameList.Contains(primitive.GetDjb2HashCode()) || _processor.HasValueRef(primitive.GetDjb2HashCode()))
                {
                    return IdentifierIntent.Property;
                }
            }

            return IdentifierIntent.Unknown;
        }

        private ValueDataRef DecodeValueDataRefFromType(ReadOnlySpan<char> type)
        {
            ValueGeneric generic = ValueGeneric.Custom;
            int rows = 1;
            int columns = 1;

            int numIdx = type.FindIndex(char.IsDigit);

            ReadOnlySpan<char> sliced = numIdx == -1 ? type : type.Slice(0, numIdx);
            int djb2 = sliced.GetDjb2HashCode();

            int findIdx = Array.FindIndex(s_primitiveNameList, (x) => x == djb2);
            if (findIdx == -1)
            {
                int idx = _processor.FindRefIndexFor(type.GetDjb2HashCode());
                if (idx != -1)
                    return new ValueDataRef(ValueGeneric.Custom, idx);

                return ValueDataRef.Unspecified;
            }

            generic = (ValueGeneric)findIdx;

            if (numIdx != -1)
            {
                ReadOnlySpan<char> dimension = type.Slice(numIdx);
                rows = int.Parse(new ReadOnlySpan<char>(in dimension[0]));

                if (dimension.Length > 2)
                    columns = int.Parse(new ReadOnlySpan<char>(in dimension[2]));
            }

            Debug.Assert(rows >= 1 && rows <= 4);
            Debug.Assert(columns >= 1 && columns <= 4);

            return new ValueDataRef(generic, rows, columns);
        }

        private VarSemantic? AttemptDecodeSemantic(ReadOnlySpan<char> semantic)
        {
            int numIdx = semantic.FindIndex(char.IsDigit);

            ReadOnlySpan<char> general = numIdx == -1 ? semantic : semantic.Slice(0, numIdx);
            int djb2;

            unsafe
            {
                //TODO: add fallback if failure
                Span<char> temp = stackalloc char[general.Length];
                general.ToLowerInvariant(temp);
                djb2 = temp.GetDjb2HashCode();
            }

            int findIdx = Array.FindIndex(s_semanticNameList, (x) => x == djb2);
            if (findIdx == -1)
                return null;

            int index = 0;
            if (numIdx != -1)
                index = int.Parse(new ReadOnlySpan<char>(in semantic[numIdx]));

            return new VarSemantic((SemanticName)findIdx, index);
        }

        private bool ValidateAttributes(AttributeData[] attributes, AttributeUsage usage)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                ref AttributeData attribData = ref attributes[i];
                AttributeSignature signature = attribData.Signature;

                if (!FlagUtility.HasEither(signature.Usage, usage))
                {
                    ReportErrorMessage("Attribute: {n} is incompatible with usage: {u}", signature.Name, usage);
                    return false;
                }
            }

            return true;
        }

        private void ReportErrorMessage(string message, params object?[]? objects)
        {
            if (objects != null && objects.Length > 0)
                _processor.Logger.Error("[{f}:{l}] {i}: " + message, [_diagFile, _diagLine, _diagIndex, .. objects]);
            else
                _processor.Logger.Error("[{f}:{l}] {i}: " + message, _diagFile, _diagLine, _diagIndex);
        }

        private bool FindAfterWhitespace(char c, int startIndex = -1)
        {
            int index = startIndex != -1 ? startIndex : _index;
            while (char.IsWhiteSpace(_source[index]))
            {
                if (++index >= _source.Length)
                    return false;

                if (_source[index] == '/')
                {
                    MoveUntilOutOfComment(ref index);
                }
            }

            return _source[index] == c;
        }

        private bool Find(char c, int startIndex = -1)
        {
            int index = startIndex != -1 ? startIndex : _index;
            while (_source[index] != c)
            {
                if (++index >= _source.Length)
                    return false;

                if (_source[index] == '/')
                {
                    MoveUntilOutOfComment(ref index);
                }
            }

            return true;
        }

        private void MoveUntilOutOfComment(ref int index)
        {
            int followingIndex = index + 1;
            if (followingIndex >= _source.Length)
                return;

            char nextChar = _source[followingIndex];
            if (nextChar == '/')
            {
                index = followingIndex;
                do
                {
                    index++;
                } while (index < _source.Length && _source[index] != '\n');
            }
            else if (nextChar == '*')
            {
                index = followingIndex;
                do
                {
                    if (_source[index] == '*')
                    {
                        followingIndex = index + 1;
                        if (followingIndex < _source.Length && _source[followingIndex] == '/')
                            break;
                    }

                    index++;
                } while (index < _source.Length);
            }
        }

        private bool IsEOF => _source.Length <= _index;

        private static readonly int[] s_resourceNameList = [
            "Texture1D".GetDjb2HashCode(),
            "Texture2D".GetDjb2HashCode(),
            "Texture3D".GetDjb2HashCode(),
            "TextureCube".GetDjb2HashCode(),
            "ConstantBuffer".GetDjb2HashCode(),
            "StructuredBuffer".GetDjb2HashCode(),
            "ByteAddressBuffer".GetDjb2HashCode(),
            "SamplerState".GetDjb2HashCode()
            ];

        private static readonly int[] s_primitiveNameList = [
            "float".GetDjb2HashCode(),
            "double".GetDjb2HashCode(),
            "int".GetDjb2HashCode(),
            "uint".GetDjb2HashCode(),
            ];

        private static readonly int[] s_semanticNameList = [
            "position".GetDjb2HashCode(),
            "texcoord".GetDjb2HashCode(),
            "color".GetDjb2HashCode(),
            "normal".GetDjb2HashCode(),
            "tangent".GetDjb2HashCode(),
            "bitangent".GetDjb2HashCode(),
            "blendindices".GetDjb2HashCode(),
            "blendweight".GetDjb2HashCode(),
            "positiont".GetDjb2HashCode(),
            "psize".GetDjb2HashCode(),
            "fog".GetDjb2HashCode(),
            "tessfactor".GetDjb2HashCode(),
            "sv_instanceid".GetDjb2HashCode(),
            "sv_vertexid".GetDjb2HashCode(),
            "sv_position".GetDjb2HashCode()
            ];

        private static readonly FrozenDictionary<Type, Func<ReadOnlySpan<char>, object?>> s_numericalConverters = new Dictionary<Type, Func<ReadOnlySpan<char>, object?>>
        {
            { typeof(float), (x) => { if (float.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(double), (x) => { if (double.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(ulong), (x) => { if (ulong.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(long), (x) => { if (long.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(uint), (x) => { if (uint.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(int), (x) => { if (int.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(ushort), (x) => { if (ushort.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(short), (x) => { if (short.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(byte), (x) => { if (byte.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(sbyte), (x) => { if (sbyte.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },

            { typeof(float?), (x) => { if (float.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(double?), (x) => { if (double.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(ulong?), (x) => { if (ulong.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(long?), (x) => { if (long.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(uint?), (x) => { if (uint.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(int?), (x) => { if (int.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(ushort?), (x) => { if (ushort.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(short?), (x) => { if (short.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(byte?), (x) => { if (byte.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
            { typeof(sbyte?), (x) => { if (sbyte.TryParse(x, CultureInfo.InvariantCulture, out var v)) return v; return null; } },
        }.ToFrozenDictionary();

        private static readonly FrozenDictionary<int, SamplerTempData> s_presetSamplerStates = new Dictionary<int, SamplerTempData>
        {
            { "defaultLinear".GetDjb2HashCode(), new SamplerTempData() },
            { "defaultPoint".GetDjb2HashCode(), new SamplerTempData(SamplerFilter.Point) },
        }.ToFrozenDictionary();

        private static readonly int s_samplerFilterName = "Filter".GetDjb2HashCode();
        private static readonly int s_samplerAddressUName = "AddressU".GetDjb2HashCode();
        private static readonly int s_samplerAddressVName = "AddressV".GetDjb2HashCode();
        private static readonly int s_samplerAddressWName = "AddressW".GetDjb2HashCode();
        private static readonly int s_samplerMaxAnisotropyName = "MaxAnisotropy".GetDjb2HashCode();
        private static readonly int s_samplerMipLODBiasName = "MipLODBias".GetDjb2HashCode();
        private static readonly int s_samplerMinLODName = "MinLOD".GetDjb2HashCode();
        private static readonly int s_samplerMaxLODName = "MaxLOD".GetDjb2HashCode();
        private static readonly int s_samplerBorderName = "Border".GetDjb2HashCode();

        private ref struct IndexBackupStorage(ref SourceParser Parser, int Start, int Index, int Line, int DiagStart, int DiagIndex, int DiagLine) : IDisposable
        {
            private ref int _parserStart = ref Parser._start;
            private ref int _parserIndex = ref Parser._index;
            private ref int _parserLine = ref Parser._line;

            private ref int _parserDiagStart = ref Parser._diagStart;
            private ref int _parserDiagIndex = ref Parser._diagIndex;
            private ref int _parserDiagLine = ref Parser._diagLine;

            public readonly int Start { get; init; } = Start;
            public readonly int Index { get; init; } = Index;
            public readonly int Line { get; init; } = Line;

            public readonly int DiagStart { get; init; } = DiagStart;
            public readonly int DiagIndex { get; init; } = DiagIndex;
            public readonly int DiagLine { get; init; } = DiagLine;

            public void Dispose()
            {
                _parserStart = Start;
                _parserIndex = Index;
                _parserLine = Line;

                _parserDiagStart = DiagStart;
                _parserDiagIndex = DiagIndex;
                _parserDiagLine = DiagLine;
            }
        }

        private record struct SamplerTempData(
            SamplerFilter Filter = SamplerFilter.Linear,
            SamplerAddressMode AddressModeU = SamplerAddressMode.Repeat,
            SamplerAddressMode AddressModeV = SamplerAddressMode.Repeat,
            SamplerAddressMode AddressModeW = SamplerAddressMode.Repeat,
            uint MaxAnisotropy = 1,
            float MipLODBias = 2.0f,
            float MinLOD = 0.0f,
            float MaxLOD = float.MaxValue,
            SamplerBorder Border = SamplerBorder.TransparentBlack);

        private enum IdentifierIntent : byte
        {
            Unknown = 0,
            Function,
            Resource,
            Property,
            Struct,
            StaticSampler
        }

        private enum PrimitiveTypes : byte
        {
            Single = 0,
            Double,
            Int32,
            UInt32,
        }
    }
}
