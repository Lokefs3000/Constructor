using CommunityToolkit.HighPerformance;
using Primary.Common;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using RHI = Primary.RHI;

namespace Editor.Processors.Shaders
{
    internal static class ShaderSourceParser
    {
        public static ShaderParseResult ParseSource(ReadOnlySpan<char> source, string sourceSearchDir, string contentSearchDir)
        {
            ShaderParseResult result = new ShaderParseResult();

            using PoolArray<char> newSource = ArrayPool<char>.Shared.Rent(source.Length);
            source.CopyTo(newSource.AsSpan());

            HashSet<string> defines = new HashSet<string>();

            ParseIndividualSource(result, sourceSearchDir, contentSearchDir, newSource.AsSpan(0, source.Length), defines, true);

            result.OutputSource = newSource.AsSpan(0, source.Length).ToString();

            GenerateBindlessSource(result, out int offset);
            ConvertBindlessFunctions(result, offset);

            return result;
        }

        private static void ParseIndividualSource(ShaderParseResult result, string sourceSearchDir, string contentSearchDir, Span<char> source, HashSet<string> defines, bool isSourceFile)
        {
            int current = 0;

            Queue<Attrib> attribs = new Queue<Attrib>();
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            List<ShaderStructVariable> structVars = new List<ShaderStructVariable>();

            while (current < source.Length && current > -1)
            {
                char c = source[current];

                if (c == '#')
                {
                    current++;

                    ReadOnlySpan<char> identifier = ReadIdentifier(ref current, source);
                    if (identifier.ToString() == "include")
                    {
                        //bad
                        //output some legacy uh-oh warning message

                        SkipUntil(ref current, source, '"', true);
                        Span<char> @string = ReadString(ref current, source);

                        string sourceFile = ReadShaderSourceFile(@string.ToString(), sourceSearchDir, contentSearchDir, "");

                        using PoolArray<char> newSource = ArrayPool<char>.Shared.Rent(sourceFile.Length);
                        sourceFile.CopyTo(newSource.Array);

                        ParseIndividualSource(result, sourceSearchDir, contentSearchDir, newSource.AsSpan(0, sourceFile.Length), defines, false);
                    }
                    else if (identifier.ToString() == "pragma")
                    {
                        int start = current;
                        current++;

                        ReadOnlySpan<char> pragmaSwitch = ReadIdentifier(ref current, source);
                        if (pragmaSwitch.ToString() == "path")
                        {
                            if (!isSourceFile)
                                throw new ArgumentException("path can only be set in source");
                            if (result.Path != null)
                                throw new ArgumentException("path already set");

                            SkipUntil(ref current, source, '"', true);
                            Span<char> @string = ReadString(ref current, source);

                            result.Path = @string.ToString();

                            RemoveRestOfLine(ref start, source);
                            current = start;
                        }
                        else if (pragmaSwitch.ToString() == "bindgroup")
                        {
                            SkipUntil(ref current, source, '"', true);
                            Span<char> @string = ReadString(ref current, source);

                            string group = @string.ToString();
                            if (result.BindGroups.Contains(group))
                                throw new ArgumentException("group already exists " + group);

                            result.BindGroups.Add(group);

                            RemoveRestOfLine(ref start, source);
                            current = start;
                        }
                    }
                }
                else if (c == '[')
                {
                    int start = current;
                    current++;

                    Span<char> identifier = ReadIdentifier(ref current, source);
                    string identifierString = identifier.ToString();

                    switch (identifierString)
                    {
                        case "vertex":
                            {
                                EnsureNoArguments(ref current, source);

                                attribs.Enqueue(new Attrib
                                {
                                    Type = AttribType.Vertex,
                                });
                                break;
                            }
                        case "pixel":
                            {
                                EnsureNoArguments(ref current, source);

                                attribs.Enqueue(new Attrib
                                {
                                    Type = AttribType.Pixel,
                                });
                                break;
                            }
                        case "constants":
                            {
                                EnsureNoArguments(ref current, source);

                                attribs.Enqueue(new Attrib
                                {
                                    Type = AttribType.Constants,
                                });
                                break;
                            }
                        case "ialayout":
                            {
                                SearchForArguments(ref current, source, arguments);

                                if (!arguments.ContainsKey("Name"))
                                {
                                    throw new ArgumentException("Name");
                                }

                                AttributeIALayout attribute = new AttributeIALayout();
                                foreach (var kvp in arguments)
                                {
                                    string key = kvp.Key;
                                    switch (key)
                                    {
                                        case "Name":
                                            {
                                                attribute.Name = kvp.Value;
                                                break;
                                            }
                                        case "Offset":
                                            {
                                                attribute.Offset = int.Parse(kvp.Value);
                                                break;
                                            }
                                        case "Slot":
                                            {
                                                attribute.Slot = int.Parse(kvp.Value);
                                                break;
                                            }
                                        case "Class":
                                            {
                                                attribute.Class = Enum.Parse<RHI.InputClassification>(kvp.Value);
                                                break;
                                            }
                                        case "Format":
                                            {
                                                attribute.Format = Enum.Parse<RHI.InputElementFormat>(kvp.Value);
                                                break;
                                            }
                                        default: throw new ArgumentException(key);
                                    }
                                }

                                attribs.Enqueue(new Attrib
                                {
                                    Type = AttribType.IALayout,
                                    Value = attribute
                                });
                                break;
                            }
                        case "property":
                            {
                                SearchForArguments(ref current, source, arguments);

                                if (!arguments.ContainsKey("Name"))
                                {
                                    throw new ArgumentException("Name");
                                }

                                AttributeProperty attribute = new AttributeProperty();
                                foreach (var kvp in arguments)
                                {
                                    string key = kvp.Key;
                                    switch (key)
                                    {
                                        case "Name":
                                            {
                                                attribute.Name = kvp.Value;
                                                break;
                                            }
                                        default: throw new ArgumentException(key);
                                    }
                                }

                                attribs.Enqueue(new Attrib
                                {
                                    Type = AttribType.Property,
                                    Value = attribute
                                });
                                break;
                            }
                        case "bindgroup":
                            {
                                SearchForArguments(ref current, source, arguments);

                                AttributeBindGroup attribute = new AttributeBindGroup { Group = "__Default" };
                                foreach (var kvp in arguments)
                                {
                                    string key = kvp.Key;
                                    switch (key)
                                    {
                                        case "Group":
                                            {
                                                attribute.Group = kvp.Value;
                                                break;
                                            }
                                        default: throw new ArgumentException(key);
                                    }
                                }

                                if (attribute.Group != "__Default")
                                {
                                    if (!result.BindGroups.Contains(attribute.Group))
                                    {
                                        throw new Exception("no group " + attribute.Group);
                                    }

                                    attribs.Enqueue(new Attrib
                                    {
                                        Type = AttribType.BindGroup,
                                        Value = attribute
                                    });
                                }

                                break;
                            }
                        default: throw new Exception(identifierString);
                    }

                    RemoveSectionOfLine(start, current - start + 1, source);
                }
                else if (char.IsLetter(source[current]))
                {
                    int start = current;

                    Span<char> identifier = ReadIdentifier(ref current, source, false);
                    string keyword = identifier.ToString();

                    if (ResourceTypes.Contains(keyword))
                    {
                        string signature = keyword;
                        string? varName = null;

                        if (source[current] == '<')
                        {
                            current++;

                            identifier = ReadIdentifier(ref current, source);
                            varName = identifier.ToString();

                            signature += $"<{varName}>";

                            if (source[current] != '>')
                                throw new ArgumentException(">");

                            current++;
                        }

                        ShaderAttribute[] attribsArray = attribs.Count > 0 ? new ShaderAttribute[attribs.Count((x) => x.Type != AttribType.BindGroup)] : Array.Empty<ShaderAttribute>();

                        string bindGroup = "__Default";

                        int index = 0;
                        while (attribs.TryDequeue(out Attrib attrib))
                        {
                            if (!IsAttributeValidOnVariable(attrib.Type, keyword))
                                throw new NotSupportedException(attrib.Type.ToString());

                            if (attrib.Type == AttribType.BindGroup)
                            {
                                bindGroup = ((AttributeBindGroup)attrib.Value).Group;
                            }
                            else
                            {
                                attribsArray[index++] = ConvertAttribute(ref attrib);
                            }
                        }

                        current++;

                        result.Variables.Add(new ShaderVariable
                        {
                            Type = Enum.Parse<ShaderVariableType>(keyword),
                            Signature = signature,
                            VariableName = varName,
                            Name = ReadIdentifier(ref current, source).ToString(),
                            BindGroup = bindGroup,
                            Index = result.Variables.Count,
                            Attributes = attribsArray
                        });

                        RemoveRestOfLine(ref start, source);
                        current = Math.Max(current, start);
                    }
                    else if (keyword == "SamplerState")
                    {
                        if (FindCharacter(current, source, ':', false))
                        {
                            current++;
                            ReadOnlySpan<char> name = ReadIdentifier(ref current, source);

                            int namePost = current;

                            while (current < source.Length && !char.IsLetter(source[current]))
                                current++;
                            if (current == source.Length)
                                throw new ArgumentException("EOF");

                            ReadOnlySpan<char> defaultState = ReadIdentifier(ref current, source);
                            if (defaultState.IsEmpty)
                                throw new ArgumentException("no sampler decl");

                            string dsString = defaultState.ToString();
                            if (dsString != "register")
                            {
                                ImmutableSampler sampler = PresetSamplerTypes[dsString];
                                sampler.Index = result.ImmutableSamplers.Count;
                                sampler.Name = name.ToString();

                                //start = namePost;

                                bool iterateData = false;
                                int temporary = current;
                                while (temporary < source.Length && (char.IsWhiteSpace(source[temporary]) || char.IsControl(source[temporary]) || source[temporary] == '{'))
                                {
                                    if (source[temporary] == '{')
                                    {
                                        iterateData = true;
                                        break;
                                    }
                                    else if (source[temporary] == ';')
                                    {
                                        break;
                                    }

                                    temporary++;
                                }

                                if (iterateData)
                                {
                                    while (current < source.Length && source[current] != '}')
                                    {
                                        if (!char.IsLetter(source[current]))
                                        {
                                            current++;
                                            continue;
                                        }

                                        ReadOnlySpan<char> decleration = ReadIdentifier(ref current, source);
                                        if (decleration.IsEmpty)
                                            throw new ArgumentException("bad sampler decleration");

                                        bool hasEqualsSign = false;
                                        while (current < source.Length && (char.IsWhiteSpace(source[current]) || source[current] == '='))
                                        {
                                            current++;

                                            if (source[current] == '=')
                                                hasEqualsSign = true;
                                        }

                                        if (!hasEqualsSign)
                                            throw new ArgumentException("no sampler decleration value");
                                        if (current == source.Length)
                                            throw new ArgumentException("EOF");

                                        string declStr = decleration.ToString();
                                        switch (declStr)
                                        {
                                            case "Filter": sampler.Filter = Enum.Parse<RHI.TextureFilter>(ReadString(ref current, source)); break;
                                            case "AddressModeU": sampler.AddressModeU = Enum.Parse<RHI.TextureAddressMode>(ReadString(ref current, source)); break;
                                            case "AddressModeV": sampler.AddressModeV = Enum.Parse<RHI.TextureAddressMode>(ReadString(ref current, source)); break;
                                            case "AddressModeW": sampler.AddressModeW = Enum.Parse<RHI.TextureAddressMode>(ReadString(ref current, source)); break;
                                            case "MaxAnistropy": sampler.MaxAnistropy = Math.Clamp(uint.Parse(ReadIdentifier(ref current, source)), 0, 16u); break;
                                            case "MipLODBias": sampler.MipLODBias = MathF.Max(float.Parse(ReadIdentifier(ref current, source)), 0.0f); break;
                                            case "MinLOD": sampler.MinLOD = MathF.Max(float.Parse(ReadIdentifier(ref current, source)), 0.0f); break;
                                            case "MaxLOD": sampler.MaxLOD = MathF.Max(float.Parse(ReadIdentifier(ref current, source)), 0.0f); break;
                                            default: throw new ArgumentException("unknown sampler decleration: " + declStr);
                                        }

                                        SkipWhitespace(ref current, source, true);

                                        if (source[current] != ',')
                                        {
                                            bool foundEOFBeforeBad = false;

                                            temporary = current;
                                            while (temporary < source.Length)
                                            {
                                                bool isUselesss = char.IsWhiteSpace(source[temporary]) || char.IsControl(source[temporary]);
                                                if (!isUselesss)
                                                {
                                                    if (source[temporary] == '}')
                                                    {
                                                        foundEOFBeforeBad = true;
                                                        break;
                                                    }
                                                }

                                                temporary++;
                                            }

                                            if (!foundEOFBeforeBad)
                                                throw new ArgumentException("no sampler end splitter");
                                        }
                                    }

                                    if (current == source.Length)
                                        throw new ArgumentException("EOF");
                                }

                                current++;
                                RemoveSectionOfSource(start, current - start, source);

                                result.ImmutableSamplers.Add(sampler);
                            }
                        }
                    }
                    else if (CheckIsFunction(keyword, current, source))
                    {
                        ShaderAttribute[] attribsArray = attribs.Count > 0 ? new ShaderAttribute[attribs.Count] : Array.Empty<ShaderAttribute>();

                        while (attribs.TryDequeue(out Attrib attrib))
                        {
                            if (!IsAttributeValidOnFunction(attrib.Type, result, keyword))
                                throw new NotSupportedException(attrib.Type.ToString());

                            switch (attrib.Type)
                            {
                                case AttribType.Vertex: result.EntryPointVertex = keyword; break;
                                case AttribType.Pixel: result.EntryPointPixel = keyword; break;
                                case AttribType.IALayout:
                                    {
                                        AttributeIALayout value = (AttributeIALayout)attrib.Value;
                                        result.InputLayout.Add(new ShaderInputLayout
                                        {
                                            Name = value.Name,
                                            Offset = value.Offset,
                                            Slot = value.Slot,
                                            Class = value.Class,
                                            Format = value.Format,
                                        });

                                        break;
                                    }
                                default: break;
                            }
                        }

                        int functionDepth = 1;

                        SkipUntil(ref current, source, '{', false);
                        current++;

                        int bodyStart = current;

                        while (current < source.Length && functionDepth > 0)
                        {
                            if (source[current] == '{')
                                functionDepth++;
                            else if (source[current] == '}')
                                functionDepth--;

                            current++;
                        }

                        result.Functions.Add(new ShaderFunction
                        {
                            Name = keyword,
                            BodyBegin = bodyStart,
                            BodyEnd = current - 1
                        });
                    }
                    else if (keyword == "struct")
                    {
                        start = current - 6;

                        SkipWhitespace(ref current, source, true);

                        identifier = ReadIdentifier(ref current, source);
                        string structName = identifier.IsEmpty ? $"Struct{result.Structs.Count}" : identifier.ToString();

                        SkipWhitespace(ref current, source, true);

                        structVars.Clear();

                        do
                        {
                            identifier = ReadLetters(ref current, source);
                            if (Enum.TryParse(identifier, true, out ShaderStructVariableType componentType))
                            {
                                byte rows = 1;
                                byte columns = 1;

                                switch (componentType)
                                {
                                    case ShaderStructVariableType.Bool:
                                    case ShaderStructVariableType.Int:
                                    case ShaderStructVariableType.UInt:
                                    case ShaderStructVariableType.DWord:
                                    case ShaderStructVariableType.Half:
                                    case ShaderStructVariableType.Float:
                                    case ShaderStructVariableType.Double:
                                    case ShaderStructVariableType.UInt64:
                                    case ShaderStructVariableType.Int64:
                                    case ShaderStructVariableType.Float16:
                                    case ShaderStructVariableType.UInt16:
                                    case ShaderStructVariableType.Int16:
                                        {
                                            if (char.IsNumber(source[current]))
                                            {
                                                rows = byte.Parse(source.Slice(current, 1));
                                                current++;

                                                if (source[current++] == 'x')
                                                {
                                                    columns = byte.Parse(source.Slice(current++, 1));
                                                }
                                            }

                                            break;
                                        }
                                    case ShaderStructVariableType.Matrix:
                                        {
                                            SkipWhitespace(ref current, source, true);
                                            if (source[current] == '<')
                                            {
                                                identifier = ReadIdentifier(ref current, source);
                                                if (identifier.IsEmpty)
                                                    throw new Exception();

                                                componentType = Enum.Parse<ShaderStructVariableType>(identifier);

                                                SkipWhitespace(ref current, source, true);
                                                if (source[current] == ',')
                                                {
                                                    SkipWhitespace(ref current, source, true);
                                                    rows = byte.Parse(source.Slice(current, 1));

                                                    SkipWhitespace(ref current, source, true);
                                                    if (source[current] == ',')
                                                    {
                                                        SkipWhitespace(ref current, source, true);
                                                        columns = byte.Parse(source.Slice(current, 1));
                                                        SkipWhitespace(ref current, source, true);
                                                    }
                                                }

                                                if (source[current] != '>')
                                                    throw new Exception("matrix");
                                            }
                                            else
                                            {
                                                componentType = ShaderStructVariableType.Float;
                                                rows = 4;
                                                columns = 4;
                                            }

                                            break;
                                        }
                                }

                                SkipWhitespace(ref current, source, true);
                                identifier = ReadIdentifier(ref current, source);

                                if (identifier.IsEmpty)
                                    throw new ArgumentException("name");

                                structVars.Add(new ShaderStructVariable
                                {
                                    Type = componentType,
                                    Rows = rows,
                                    Columns = columns,
                                    Name = identifier.ToString()
                                });

                                SkipUntil(ref current, source, ';', false);
                            }

                            int whitespaceStart = current;
                            SkipWhitespace(ref current, source, true);

                            if (current == whitespaceStart)
                                current++;
                        } while (current < source.Length && source[current] != '}');

                        int idx = current;
                        SkipUntil(ref idx, source, ' ', false);

                        result.Structs.Add(structName, new ShaderStruct
                        {
                            Name = structName,
                            DefBegin = start,
                            DefEnd = idx,
                            Variables = structVars.ToArray()
                        });
                    }
                }

                current++;
            }

            if (attribs.Count > 0)
                throw new ArgumentException("unresolved attribs");
        }

        private static void ConvertBindlessFunctions(ShaderParseResult result, int offset)
        {
            StringBuilder sb = new StringBuilder();

            Dictionary<string, ShaderVariable> fullVariables = result.Variables.ToDictionary((x) => x.Name);
            HashSet<string> foundVariables = new HashSet<string>();

            int sourceIndexOffset = offset;
            for (int i = 0; i < result.Functions.Count; i++)
            {
                ShaderFunction function = result.Functions[i];

                ScanFunctionForVariables(fullVariables, foundVariables, result.OutputSource.AsSpan(), function.BodyBegin + sourceIndexOffset, function.BodyEnd + sourceIndexOffset);

                if (foundVariables.Count > 0)
                {
                    sb.Clear();

                    sb.Append(' ');
                    foreach (string variable in foundVariables)
                    {
                        ShaderVariable var = fullVariables[variable];
                        if (var.IsConstants)
                            continue;

                        sb.Append(var.Signature);
                        sb.Append(' ');
                        sb.Append(var.Name);
                        sb.Append(" = ");
                        sb.Append($"({var.Signature})ResourceDescriptorHeap[__BINDLESS_CB.IDX_{var.Name}]; ");
                    }

                    result.OutputSource = result.OutputSource.Insert(function.BodyBegin + sourceIndexOffset, sb.ToString());
                    sourceIndexOffset += sb.Length;
                }
            }
        }

        private static void ScanFunctionForVariables(Dictionary<string, ShaderVariable> existing, HashSet<string> found, ReadOnlySpan<char> source, int start, int end)
        {
            found.Clear();

            ReadOnlySpan<char> interestingPortion = source.Slice(start, end - start);

            foreach (var kvp in existing)
            {
                string variable = kvp.Key;

                int index = interestingPortion.IndexOf(variable, StringComparison.InvariantCulture);
                ReadOnlySpan<char> subsection = interestingPortion;

                while (index > -1)
                {
                    subsection = subsection.Slice(index);
                    index = 0;

                    if (variable.Length == subsection.Length || !char.IsLetterOrDigit(subsection[variable.Length]))
                    {
                        found.Add(variable);
                    }

                    subsection = subsection.Slice(1);
                    index = subsection.IndexOf(variable, StringComparison.InvariantCulture);
                }
            }
        }

        private static void GenerateBindlessSource(ShaderParseResult result, out int offset)
        {
            if (result.Variables.Count == 0)
            {
                offset = 0;
                return;
            }

            StringBuilder sb = new StringBuilder();

            using PoolArray<char> pooledString = ArrayPool<char>.Shared.Rent(result.OutputSource.Length);
            result.OutputSource.CopyTo(pooledString.AsSpan());

            Span<ShaderVariable> variables = result.Variables.AsSpan();

            bool hasUserConstants = result.Variables.Exists((x) => x.VariableName != null && result.Structs.ContainsKey(x.VariableName) && x.IsConstants);
            byte userConstantsSize = 0;

            if (hasUserConstants)
            {
                for (int i = 0; i < variables.Length; i++)
                {
                    ref ShaderVariable variable = ref variables[i];
                    if (variable.VariableName != null && variable.IsConstants)
                    {
                        ref ShaderStruct @struct = ref CollectionsMarshal.GetValueRefOrNullRef(result.Structs, variable.VariableName);
                        if (!Unsafe.IsNullRef(ref @struct))
                        {
                            int dataSize = CalculateStructSize(ref @struct);
                            if (userConstantsSize + dataSize > 128)
                                throw new Exception("constants overflow >128b");

                            userConstantsSize += (byte)dataSize;

                            sb.Append("struct ");
                            sb.AppendLine(@struct.Name);
                            sb.AppendLine("{");

                            for (int j = 0; j < @struct.Variables.Length; j++)
                            {
                                ref ShaderStructVariable structVar = ref @struct.Variables[j];

                                sb.Append("    ");
                                sb.Append(structVar.Type.ToString().ToLowerInvariant());

                                if (structVar.Rows > 1)
                                    sb.Append(structVar.Rows);
                                if (structVar.Columns > 1)
                                {
                                    sb.Append('x');
                                    sb.Append(structVar.Columns);
                                }

                                sb.Append(' ');
                                sb.Append(@structVar.Name);
                                sb.AppendLine(";");
                            }

                            sb.AppendLine("};");

                            for (int j = @struct.DefBegin; j < @struct.DefEnd; j++)
                            {
                                if (!char.IsControl(pooledString[j]))
                                    pooledString[j] = ' ';
                            }
                        }
                    }
                }
            }

            hasUserConstants = userConstantsSize > 0;
            result.ConstantsSize = userConstantsSize;

            if (hasUserConstants && userConstantsSize > 0)
            {
                sb.AppendLine("struct __USER_CONSTANTS_GENERATED");
                sb.AppendLine("{");

                for (int i = 0; i < variables.Length; i++)
                {
                    ref ShaderVariable variable = ref variables[i];
                    if (variable.VariableName != null && result.Structs.ContainsKey(variable.VariableName) && variable.IsConstants)
                    {
                        sb.Append("    ");
                        sb.Append(variable.VariableName);
                        sb.AppendLine($" USER_{variable.Name};");
                    }
                }

                sb.AppendLine("};");
            }

            bool hasSizeForVkPush = (result.Variables.Count * 4 + userConstantsSize) < 129;
            int linesInGenerated = 3 + (hasSizeForVkPush ? 2 : 1) + result.Variables.Count * 2;

            sb.AppendLine("struct __BINDLESS_GENERATED");
            sb.AppendLine("{");

            SortShaderBindGroups(result);

            if (hasSizeForVkPush && hasUserConstants)
            {
                sb.AppendLine("    __USER_CONSTANTS_GENERATED USER_ConstantsData;");
            }

            for (int i = 0; i < variables.Length; i++)
            {
                ref ShaderVariable variable = ref variables[i];
                if (!variable.IsConstants)
                    sb.AppendLine($"    uint IDX_{variable.Name}; //{variable.BindGroup}:{variable.Index}");
            }

            sb.AppendLine("};");

            if (hasSizeForVkPush)
            {
                sb.AppendLine("#ifdef __spirv__");
                sb.AppendLine("[[vk::push_constant]]");
                sb.AppendLine("#endif");
                sb.AppendLine("ConstantBuffer<__BINDLESS_GENERATED> __BINDLESS_CB : register(b0);");
            }
            else
            {
                sb.AppendLine("ConstantBuffer<__BINDLESS_GENERATED> __BINDLESS_CB : register(b0);");
            }

            if (hasUserConstants)
            {
                if (!hasSizeForVkPush)
                {
                    sb.AppendLine("#ifdef __spirv__");
                    sb.AppendLine("[[vk::push_constant]]");
                    sb.AppendLine("#endif");
                    sb.AppendLine("ConstantBuffer<__USER_CONSTANTS_GENERATED> __USER_CONSTANTS_CB : register(b1);");

                    for (int i = 0; i < variables.Length; i++)
                    {
                        ref ShaderVariable variable = ref variables[i];
                        if (variable.VariableName != null && result.Structs.ContainsKey(variable.VariableName) && variable.IsConstants)
                        {
                            sb.Append("#define ");
                            sb.Append(variable.Name);
                            sb.Append($" __USER_CONSTANTS_CB.USER_");
                            sb.Append(variable.Name);
                            sb.AppendLine();
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < variables.Length; i++)
                    {
                        ref ShaderVariable variable = ref variables[i];
                        if (variable.VariableName != null && result.Structs.ContainsKey(variable.VariableName) && variable.IsConstants)
                        {
                            sb.Append("#define ");
                            sb.Append(variable.Name);
                            sb.Append($" __BINDLESS_CB.USER_ConstantsData.USER_");
                            sb.Append(variable.Name);
                            sb.AppendLine();
                        }
                    }
                }
            }

            /*for (int i = 0; i < result.Variables.Count; i++)
            {
                ShaderVariable variable = result.Variables[i];
                sb.AppendLine($"#define {variable.Name} (({variable.Signature})ResourceDescriptorHeap[__BINDLESS_CB.IDX_{variable.Name}])");
            }*/

            for (int i = 0; i < result.ImmutableSamplers.Count; i++)
            {
                ImmutableSampler sampler = result.ImmutableSamplers[i];
                sb.AppendLine($"SamplerState {sampler.Name} : register(s{sampler.Index});");
            }

            sb.AppendLine($"#line {0}");
            offset = sb.Length;

            result.OutputSource = sb.ToString() + pooledString.AsSpan(0, result.OutputSource.Length).ToString();
        }

        private static void SortShaderBindGroups(ShaderParseResult result)
        {
            using PoolArray<ShaderVariable> variables = ArrayPool<ShaderVariable>.Shared.Rent(result.Variables.Count);
            result.Variables.CopyTo(variables.Array);

            Span<ShaderVariable> span = variables.AsSpan();

            result.Variables.Clear();
            foreach (string bindGroup in result.BindGroups)
            {
                int start = result.Variables.Count;

                for (int i = 0; i < span.Length; i++)
                {
                    ref ShaderVariable var = ref span[i];
                    if (var.BindGroup == bindGroup)
                    {
                        result.Variables.Add(var);
                    }
                }

                result.Variables.Sort(start, result.Variables.Count - start, new VarBindGroupComparer());
            }

            span = result.Variables.AsSpan();

            int k = 0;
            for (int i = 0; i < span.Length; i++)
            {
                ref ShaderVariable svar = ref span[i];
                if (svar.IsConstants)
                    svar.Index = int.MaxValue;
                else
                    span[i].Index = k++;
            }
        }

        private static int CalculateStructSize(ref ShaderStruct @struct)
        {
            int size = 0;
            for (int i = 0; i < @struct.Variables.Length; i++)
            {
                ref ShaderStructVariable variable = ref @struct.Variables[i];
                size += StructVariableSizes[(int)variable.Type] * (variable.Rows * variable.Columns);
            }

            return size;
        }

        private static bool CheckIsFunction(string name, int current, Span<char> source)
        {
            if (source[current] == '(' && FindCharacter(current, source, ')', false))
            {
                int searchIndex = current - name.Length - 1;

                if (char.IsWhiteSpace(source[searchIndex]))
                {
                    do
                    {
                        searchIndex--;
                    } while (char.IsWhiteSpace(source[searchIndex]));
                }

                do
                {
                    searchIndex--;
                } while (searchIndex > 0 && char.IsLetterOrDigit(source[searchIndex]));

                int start = searchIndex;

                searchIndex++;
                ReadOnlySpan<char> identifier = ReadIdentifier(ref searchIndex, source);

                if (!identifier.IsEmpty)
                    return true;
            }

            return false;
        }

        private static Span<char> ReadIdentifier(ref int current, Span<char> identifier, bool throwAtEOF = true, bool includeSymbols = false)
        {
            if (includeSymbols)
                throw new NotSupportedException();

            int start = current;
            while (current < identifier.Length)
            {
                if (!char.IsLetterOrDigit(identifier[current]) && identifier[current] != '_')
                    break;
                current++;
            }

            if (current == identifier.Length && throwAtEOF)
                throw new IndexOutOfRangeException("EOF");

            return identifier.Slice(start, current - start);
        }

        private static Span<char> ReadLetters(ref int current, Span<char> identifier, bool throwAtEOF = true)
        {
            int start = current;
            while (current < identifier.Length)
            {
                if (!char.IsLetter(identifier[current]))
                    break;
                current++;
            }

            if (current == identifier.Length && throwAtEOF)
                throw new IndexOutOfRangeException("EOF");

            return identifier.Slice(start, current - start);
        }

        private static Span<char> ReadString(ref int current, Span<char> identifier, bool throwAtEOF = true)
        {
            int start = current;
            while (current < identifier.Length)
            {
                if (char.IsControl(identifier[current]))
                    break;
                current++;
            }

            if (current == identifier.Length && throwAtEOF)
                throw new IndexOutOfRangeException("EOF");

            return identifier.Slice(start, current - start);
        }

        private static void SkipUntil(ref int index, Span<char> source, char ch, bool throwAtNewLine)
        {
            while (index < source.Length)
            {
                if (source[index] == '\n' && throwAtNewLine)
                    throw new ArgumentException();

                if (source[index] == ch)
                    return;

                index++;
            }

            throw new IndexOutOfRangeException("EOF");
        }

        private static Span<char> ReadString(ref int index, Span<char> source)
        {
            int start = index;

            if (source[index] != '"')
                throw new ArgumentException();

            index++;
            while (index < source.Length && source[index] != '"')
            {

                index++;
            }

            if (index == source.Length)
                throw new IndexOutOfRangeException("EOF");

            index++;
            return source.Slice(start + 1, index - start - 2);
        }

        private static void SkipWhitespace(ref int current, Span<char> source, bool skipNewLine)
        {
            if (!char.IsWhiteSpace(source[current]))
                return;

            do
            {
                current++;
            } while (current < source.Length && (char.IsWhiteSpace(source[current]) || (skipNewLine ? source[current] == '\n' : false)));
        }

        private static void SkipUntilNumber(ref int current, Span<char> source)
        {
            if (char.IsNumber(source[current]))
                return;

            do
            {
                current++;
            } while (current < source.Length && !char.IsNumber(source[current]));

            if (current == source.Length)
                throw new ArgumentException("EOF");
        }

        private static string ReadShaderSourceFile(string sourceFileName, params string[] searchDirs)
        {
            for (int i = 0; i < searchDirs.Length; i++)
            {
                string dir = searchDirs[i];
                string combined = dir == string.Empty ? sourceFileName : Path.Combine(searchDirs[i], sourceFileName);

                if (File.Exists(combined))
                {
                    return File.ReadAllText(combined);
                }
            }

            throw new FileNotFoundException(sourceFileName);
        }

        private static void EnsureNoArguments(ref int current, Span<char> source)
        {
            if (source[current] == '(')
            {
                current++;

                if (source[current] != ')')
                {
                    throw new ArgumentException("args");
                }

                current++;
            }
        }

        private static void SearchForArguments(ref int current, Span<char> source, Dictionary<string, string> arguments)
        {
            if (source[current] != '(')
                throw new ArgumentException("no args");

            arguments.Clear();

            current++;
            while (current < source.Length && source[current] != ')')
            {
                if (current == source.Length)
                    throw new Exception("EOF");

                Span<char> identifier = ReadIdentifier(ref current, source);
                if (identifier.IsEmpty)
                    throw new ArgumentException("no name");

                bool hasEqualsSign = false;
                while (current < source.Length && (char.IsWhiteSpace(source[current]) || source[current] == '='))
                {
                    if (source[current] == '=')
                        hasEqualsSign = true;
                    current++;
                }

                if (!hasEqualsSign)
                    throw new ArgumentException("no eq");

                Span<char> value = (source[current] == '"' || source[current] == '\'') ? ReadString(ref current, source) : ReadIdentifier(ref current, source);
                if (value.IsEmpty)
                    throw new ArgumentException("no val");

                arguments.Add(identifier.ToString(), value.ToString());

                bool hasSeperator = false;
                while (current < source.Length && !char.IsLetter(source[current]))
                {
                    if (source[current] == ',')
                        hasSeperator = true;
                    if (source[current] == ')')
                    {
                        hasSeperator = true;
                        break;
                    }
                    current++;
                }

                if (current == source.Length)
                    throw new Exception("EOF");

                if (!hasSeperator)
                {
                    throw new ArgumentException("no seperator");
                }
            }

            current++;
        }

        private static void RemoveRestOfLine(ref int current, Span<char> source)
        {
            while (current < source.Length && source[current] != '\n')
            {
                source[current++] = ' ';
            }
        }

        private static void RemoveSectionOfLine(int start, int length, Span<char> source)
        {
            int current = start;
            int maximum = Math.Min(source.Length, length + start);

            while (current < maximum && source[current] != '\n')
            {
                source[current++] = ' ';
            }
        }

        private static void RemoveSectionOfSource(int start, int length, Span<char> source)
        {
            int current = start;
            int maximum = Math.Min(source.Length, length + start);

            while (current < maximum)
            {
                if (!char.IsControl(source[current]))
                    source[current] = ' ';
                current++;
            }
        }

        private static char PeekNext(int current, Span<char> source)
        {
            int next = current + 1;

            if (next >= source.Length)
                return '\0';
            return source[next];
        }

        private static bool FindCharacter(int current, Span<char> source, char searchFor, bool keepReadingNextLine)
        {
            while (current < source.Length)
            {
                if (source[current] == searchFor)
                    return true;
                current++;
            }

            return false;
        }

        private static bool IsAttributeValidOnVariable(AttribType type, ReadOnlySpan<char> signature)
        {
            switch (type)
            {
                case AttribType.Vertex: return false;
                case AttribType.Pixel: return false;
                case AttribType.Constants: return signature.ToString() == "ConstantBuffer";
                case AttribType.IALayout: return false;
                case AttribType.Property: return signature.ToString() == "Texture2D";
                case AttribType.BindGroup: return true;
            }

            return false;
        }

        private static bool IsAttributeValidOnFunction(AttribType type, ShaderParseResult result, ReadOnlySpan<char> functionName)
        {
            switch (type)
            {
                case AttribType.Vertex: return result.EntryPointVertex == null;
                case AttribType.Pixel: return result.EntryPointPixel == null;
                case AttribType.Constants: return false;
                case AttribType.IALayout: return result.EntryPointVertex == functionName;
                case AttribType.Property: return false;
                case AttribType.BindGroup: return false;
            }

            return false;
        }

        private static ShaderAttribute ConvertAttribute(ref Attrib attrib)
        {
            switch (attrib.Type)
            {
                case AttribType.Constants: return new ShaderAttribute { Type = ShaderAttributeType.Constants, Value = null };
                case AttribType.Property: return new ShaderAttribute { Type = ShaderAttributeType.Property, Value = new ShaderAttribProperty { Name = ((AttributeProperty)attrib.Value).Name } };
            }

            throw new NotImplementedException(attrib.Type.ToString());
        }

        private static HashSet<string> ResourceTypes = new HashSet<string>
        {
            "ConstantBuffer",
            "StructuredBuffer",
            "RWStructuredBuffer",
            "Texture1D",
            "Texture1DArray",
            "Texture2D",
            "Texture2DArray",
            "Texture3D",
            "TextureCube",
            "TextureCubeArray",
        };

        private static Dictionary<string, ImmutableSampler> PresetSamplerTypes = new Dictionary<string, ImmutableSampler>
        {
            { "default", new ImmutableSampler
            {
                Filter = RHI.TextureFilter.Linear,
                AddressModeU = RHI.TextureAddressMode.Repeat,
                AddressModeV = RHI.TextureAddressMode.Repeat,
                AddressModeW = RHI.TextureAddressMode.Repeat,
                MaxAnistropy = 1,
                MipLODBias = 1.0f,
                MinLOD = 0.0f,
                MaxLOD = float.MaxValue
            } },
            { "defaultLinear", new ImmutableSampler
            {
                Filter = RHI.TextureFilter.Linear,
                AddressModeU = RHI.TextureAddressMode.Repeat,
                AddressModeV = RHI.TextureAddressMode.Repeat,
                AddressModeW = RHI.TextureAddressMode.Repeat,
                MaxAnistropy = 1,
                MipLODBias = 1.0f,
                MinLOD = 0.0f,
                MaxLOD = float.MaxValue
            } },
            { "defaultPoint", new ImmutableSampler
            {
                Filter = RHI.TextureFilter.Point,
                AddressModeU = RHI.TextureAddressMode.Repeat,
                AddressModeV = RHI.TextureAddressMode.Repeat,
                AddressModeW = RHI.TextureAddressMode.Repeat,
                MaxAnistropy = 1,
                MipLODBias = 1.0f,
                MinLOD = 0.0f,
                MaxLOD = float.MaxValue
            } }
        };

        private static int[] StructVariableSizes = [
            0, //Struct
            4, //Bool
            4, //Int
            4, //UInt,
            4, //DWord
            2, //Half
            4, //Float
            8, //Double
            8, //UInt64
            8, //Int64
            2, //Float16
            2, //UInt16
            2, //Int16
            0  //Matrix
            ];

        private record struct Attrib
        {
            public AttribType Type;
            public object Value;
        }

        private enum AttribType : byte
        {
            Vertex = 0,
            Pixel,
            Constants,
            IALayout,
            Property,
            BindGroup
        }

        private record struct AttribVertex
        {

        }

        private record struct AttributePixel
        {

        }

        private record struct AttributeConstants
        {

        }

        private record struct AttributeIALayout
        {
            public string Name;
            public int? Offset;
            public int? Slot;
            public RHI.InputClassification? Class;
            public RHI.InputElementFormat? Format;
        }

        private record struct AttributeProperty
        {
            public string Name;
        }

        private record struct AttributeBindGroup
        {
            public string Group;
        }

        private struct VarBindGroupComparer : IComparer<ShaderVariable>
        {
            public int Compare(ShaderVariable x, ShaderVariable y)
            {
                return string.Compare(x.Name, y.Name, StringComparison.InvariantCulture);
            }
        }
    }
}
