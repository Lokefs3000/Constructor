using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Primary.SourceGenerator.Utility;

namespace Primary.SourceGenerator
{
    [Generator]
    public sealed class CRefExtensionGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<CRefExtensionSource?> valueProvider = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "Primary.Components.ComponentAttribute",
                    predicate: static (s, _) => true,
                    transform: static (ctx, _) => ResolveCRefSourceData(ctx.SemanticModel, ctx.TargetNode))
                .Where(static m => m is not null);

            context.RegisterSourceOutput(valueProvider, WriteOutput);
        }

        private static CRefExtensionSource? ResolveCRefSourceData(SemanticModel semanticModel, SyntaxNode componentDeclerationSyntax)
        {
            if (semanticModel.GetDeclaredSymbol(componentDeclerationSyntax) is not INamedTypeSymbol structSymbol ||
                structSymbol.DeclaredAccessibility != Accessibility.Public ||
                structSymbol.TypeKind != TypeKind.Struct)
            {
                return null;
            }

            bool inheritsFromComponent = false;
            foreach (INamedTypeSymbol typeSymbol in structSymbol.Interfaces)
            {
                if (typeSymbol.Name == "IComponent")
                {
                    inheritsFromComponent = true;
                    break;
                }
            }

            if (!inheritsFromComponent)
            {
                return null;
            }    

            using RentedList<CRefField> fields = new RentedList<CRefField>();

            ImmutableArray<ISymbol> structMembers = structSymbol.GetMembers();
            foreach (ISymbol symbol in structMembers)
            {
                if (symbol is IFieldSymbol fieldSymbol)
                {
                    AccesibilityModifier modifier;
                    switch (fieldSymbol.DeclaredAccessibility)
                    {
                        case Accessibility.Public: modifier = AccesibilityModifier.Public; break;
                        case Accessibility.Internal: modifier = AccesibilityModifier.Internal; break;
                        case Accessibility.ProtectedOrInternal: modifier = AccesibilityModifier.Internal; break;
                        default: continue;
                    }

                    AccessCapabilities access = fieldSymbol.IsReadOnly ? AccessCapabilities.Read : AccessCapabilities.ReadWrite;
                    string fullTypeName = GetFullTypeName(fieldSymbol.Type);

                    fields.Add(new CRefField(fullTypeName, fieldSymbol.Name, modifier, access));
                }
                else if (symbol is IPropertySymbol propertySymbol)
                {
                    AccesibilityModifier? setModifier = null;
                    if (propertySymbol.SetMethod != null)
                    {
                        IMethodSymbol methodSymbol = propertySymbol.SetMethod;
                        switch (methodSymbol.DeclaredAccessibility)
                        {
                            case Accessibility.Public: setModifier = AccesibilityModifier.Public; break;
                            case Accessibility.Internal: setModifier = AccesibilityModifier.Internal; break;
                            case Accessibility.ProtectedOrInternal: setModifier = AccesibilityModifier.Internal; break;
                            default: break;
                        }
                    }

                    AccesibilityModifier? getModifier = null;
                    if (propertySymbol.GetMethod != null)
                    {
                        IMethodSymbol methodSymbol = propertySymbol.GetMethod;
                        switch (methodSymbol.DeclaredAccessibility)
                        {
                            case Accessibility.Public: getModifier = AccesibilityModifier.Public; break;
                            case Accessibility.Internal: getModifier = AccesibilityModifier.Internal; break;
                            case Accessibility.ProtectedOrInternal: getModifier = AccesibilityModifier.Internal; break;
                            default: break;
                        }
                    }

                    if (setModifier is null && getModifier is null)
                        continue;

                    string fullTypeName = GetFullTypeName(propertySymbol.Type);
                    fields.Add(new CRefField(fullTypeName, propertySymbol.Name, getModifier, setModifier));
                }
            }

            if (fields.IsEmpty)
                return null;

            string fullName = GetFullTypeName(structSymbol);
            return new CRefExtensionSource(fullName, structSymbol.ContainingNamespace.ToString(), structSymbol.Name, [.. fields]);
        }

        private static void WriteOutput(SourceProductionContext context, CRefExtensionSource? source)
        {
            if (source is { } value)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(@"using System.Runtime.CompilerServices;");
                sb.AppendLine(@"using Primary.Scenes.Entities;");
                sb.AppendLine();
                sb.Append("namespace ");
                sb.AppendLine(value.NamespaceName);
                sb.AppendLine("{");
                {
                    sb.AppendLine("    public static partial class CRefExtensions");
                    sb.AppendLine("    {");
                    {
                        sb.Append("        extension (CRef<");
                        sb.Append(value.FullName);
                        sb.AppendLine("> cref)");
                        sb.AppendLine("        {");
                        foreach (CRefField field in value.Fields)
                        {
                            string? getterAccessor = field.Getter?.ToString().ToLower();
                            string? setterAccessor = field.Setter?.ToString().ToLower();

                            bool areAccessorsEqual = field.Getter == field.Setter;

                            if (areAccessorsEqual)
                            {
                                sb.AppendLine("            [CompilerGenerated]");
                                sb.Append("            ");
                                sb.Append(getterAccessor ?? setterAccessor);
                                sb.Append(' ');
                                sb.Append(field.FullTypeName);
                                sb.Append(' ');
                                sb.Append(field.PublicName);
                                sb.Append(" { ");
                                if (getterAccessor != null)
                                {
                                    sb.Append("get => cref.ComponentRef.");
                                    sb.Append(field.PublicName);
                                    sb.Append("; ");
                                }

                                if (setterAccessor != null)
                                {
                                    sb.Append("set => cref.ComponentRef.");
                                    sb.Append(field.PublicName);
                                    sb.Append(" = value; ");
                                }

                                sb.AppendLine(" }");
                            }
                            else
                            {
                                sb.AppendLine("            [CompilerGenerated]");
                                sb.Append("            ");
                                sb.Append(getterAccessor ?? setterAccessor);
                                sb.Append(' ');
                                sb.Append(field.FullTypeName);
                                sb.Append(' ');
                                sb.Append(field.PublicName);
                                sb.Append(" { ");
                                if (getterAccessor != null)
                                {
                                    if (field.Getter != AccesibilityModifier.Public && field.Setter.HasValue)
                                    {
                                        sb.Append(getterAccessor);
                                        sb.Append(' ');
                                    }

                                    sb.Append("get => cref.ComponentRef.");
                                    sb.Append(field.PublicName);
                                    sb.Append("; ");
                                }

                                if (setterAccessor != null)
                                {
                                    if (field.Setter != AccesibilityModifier.Public && field.Getter.HasValue)
                                    {
                                        sb.Append(setterAccessor);
                                        sb.Append(' ');
                                    }

                                    sb.Append("set => cref.ComponentRef.");
                                    sb.Append(field.PublicName);
                                    sb.Append(" = value; ");
                                }

                                sb.AppendLine(" }");
                            }
                        }
                        sb.AppendLine("        }");
                    }
                    sb.AppendLine("    }");
                }
                sb.AppendLine("}");

                context.AddSource($"CRefExtensions.{value.PublicName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        }

        private static string GetFullTypeName(ITypeSymbol typeSymbol) => $"{typeSymbol.ContainingNamespace}.{typeSymbol.Name}";

        private readonly record struct CRefExtensionSource
        {
            public readonly string FullName;
            public readonly string NamespaceName;
            public readonly string PublicName;
            public readonly ImmutableArray<CRefField> Fields;

            public CRefExtensionSource(string fullName, string namespaceName, string publicName, ImmutableArray<CRefField> fields)
            {
                FullName = fullName;
                NamespaceName = namespaceName;
                PublicName = publicName;
                Fields = fields;
            }
        }

        private readonly record struct CRefField
        {
            public readonly string FullTypeName;
            public readonly string PublicName;
            public readonly AccesibilityModifier? Setter;
            public readonly AccesibilityModifier? Getter;

            public CRefField(string fullTypeName, string publicName, AccesibilityModifier modifiers, AccessCapabilities access)
            {
                FullTypeName = fullTypeName;
                PublicName = publicName;
                Setter = (access & AccessCapabilities.Read) > 0 ? modifiers : null;
                Getter = (access & AccessCapabilities.Write) > 0 ? modifiers : null;
            }

            public CRefField(string fullTypeName, string propertyPath, AccesibilityModifier? read, AccesibilityModifier? write)
            {
                FullTypeName = fullTypeName;
                PublicName = propertyPath;
                Setter = write;
                Getter = read;
            }
        }

        private enum AccesibilityModifier : byte
        {
            Public = 0,
            Internal
        }

        private enum AccessCapabilities : byte
        {
            None = 0,
            ReadWrite = Read | Write,

            Read = 1 << 0,
            Write = 1 << 1
        }
    }
}
