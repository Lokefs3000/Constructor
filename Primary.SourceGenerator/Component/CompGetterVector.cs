using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Primary.SourceGenerator.Component
{
    internal static class CompGetterVector
    {
        public static void Generate(StringBuilder sb, ComponentField field, string structName, char elementCount)
        {
            sb.Append(@"                            if (reader.TokenType != JsonTokenType.StartArray)
                                SceneJsonSerializer.PrintWarning($""Invalid json at: {reader.Position}: Expected array start for field but instead got {reader.TokenType}"", entity, typeof(");
            sb.Append(structName);
            sb.Append(@"));
                            Vector");
            sb.Append(elementCount);
            sb.Append(@" v = default;
                            Span<float> span = MemoryMarshal.Cast<Vector");
            sb.Append(elementCount);
            sb.Append(@", float>(ref new Span<Vector");
            sb.Append(elementCount);
            sb.Append(@"(ref v));
                            for (int i = 0; i < ");
            sb.Append(elementCount);
            sb.Append(@"; ++i)
                            {
                                reader.Read();
                                if (reader.TokenType != JsonTokenType.Number)
                                {
                                    SceneJsonSerializer.PrintWarning($""Invalid json at: {reader.Position}: Expected Number at index {i} but instead got {reader.TokenType}"", entity, typeof(");
            sb.Append(structName);
            sb.Append(@"));
                                    SceneJsonSerializer.SkipUntilToken(ref reader, JsonTokenType.EndArray);
                                    reader.Read();
                                    goto ReadStart;
                                }
                                if (reader.TryGetSingle(out float value))
                                    span[i] = value;
                                else
                                {
                                    SceneJsonSerializer.PrintWarning($""Invalid json at: {reader.Position}: Failed to parse field value {reader.ValueSpan.ToString()}"", entity, typeof(");
            sb.Append(structName);
            sb.Append(@"));
                                    SceneJsonSerializer.SkipUntilToken(ref reader, JsonTokenType.EndArray);
                                    reader.Read();
                                    goto ReadStart;
                                }
                            }
                            if (reader.TokenType != JsonTokenType.EndArray)
                                SceneJsonSerializer.PrintWarning($""Invalid json at: {reader.Position}: Expected array end for field but instead got {reader.TokenType}"", entity, typeof(");
            sb.Append(structName);
            sb.Append(@"));
                            comp._{} = v;
");
        }
    }
}
