using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.SourceGenerator.Component
{
    internal static class CompSerializerGenericText
    {
        public const string Section1 = @"if (reader.TokenType != JsonTokenType.";
        public const string Section2 = @")
                                SceneJsonSerializer.PrintWarning($""Invalid json at: {reader.BytesConsumed}: Expected ";
        public const string Section3 = @" for field but instead got {reader.TokenType}"", entity, typeof(";
        public const string Section4 = @"));
                            if (";
        public const string Section5 = @")
                                Get_";
        public const string Section6 = @"(ref comp) = value;
                            else
                                SceneJsonSerializer.PrintWarning($""Invalid json at: {reader.BytesConsumed}: Failed to parse field value {Encoding.UTF8.GetString(reader.ValueSpan)}"", entity, typeof(";
        public const string Section7 = @"));
";

        public const string Section2_Alt = @")
                                SceneJsonSerializer.PrintWarning($""Invalid json at: {reader.BytesConsumed}: Expected {";
        public const string Section4_Alt = @"));
                            ";
        public const string Section5_Alt = @"                                Get_";
        public const string Section6_Alt = @"(ref comp) = value;
";
    }

    internal static class CompGetterNumber
    {
        public static void Generate(StringBuilder sb, ComponentField field, string structName, string readerMethodName)
        {
            sb.Append(CompSerializerGenericText.Section1);
            sb.Append("Number");
            sb.Append(CompSerializerGenericText.Section2);
            sb.Append("Number");
            sb.Append(CompSerializerGenericText.Section3);
            sb.Append(structName);
            sb.Append(CompSerializerGenericText.Section4);
            sb.Append("reader.");
            sb.Append(readerMethodName);
            sb.Append("(out ");
            sb.Append(field.Type);
            sb.Append(" value)");
            sb.Append(CompSerializerGenericText.Section5);
            sb.Append(field.Name);
            sb.Append(CompSerializerGenericText.Section6);
            sb.Append(structName);
            sb.Append(CompSerializerGenericText.Section7);
        }

        public static readonly int ByteDjb2 = "byte".GetDjb2HashCode();
        public static readonly int SByteDjb2 = "sbyte".GetDjb2HashCode();
        public static readonly int ShortDjb2 = "short".GetDjb2HashCode();
        public static readonly int UShortDjb2 = "ushort".GetDjb2HashCode();
        public static readonly int IntDjb2 = "int".GetDjb2HashCode();
        public static readonly int UIntDjb2 = "uint".GetDjb2HashCode();
        public static readonly int LongDjb2 = "long".GetDjb2HashCode();
        public static readonly int ULongDjb2 = "ulong".GetDjb2HashCode();
        public static readonly int FloatDjb2 = "float".GetDjb2HashCode();
        public static readonly int DoubleDjb2 = "double".GetDjb2HashCode();
    }

    internal static class CompGetterBoolean
    {
        public static void Generate(StringBuilder sb, ComponentField field, string structName)
        {
            sb.Append(CompSerializerGenericText.Section1);
            sb.Append(@"False || reader.TokenType != JsonTokenType.True");
            sb.Append(CompSerializerGenericText.Section2);
            sb.Append("Boolean");
            sb.Append(CompSerializerGenericText.Section3);
            sb.Append(structName);
            sb.Append(CompSerializerGenericText.Section4_Alt);
            sb.Append(@"bool value = reader.GetBoolean();
");
            sb.Append(CompSerializerGenericText.Section5_Alt);
            sb.Append(field.Name);
            sb.Append(CompSerializerGenericText.Section6_Alt);
        }

        public static readonly int BooleanDjb2 = "bool".GetDjb2HashCode();
    }

    internal static class CompGetterString
    {
        public static void Generate(StringBuilder sb, ComponentField field, string structName)
        {
            sb.Append(CompSerializerGenericText.Section1);
            sb.Append("String");
            sb.Append(CompSerializerGenericText.Section2);
            sb.Append("String");
            sb.Append(CompSerializerGenericText.Section3);
            sb.Append(structName);
            sb.Append(CompSerializerGenericText.Section4_Alt);
            sb.Append(@"string value = reader.GetString();
");
            sb.Append(CompSerializerGenericText.Section5_Alt);
            sb.Append(field.Name);
            sb.Append(CompSerializerGenericText.Section6_Alt);
        }

        public static readonly int StringDjb2 = "string".GetDjb2HashCode();
    }

    internal static class CompGetterEnum
    {
        public static void Generate(StringBuilder sb, ComponentField field, string structName)
        {
            sb.Append(CompSerializerGenericText.Section1);
            sb.Append("String");
            sb.Append(CompSerializerGenericText.Section2);
            sb.Append("String");
            sb.Append(CompSerializerGenericText.Section3);
            sb.Append(structName);
            sb.Append(CompSerializerGenericText.Section4);
            sb.Append("reader.TryGetString(out string? str) && Enum.TryParse<");
            sb.Append(field.Type);
            sb.Append(">(out ");
            sb.Append(field.Type);
            sb.Append(" value)");
            sb.Append(CompSerializerGenericText.Section5);
            sb.Append(field.Name);
            sb.Append(CompSerializerGenericText.Section6);
            sb.Append(structName);
            sb.Append(CompSerializerGenericText.Section7);
        }
    }

    internal static class CompCustomSerializerGetter
    {
        public static void Generate(StringBuilder sb, ComponentField field, string structName, string? customSerializer)
        {
            sb.Append(CompSerializerGenericText.Section1);
            sb.Append(customSerializer);
            sb.Append(".RequiredType");
            sb.Append(CompSerializerGenericText.Section2_Alt);
            sb.Append(customSerializer);
            sb.Append(".RequiredType}");
            sb.Append(CompSerializerGenericText.Section3);
            sb.Append(structName);
            sb.Append(CompSerializerGenericText.Section4);
            sb.Append(customSerializer);
            sb.Append(".Serialize(out ");
            sb.Append(field.Type);
            sb.Append(" value)");
            sb.Append(CompSerializerGenericText.Section5);
            sb.Append(field.Name);
            sb.Append(CompSerializerGenericText.Section6);
            sb.Append(structName);
            sb.Append(CompSerializerGenericText.Section7);
        }
    }

    internal static class CompGetterGeneric
    {
        public static void Generate(StringBuilder sb, ComponentField field, string structName)
        {
            sb.Append(@"if (SceneJsonSerializer.DeserializeGeneric<");
            sb.Append(structName);
            sb.Append(", ");
            sb.Append(field.Type);
            sb.Append(">(ref reader, ref entity, out ");
            sb.Append(field.Type);
            sb.Append(@" value))
                                Get_");
            sb.Append(field.Name);

            sb.Append(CompSerializerGenericText.Section6);
            sb.Append(structName);
            sb.Append(CompSerializerGenericText.Section7);
        }
    }
}
