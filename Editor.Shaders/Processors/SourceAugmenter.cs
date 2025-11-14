using Editor.Shaders.Attributes;
using Editor.Shaders.Data;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Shaders.Processors
{
    internal ref struct SourceAugmenter
    {
        private readonly ShaderProcessor _processor;

        internal SourceAugmenter(ShaderProcessor processor)
        {
            _processor = processor;
        }

        internal string Augment(string orignalSource)
        {
            using RentedArray<char> augment = RentedArray<char>.Rent(orignalSource.Length);
            orignalSource.CopyTo(augment.Span);

            StringBuilder sb = new StringBuilder();

            RemoveOldDirectives(augment.Span);
            GenerateNewHeader(sb);

            sb.Append(augment.Span);
            return AppendBindlessResources(sb.ToString());
        }

        private void RemoveOldDirectives(Span<char> source)
        {
            foreach (ref readonly ResourceData resources in _processor.Resources)
            {
                source.Slice(resources.DeclerationRange.Start.Value, resources.DeclerationRange.End.Value - resources.DeclerationRange.Start.Value).Fill(' ');
            }

            foreach (ref readonly PropertyData property in _processor.Properties)
            {
                source.Slice(property.DeclerationRange.Start.Value, property.DeclerationRange.End.Value - property.DeclerationRange.Start.Value).Fill(' ');
            }

            foreach (ref readonly StructData @struct in _processor.Structs)
            {
                source.Slice(@struct.DeclerationRange.Start.Value, @struct.DeclerationRange.End.Value - @struct.DeclerationRange.Start.Value).Fill(' ');
            }
        }

        private void GenerateNewHeader(StringBuilder sb)
        {
            bool hasUserConstants = false;

            int variableCount = CountTotalVariablesUsed();
            using RentedArray<RawVariableData> variableDatas = RentedArray<RawVariableData>.Rent(variableCount);
            FillAndSortVariables(variableDatas.Span);

            //generate bindless header
            {
                sb.AppendLine("struct __BINDLESS_GENERATED");
                sb.AppendLine("{");
                
            }
        }

        private string AppendBindlessResources(string @string)
        {
            return @string;
        }

        private int CountTotalVariablesUsed() => _processor.Resources.Length + _processor.Properties.Length;
        private void FillAndSortVariables(Span<RawVariableData> variableDatas)
        {
            Dictionary<string, int> bindGroupIndices = new Dictionary<string, int>();

            int offset = 0;
            int i = 0;

            foreach (ref readonly ResourceData resource in _processor.Resources)
            {
                RawVariableUsage usage = RawVariableUsage.Property;
                int bindGroup = 0;

                foreach (ref readonly AttributeData attribute in resource.Attributes.AsSpan())
                {
                    if (attribute.Signature is AttributeGlobal)
                    {
                        usage = RawVariableUsage.Global;
                        bindGroup = -1;
                    }
                    else if (attribute.Signature is AttributeConstants)
                    {
                        usage = RawVariableUsage.Constants;
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

                variableDatas[offset++] = new RawVariableData(RawVariableDataType.Resource, usage, i++, resource.Name, bindGroup);
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

                variableDatas[offset++] = new RawVariableData(RawVariableDataType.Property, usage, i++, property.Name, bindGroup);
            }

            variableDatas.Sort();
        }

        private readonly record struct RawVariableData(RawVariableDataType Type, RawVariableUsage Usage, int Index, string Name, int BindGroup) : IComparable<RawVariableData>
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
            Property,
            Constants,
        }
    }
}
