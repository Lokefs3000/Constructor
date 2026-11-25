using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Streams;
using Primary.Utility;
using PrimaryEditor.Assets;
using PrimaryEditor.Assets.Loaders;
using PrimaryEditor.Utility;

namespace PrimaryEditor.Processors.Stylesheet
{
    public sealed class StylesheetProcessor
    {
        public static unsafe void Execute(string localPath, string sourceText, Stream outputStream, out string[] usedFiles)
        {
            string fullLocalPath = localPath;

            HashSet<string> includedFiles = [fullLocalPath];
            List<SourceState> sourceStates = new List<SourceState>();

            Dictionary<string, string> currentVariables = new Dictionary<string, string>();
            var currentVariablesSpanLookup = currentVariables.GetAlternateLookup<ReadOnlySpan<char>>();

            Dictionary<RulesetKey, RulesetData> rulesets = new Dictionary<RulesetKey, RulesetData>();

            string[]? triggerValueArray = null;
            Stack<string> triggerValuesStack = new Stack<string>();

            sourceStates.Add(new SourceState(sourceText, 0, 0, 0, []));
            while (sourceStates.Count > 0)
            {
                ref SourceState ss = ref sourceStates.AsSpan()[0];

                if (IsEOF(ref ss))
                {
                    sourceStates.RemoveAt(0);
                    continue;
                }

                ss.Start = ss.Current;

                char c = Advance(ref ss);
                switch (c)
                {
                    case '\n': ++ss.Line; break;
                    case '@': ParseDirective(ref ss); break;

                    case '$': ParseVariable(ref ss); break;
                    case '.': ParseRuleset(ref ss, true); break;

                    case '&': ParseSelectorRuleset(ref ss, [], []); break;
                    case '!': ParseSelectorRuleset(ref ss, [], [], true); break;

                    case '/': HandleComment(ref ss); break;

                    case '}':
                        {
                            if (ss.Context.TryPop(out _))
                            {
                                if (ss.Context.TryPeek(out RulesetData? previousRuleset))
                                {
                                    triggerValueArray = previousRuleset.TriggerValues;
                                    triggerValuesStack.TryPop(out _);
                                }
                                else
                                {
                                    triggerValueArray = null;
                                    triggerValuesStack.Clear();
                                }
                            }
                            else
                                throw new Exception($"{ss.Line}: Cannot close context because none are currently active");

                            break;
                        }

                    default:
                        {
                            if (char.IsControl(c) || char.IsWhiteSpace(c))
                                break;
                            else if (char.IsLetter(c))
                            {
                                if (ss.Context.Count == 0)
                                    ParseRuleset(ref ss, false);
                                else
                                    ParseRulesetValue(ref ss);
                                break;
                            }

                            throw new Exception($"{ss.Line}: Invalid character '{c}'");
                        }
                }
            }

            StringTable stringTable = new StringTable();

            using PooledMemoryStream dataStream = new PooledMemoryStream();

            int writtenRulesets = 0;
            foreach (var (key, ruleset) in rulesets)
            {
                if (ruleset.OwningRulset == null)
                {
                    ++writtenRulesets;

                    dataStream.Write(new SSTClass
                    {
                        NameSId = stringTable.GetStringId(ruleset.RulesetName),
                        PropertyGroupCount = (ushort)(1 + ruleset.ChildRulesets.Count(static (x) => x.RulesetType == RulesetType.Default && x.TriggerValues != null)),
                        ChildCount = (ushort)ruleset.ChildRulesets.Count(static (x) => x.RulesetType != RulesetType.Default)
                    });

                    WritePropertyGroups(ruleset);

                    foreach (RulesetData childRuleset in ruleset.ChildRulesets)
                    {
                        if (childRuleset.TriggerValues == null && childRuleset.RulesetType != RulesetType.Default)
                        {
                            dataStream.Write(new SSTPseudoClass
                            {
                                Type = childRuleset.RulesetType switch
                                {
                                    RulesetType.Default => throw new NotSupportedException(),
                                    RulesetType.FirstChild => SSTPseudoClassType.FirstChild,
                                    RulesetType.LastChild => SSTPseudoClassType.LastChild,
                                    RulesetType.OnlyChild => SSTPseudoClassType.OnlyChild,
                                    RulesetType.FirstOfType => SSTPseudoClassType.FirstOfType,
                                    RulesetType.LastOfType => SSTPseudoClassType.LastOfType,
                                    RulesetType.OnlyOfType => SSTPseudoClassType.OnlyOfType,
                                    RulesetType.AllChildren => SSTPseudoClassType.AllChildren,
                                    _ => throw new NotImplementedException(),
                                },
                                NameSId = childRuleset.Target == null ? uint.MaxValue : stringTable.GetStringId(childRuleset.Target),
                                PropertyGroupCount = (ushort)(1 + childRuleset.ChildRulesets.Count(static (x) => x.RulesetType == RulesetType.Default && x.TriggerValues != null)),
                            });

                            WritePropertyGroups(childRuleset);
                        }
                    }

                    void WritePropertyGroups(RulesetData ruleset)
                    {
                        dataStream.Write(new SSTPropertyGroup
                        {
                            TriggerCount = (byte)(ruleset.TriggerValues?.Length ?? 0),
                            PropertyCount = (ushort)ruleset.Values.Count,
                        });

                        if (ruleset.TriggerValues != null)
                        {
                            foreach (string triggerValue in ruleset.TriggerValues)
                            {
                                dataStream.Write(new SSTTrigger
                                {
                                    NameSId = stringTable.GetStringId(triggerValue)
                                });
                            }
                        }

                        foreach (var (key, value) in ruleset.Values)
                        {
                            dataStream.Write(new SSTProperty
                            {
                                NameSId = stringTable.GetStringId(key),
                                ValueSId = stringTable.GetStringId(value),
                            });
                        }

                        foreach (RulesetData childRuleset in ruleset.ChildRulesets)
                        {
                            if (childRuleset.TriggerValues != null && childRuleset.RulesetType == RulesetType.Default)
                                WritePropertyGroups(childRuleset);
                        }
                    }
                }
            }

            outputStream.Write(new StylesheetHeader
            {
                FileHeader = StylesheetHeader.Header,
                FileVersion = StylesheetHeader.Version,

                StringTableSize = (uint)stringTable.StringList.Count,
                ClassCount = (uint)writtenRulesets
            });

            foreach (string str in stringTable.StringList)
            {
                outputStream.Write((ushort)str.Length);
                outputStream.Write(str.AsSpan());
            }

            dataStream.CopyTo(outputStream);

            usedFiles = [.. includedFiles.Where((x) => x != fullLocalPath)];

            void ParseDirective(ref SourceState ss)
            {
                if (ss.Context.Count != 0)
                    throw new Exception($"'{ss.Line}': Directive is invalid when within context");

                ReadOnlySpan<char> directiveName = ReadIdentifier(ref ss, true);
                if (directiveName.SequenceEqual("use"))
                {
                    SkipWhitespaceUntil(ref ss, '"');

                    --ss.Current;
                    ReadOnlySpan<char> fileLocation = ReadString(ref ss);

                    string fileLocationAsStr = fileLocation.ToString();
                    if (includedFiles.Contains(fileLocationAsStr))
                        throw new Exception($"{ss.Line}: Stylesheet used twice '{fileLocationAsStr}'");

                    string? sourceText = FilesystemManager.ReadAllText(fileLocationAsStr, true);
                    if (sourceText == null)
                        throw new Exception($"{ss.Line}: Failed to read source text for used stylesheet '{fileLocationAsStr}'");

                    SkipWhitespaceUntil(ref ss, ';');

                    sourceStates.Insert(0, new SourceState(sourceText, 0, 0, 0, []));
                    includedFiles.Add(fileLocationAsStr);
                }
                else
                {
                    throw new Exception($"{ss.Line}: Unknown directive '{directiveName}'");
                }
            }

            void ParseVariable(ref SourceState ss)
            {
                if (ss.Context.Count != 0)
                    throw new Exception($"'{ss.Line}': Variable is invalid when within context");

                ReadOnlySpan<char> variableName = ReadIdentifier(ref ss);
                if (variableName.IsEmpty)
                    throw new Exception($"{ss.Line}: Expected name for variable");

                SkipWhitespaceUntil(ref ss, ':');

                ReadOnlySpan<char> variableValue = ReadUntil(ref ss, ';', true).Trim();
                if (variableValue.IsEmpty)
                    throw new Exception($"{ss.Line}: Expected value for variable '{variableName}'");

                currentVariablesSpanLookup[variableName] = variableValue.ToString();
            }

            void ParseRuleset(ref SourceState ss, bool rulesetDefinesClass)
            {
                if (ss.Context.Count != 0)
                    throw new Exception($"'{ss.Line}': Top-level ruleset is invalid when within context");

                if (triggerValuesStack.Count != 0)
                    throw new Exception($"{ss.Line}: Cannot define top-level ruleset with trigger values");

                ReadOnlySpan<char> rulesetName = ReadIdentifier(ref ss);
                if (rulesetName.IsEmpty)
                    throw new Exception($"{ss.Line}: Empty ruleset name");

                SkipWhitespaceUntil(ref ss, '{');

                RulesetKey key = new RulesetKey(rulesetName.ToString(), RulesetType.Default, null, ArraySegment<string>.Empty);

                ref RulesetData? ruleset = ref CollectionsMarshal.GetValueRefOrAddDefault(rulesets, key, out bool exists);
                if (!exists)
                    ruleset = new RulesetData(null, key.RulesetName, RulesetType.Default, null, null);

                ss.Context.Push(ruleset!);
            }

            void ParseRulesetValue(ref SourceState ss)
            {
                if (ss.Context.Count == 0)
                    throw new Exception($"'{ss.Line}': Ruleset values must have a context");

                ReadOnlySpan<char> valueName = ReadIdentifier(ref ss);
                if (valueName.IsEmpty)
                    throw new Exception($"{ss.Line}: Empty ruleset value name");

                SkipWhitespaceUntil(ref ss, ':');
                SkipWhitespace(ref ss);

                ReadOnlySpan<char> valueRaw = ReadUntilEither(ref ss, ';', '{', true).Trim();
                if (valueRaw.IsEmpty)
                    throw new Exception($"'{ss.Line}': Empty ruleset value");

                string? variableValue = null;
                if (valueRaw[0] == '$')
                {
                    if (valueRaw.Length == 1)
                        throw new Exception($"{ss.Line}: Expected name after variable invokation");

                    if (!currentVariablesSpanLookup.TryGetValue(valueRaw, out variableValue))
                        throw new Exception($"{ss.Line}: Undefined variable '{valueRaw}'");
                }
                else
                {
                    variableValue = valueRaw.ToString();
                }

                --ss.Current;

                char nextToken = Peek(ref ss);
                if (nextToken == '{')
                {
                    //ss.Current = ss.Start;
                    Advance(ref ss);
                    ParseSelectorRuleset(ref ss, valueRaw, valueName);
                }
                else
                {
                    if (nextToken != ';')
                        throw new Exception($"{ss.Line}: Expected ';'");
                    Advance(ref ss);

                    if (!ss.Context.Peek().TryAddUnique(valueName, variableValue))
                        throw new Exception($"{ss.Line}: Duplicate value in ruleset '{valueName}'");
                }
            }

            void ParseSelectorRuleset(ref SourceState ss, ReadOnlySpan<char> pseudoClassName, ReadOnlySpan<char> pseudoClassValue, bool asTrigger = false)
            {
                if (pseudoClassName.IsEmpty)
                {
                    SkipWhitespaceUntil(ref ss, ':');
                }

                if (pseudoClassValue.IsEmpty)
                {
                    pseudoClassValue = ReadIdentifier(ref ss, true);
                    if (pseudoClassValue.IsEmpty)
                        throw new Exception($"{ss.Line}: Target selector value is empty");

                    SkipWhitespaceUntil(ref ss, '{');
                }

                RulesetType rulesetType = RulesetType.Default;
                if (asTrigger)
                {
                    if (ss.Context.Count < 1)
                        throw new Exception($"{ss.Line}: Trigger value must be within a ruleset");

                    triggerValueArray = null;
                    triggerValuesStack.Push(pseudoClassValue.ToString());
                }
                else
                {
                    if (ss.Context.Count != 1)
                        throw new Exception($"{ss.Line}: Ruleset selectors must have a top-level ruleset as context");

                    if (s_rulesetTypeNamesAlt.TryGetValue(pseudoClassName.IsEmpty ? pseudoClassValue : pseudoClassName, out rulesetType))
                    {
                        switch (rulesetType)
                        {
                            case RulesetType.FirstChild:
                            case RulesetType.LastChild:
                            case RulesetType.OnlyChild:
                            case RulesetType.AllChildren:
                                {
                                    if (!pseudoClassName.IsEmpty)
                                        throw new Exception($"{ss.Line}: Target selector '{rulesetType}' should not have a target");
                                    pseudoClassValue = null;
                                    break;
                                }
                            case RulesetType.FirstOfType:
                            case RulesetType.LastOfType:
                            case RulesetType.OnlyOfType:
                                {
                                    if (pseudoClassValue.IsEmpty)
                                        throw new Exception($"{ss.Line}: Target selector '{rulesetType}' must have a target");
                                    pseudoClassName = pseudoClassValue;
                                    break;
                                }
                        }
                    }
                }

                if (triggerValueArray == null)
                {
                    triggerValueArray = triggerValuesStack.Count == 0 ? null : [.. triggerValuesStack];
                    if (triggerValueArray != null)
                        Array.Sort(triggerValueArray, static (x, y) => x.CompareTo(y, StringComparison.Ordinal));
                }

                RulesetData previousRuleset = ss.Context.Peek();
                RulesetKey key = new RulesetKey(previousRuleset.RulesetName, rulesetType, pseudoClassValue.IsEmpty ? null : pseudoClassValue.ToString(), triggerValueArray ?? ArraySegment<string>.Empty);

                ref RulesetData? rulesetData = ref CollectionsMarshal.GetValueRefOrAddDefault(rulesets, key, out bool exists);
                if (!exists)
                {
                    rulesetData = new RulesetData(previousRuleset.OwningRulset ?? previousRuleset, previousRuleset.RulesetName, rulesetType, key.Target, triggerValueArray);
                    previousRuleset.TryAddRuleset(rulesetData);
                }

                ss.Context.Push(rulesetData!);
            }

            void HandleComment(ref SourceState ss)
            {
                if (Advance(ref ss) != '/')
                    throw new Exception($"{ss.Line}: Comment is missing second forward slash");

                while (Advance(ref ss) != '\n') ;
                ++ss.Line;
            }

            ReadOnlySpan<char> ReadIdentifier(ref SourceState ss, bool resetStartToCurrent = false)
            {
                if (resetStartToCurrent)
                    ss.Start = ss.Current;

                while (char.IsLetterOrDigit(Peek(ref ss)) || Peek(ref ss) == '-')
                {
                    Advance(ref ss);
                }

                return ss.SourceText.AsSpan()[ss.Start..ss.Current];
            }

            ReadOnlySpan<char> ReadString(ref SourceState ss)
            {
                if (Advance(ref ss) != '"')
                    throw new Exception($"{ss.Line}: Expected a double quoted string");

                ss.Start = ss.Current;
                while (true)
                {
                    char c = Advance(ref ss);
                    if (c == '"')
                        break;

                    if (char.IsControl(c))
                    {
                        if (IsEOF(ref ss))
                            throw new Exception($"'{ss.Line}: Unexpected end of file before string end");
                        else
                            throw new Exception($"'{ss.Line}: Unexpected control char in string");
                    }
                }

                return ss.SourceText.AsSpan()[ss.Start..(ss.Current - 1)];
            }

            void SkipWhitespace(ref SourceState ss)
            {
                while (char.IsWhiteSpace(Peek(ref ss)))
                {
                    char c = Advance(ref ss);

                    if (c == '\n')
                        ++ss.Line;
                }
            }

            void SkipWhitespaceUntil(ref SourceState ss, char expectedChar)
            {
                while (char.IsWhiteSpace(Peek(ref ss)))
                {
                    char c = Advance(ref ss);

                    if (c == '\n')
                        ++ss.Line;
                }

                if (Advance(ref ss) != expectedChar)
                    throw new Exception($"{ss.Line}: Expected '{expectedChar}'");
            }

            ReadOnlySpan<char> ReadUntil(ref SourceState ss, char expectedChar, bool consumeEndChar = false)
            {
                ss.Start = ss.Current;
                while (Peek(ref ss) != expectedChar)
                {
                    char c = Advance(ref ss);
                    if (char.IsControl(c))
                    {
                        if (IsEOF(ref ss))
                            throw new Exception($"{ss.Line}: Unexpected end of file while reading");
                        throw new Exception($"{ss.Line}: Unexpected control character");
                    }
                }

                if (consumeEndChar)
                {
                    Advance(ref ss);
                    return ss.SourceText.AsSpan()[ss.Start..(ss.Current - 1)];
                }
                else
                {
                    return ss.SourceText.AsSpan()[ss.Start..ss.Current];
                }
            }

            ReadOnlySpan<char> ReadUntilEither(ref SourceState ss, char expectedChar, char expectedChar2, bool consumeEndChar = false)
            {
                ss.Start = ss.Current;
                while (Peek(ref ss) != expectedChar && Peek(ref ss) != expectedChar2)
                {
                    char c = Advance(ref ss);
                    if (char.IsControl(c))
                    {
                        if (IsEOF(ref ss))
                            throw new Exception($"{ss.Line}: Unexpected end of file while reading");
                        throw new Exception($"{ss.Line}: Unexpected control character");
                    }
                }

                if (consumeEndChar)
                {
                    Advance(ref ss);
                    return ss.SourceText.AsSpan()[ss.Start..(ss.Current - 1)];
                }
                else
                {
                    return ss.SourceText.AsSpan()[ss.Start..ss.Current];
                }
            }

            bool IsEOF(ref SourceState ss) => ss.Current >= ss.SourceText.Length;

            char Advance(ref SourceState ss) => IsEOF(ref ss) ? '\0' : ss.SourceText[ss.Current++];
            char Peek(ref SourceState ss) => IsEOF(ref ss) ? '\0' : ss.SourceText[ss.Current];
        }

        private static FrozenDictionary<string, RulesetType> s_rulesetTypeNames = new Dictionary<string, RulesetType>
        {
            { "first-child", RulesetType.FirstChild },
            { "last-child", RulesetType.LastChild },
            { "only-child", RulesetType.OnlyChild },
            { "first-of-type", RulesetType.FirstOfType },
            { "last-of-type", RulesetType.LastOfType },
            { "only-of-type", RulesetType.OnlyOfType },
            { "all-children", RulesetType.AllChildren },
        }.ToFrozenDictionary();

        private static FrozenDictionary<string, RulesetType>.AlternateLookup<ReadOnlySpan<char>> s_rulesetTypeNamesAlt = s_rulesetTypeNames.GetAlternateLookup<ReadOnlySpan<char>>();
    }
}
