using CommunityToolkit.HighPerformance;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Primary.SourceGenerator.Component;
using System.Collections.Immutable;
using System.Text;

namespace Primary.SourceGenerator
{
    [Generator]
    public class ComponentSerializerSourceGen : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "ComponentAttribute.g.cs",
                SourceText.From(Attribute, Encoding.UTF8)));

            IncrementalValuesProvider<ComponentToGenerate?> componentsToGenrate = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "Primary.Components.ComponentAttribute",
                    predicate: static (s, _) => true,
                    transform: static (ctx, _) => GetComponentToGenerate(ctx.SemanticModel, ctx.TargetNode))
                .Where(static m => m is not null);

            context.RegisterSourceOutput(componentsToGenrate,
                static (spc, source) => Execute(source, spc));
        }
        
        private static ComponentToGenerate? GetComponentToGenerate(SemanticModel semanticModel, SyntaxNode componentDeclerationSyntax)
        {
            if (semanticModel.GetDeclaredSymbol(componentDeclerationSyntax) is not INamedTypeSymbol structSymbol)
            {
                return null;
            }

            foreach (AttributeData attributeData in structSymbol.GetAttributes())
            {
                if (attributeData.AttributeClass?.Name == "DontSerializeComponentAttribute")
                    return null;
            }
                
            string structName = structSymbol.ToString();

            ImmutableArray<ISymbol> structMembers = structSymbol.GetMembers();
            List<ComponentField> fields = new List<ComponentField>(structMembers.Length);

            foreach (ISymbol member in structMembers)
            {
                if (member is IFieldSymbol field)
                {
                    string? customSerializer = null;

                    bool isValidField = true;
                    foreach (AttributeData attributeData in field.GetAttributes())
                    {
                        INamedTypeSymbol? typeSymbol = attributeData.AttributeClass;
                        if (typeSymbol != null)
                        {
                            if (typeSymbol.Name == "IgnoreDataMemberAttribute")
                            {
                                isValidField = false;
                                break;
                            }
                            else if (typeSymbol.Name == "CustomComponentSerializerAttribute")
                            {
                                foreach (var kvp in attributeData.NamedArguments)
                                {
                                    if (kvp.Key == "Type")
                                    {
                                        ITypeSymbol? argSymbol = kvp.Value.Type;
                                        if (argSymbol != null)
                                        {
                                            customSerializer = argSymbol.ToDisplayString(s_serializerArgFormat);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (isValidField)
                    {
                        ComponentFieldType fieldType = ComponentFieldType.Generic;
                        fields.Add(new ComponentField(field.Name, field.Type.ToString(), fieldType, customSerializer));
                    }
                }
            }

            return new ComponentToGenerate(structName, structSymbol.ContainingNamespace.ToString(), structSymbol.Name, fields.Count > 0 ? fields.ToArray() : Array.Empty<ComponentField>());
        }

        private static void Execute(ComponentToGenerate? componentToGenerate, SourceProductionContext context)
        {
            if (componentToGenerate is { } value)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(GeneratedSourceStart);
                sb.Append(value.Namespace);
                sb.Append(GeneratedSourceMiddle1);
                sb.Append(value.Namespace);
                sb.Append(GeneratedSourceMiddle2);
                sb.Append(value.Name);
                sb.Append(@"))]
    internal static class ");
                sb.Append(value.FriendlyName);

                sb.Append(GeneratedSourceMiddle3);
                sb.Append(value.Name);
                sb.Append(" comp = ref entity.GetComponent<");
                sb.Append(value.Name);
                sb.Append(@">();
            if (Unsafe.IsNullRef(in comp))
                throw new SceneLoadException(""Failed to add component of type ");
                sb.Append(value.Name);
                sb.Append(@""", entity);
            while (reader.TokenType != JsonTokenType.EndObject && reader.TokenType != JsonTokenType.Null)
            {
        ReadStart:
                reader.Read();
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                else if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new SceneLoadException($""Invalid json at: {reader.BytesConsumed}: Expected property name"", entity, typeof(");
                sb.Append(value.Name);
                sb.Append(@"));

                switch (reader.ValueSpan.GetDjb2HashCode())
                {
");

                foreach (ComponentField field in value.Fields)
                {
                    sb.Append("                    case ");
                    sb.Append(Encoding.UTF8.GetBytes(field.Name).AsSpan().GetDjb2HashCode());
                    sb.Append(@":
                        {
                            reader.Read();
                            ");

                    GenerateGetterFor(sb, field, value.Name);

                    sb.AppendLine(@"                            break;
                        }");
                }

                sb.Append(@"                    default:
                        SceneJsonSerializer.PrintWarning($""Invalid json at: {reader.BytesConsumed}: Unknown field name for component {Encoding.UTF8.GetString(reader.ValueSpan)} ({reader.ValueSpan.GetDjb2HashCode()})"", entity, typeof(");
                sb.Append(value.Name);
                sb.AppendLine(@"));
                        break;
                }
            }
");

                foreach (ComponentField field in value.Fields)
                {
                    sb.Append(@"            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = """);
                    sb.Append(field.Name);
                    sb.Append(@""")]
            extern static ref ");
                    sb.Append(field.Type);
                    sb.Append(" Get_");
                    sb.Append(field.Name);
                    sb.Append("(ref ");
                    sb.Append(value.Name);
                    sb.AppendLine(" self);");
                }

                sb.Append(@"        }
        public static void Add(ref SceneEntity entity)
        {
            ref ");
                sb.Append(value.Name);
                sb.Append(@" comp = ref entity.AddComponent<");
                sb.Append(value.Name);
                sb.Append(@">();
            if (!Unsafe.IsNullRef(in comp))
                comp = new ");
                sb.Append(value.Name);
                sb.Append(@"();
        }
    }
}");

                context.AddSource($"CompSerializer.{value.Name}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }

        private static void GenerateGetterFor(StringBuilder sb, ComponentField field, string structName)
        {
            if (field.CustomSerializer != null)
            {
                CompCustomSerializerGetter.Generate(sb, field, structName, field.CustomSerializer);
            }
            else
            {
                int hash = field.Type.GetDjb2HashCode();

                if (hash == CompGetterNumber.ByteDjb2)
                    CompGetterNumber.Generate(sb, field, structName, "TryGetByte");
                else if (hash == CompGetterNumber.SByteDjb2)
                    CompGetterNumber.Generate(sb, field, structName, "TryGetByte");
                else if (hash == CompGetterNumber.SByteDjb2)
                    CompGetterNumber.Generate(sb, field, structName, "TryGetByte");
                else if (hash == CompGetterNumber.ShortDjb2)
                    CompGetterNumber.Generate(sb, field, structName, "TryGetInt16");
                else if (hash == CompGetterNumber.UShortDjb2)
                    CompGetterNumber.Generate(sb, field, structName, "TryGetUInt16");
                else if (hash == CompGetterNumber.IntDjb2)
                    CompGetterNumber.Generate(sb, field, structName, "TryGetInt32");
                else if (hash == CompGetterNumber.UIntDjb2)
                    CompGetterNumber.Generate(sb, field, structName, "TryGetUInt32");
                else if (hash == CompGetterNumber.FloatDjb2)
                    CompGetterNumber.Generate(sb, field, structName, "TryGetSingle");
                else if (hash == CompGetterNumber.DoubleDjb2)
                    CompGetterNumber.Generate(sb, field, structName, "TryGetDouble");
                else if (hash == CompGetterBoolean.BooleanDjb2)
                    CompGetterBoolean.Generate(sb, field, structName);
                else if (hash == CompGetterString.StringDjb2)
                    CompGetterString.Generate(sb, field, structName);
                else
                    CompGetterGeneric.Generate(sb, field, structName);
            }
        }

        private const string Attribute = @"
namespace Primary.SourceGenerator;

[System.AttributeUsage(System.AttributeTargets.Struct | System.AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ComponentAttribute : System.Attribute
{
}";

        private const string GeneratedSourceStart = @"using CommunityToolkit.HighPerformance;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Primary.Components;
using Primary.Scenes;
using Primary.Scenes.Json;
using System.Numerics;
using System.Runtime.InteropServices;
using ";

        private const string GeneratedSourceMiddle1 = @";

namespace ";

        private const string GeneratedSourceMiddle2 = @"
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    [ComponentDeserializer(typeof(";

        private const string GeneratedSourceMiddle3 = @"_GenSerializer
    {
        public static void Deserialize(ref Utf8JsonReader reader, ref SceneEntity entity)
        {
            ref ";

        private static readonly SymbolDisplayFormat s_serializerArgFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);
    }

    public interface IComponentSerializerGetter
    {
        public void Generate(StringBuilder sb, ComponentField field, string structName);
    }

    public readonly record struct ComponentToGenerate
    {
        public readonly string Name;
        public readonly string Namespace;
        public readonly string FriendlyName;
        public readonly ImmutableArray<ComponentField> Fields;

        public ComponentToGenerate(string name, string @namespace, string friendlyName, ComponentField[] fields)
        {
            Name = name;
            Namespace = @namespace;
            FriendlyName = friendlyName;
            Fields = fields.ToImmutableArray();
        }
    }

    public readonly record struct ComponentField
    {
        public readonly string Name;
        public readonly string Type;
        public readonly ComponentFieldType FieldType;

        public readonly string? CustomSerializer;

        public ComponentField(string name, string type, ComponentFieldType fieldType, string? customSerializer)
        {
            Name = name;
            Type = type;
            FieldType = fieldType;

            CustomSerializer = customSerializer;
        }
    }

    public enum ComponentFieldType : byte
    {
        Generic = 0,
    }
}
