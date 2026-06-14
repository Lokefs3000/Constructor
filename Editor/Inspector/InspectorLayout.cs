using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using CommunityToolkit.HighPerformance;
using Editor.Gui.Inspector;
using Editor.Inspector.Layout;
using Primary.Common;

namespace Editor.Inspector
{
    internal sealed class InspectorLayout
    {
        private ImmutableArray<LayoutObject> _objects;

        internal InspectorLayout()
        {
            _objects = ImmutableArray<LayoutObject>.Empty;
        }

        internal void SetupLayoutData(InspectorManager manager, Type type, ICustomInspector inspector)
        {
            InspectorContext context = new InspectorContext();
            inspector.SetupInspectorData(context, type);

            InspectorTypeCache typeCache = manager.TypeCache;

            Dictionary<string, LayoutObject> layoutObjects = new Dictionary<string, LayoutObject>();
            List<LayoutObject> topLevelObjects = new List<LayoutObject>();

            foreach (PropertyFieldData fieldData in context.PropertyFields)
            {
                PathExpressionReader reader = new PathExpressionReader(fieldData.PathExpression);
                Type currentType = type;

                LayoutObject? parent = null;
                int previousIndex = 0;

                InspectorField field = default;
                while (true)
                {
                    previousIndex = reader.Index;
                    if (!reader.Read())
                    {
                        EdLog.Inspector.Error("Unexpected error in path expression syntax {p}", fieldData.PathExpression);
                        goto ContinueWithFail;
                    }

                    if (field.FieldOrProperty != null && (reader.Token == PathExpressionToken.EOF || reader.Token == PathExpressionToken.Name))
                    {
                        Type fieldType = field.FieldType;
                        if (fieldType.IsPointer)
                        {
                            EdLog.Inspector.Error("Pointer types are not supported in the inspector {p}", field.Name);
                            goto ContinueWithFail;
                        }

                        if (reader.Token != PathExpressionToken.EOF)
                        {
                            if (s_coreRuntimeTypes.Contains(fieldType))
                            {
                                EdLog.Inspector.Error("The built-in system types cannot be indexed {p}", field.Name);
                                goto ContinueWithFail;
                            }
                        }

                        string path = fieldData.PathExpression[..(reader.Index == -1 ? ^0 : previousIndex)];
                        if (!layoutObjects.TryGetValue(path, out LayoutObject? @object))
                        {
                            @object = new LayoutObject(reader.Token == PathExpressionToken.EOF ? fieldData.PropertyName : null, field, !s_coreRuntimeTypes.Contains(fieldType));
                            layoutObjects.Add(path, @object);

                            if (parent == null)
                                topLevelObjects.Add(@object);
                        }
                        else if (reader.Token == PathExpressionToken.EOF)
                        {
                            if (@object.PropertyName == null)
                                @object.PropertyName = fieldData.PropertyName;
                            else
                            {
                                EdLog.Inspector.Error("There has already been an property with the path {p}", fieldData.PathExpression);
                                goto ContinueWithFail;
                            }
                        }

                        parent?.AddChild(@object);
                        parent = @object;
                    }

                    if (reader.Token == PathExpressionToken.GetItem)
                    {
                        if (field.FieldOrProperty == null)
                        {
                            EdLog.Inspector.Error("Expected target before get item path expression syntax {p}", fieldData.PathExpression);
                            goto ContinueWithFail;
                        }

                        MethodInfo? getItemMethod = null;
                        foreach (MethodInfo method in field.FieldType.GetMethods())
                        {
                            if (method.Name == "get_Item" && Flags.HasFlag(method.Attributes, MethodAttributes.Public))
                            {
                                ParameterInfo[] parameters = method.GetParameters();
                                if (parameters.SingleOrDefault()?.ParameterType == typeof(int))
                                {
                                    getItemMethod = method;
                                    break;
                                }
                            }
                        }

                        if (getItemMethod == null)
                        {
                            EdLog.Inspector.Error("No suitable getter found for {t} path expression syntax {p}", field.Name, fieldData.PathExpression);
                            goto ContinueWithFail;
                        }

                        field = new InspectorField(field.FieldOrProperty, reader.GetItemIndex());
                    }
                    else if (reader.Token == PathExpressionToken.Name)
                    {
                        if (reader.Token != PathExpressionToken.Name)
                        {
                            EdLog.Inspector.Error("Expected name token for {v} in path expression {p}", reader.ReadSpan.ToString(), fieldData.PathExpression);
                            goto ContinueWithFail;
                        }

                        FrozenDictionary<string, object> dict = typeCache.GetTypeCache(currentType);

                        string targetName = reader.GetName();
                        if (!dict.TryGetValue(targetName, out object? value))
                        {
                            EdLog.Inspector.Error("Failed to resolve target {t} in path expression {p}", targetName, fieldData.PathExpression);
                            goto ContinueWithFail;
                        }

                        field = new InspectorField(value);
                    }
                    else if (reader.Token == PathExpressionToken.EOF)
                    {
                        break;
                    }
                    else if (reader.Token != PathExpressionToken.Deliminator)
                    {
                        EdLog.Inspector.Error("Unexpected unknown token in path expression syntax {p}", fieldData.PathExpression);
                        goto ContinueWithFail;
                    }
                }

            ContinueWithFail:
                continue;
            }

            ResolveMissingNames(topLevelObjects.AsSpan());
            SortLayoutObjects(topLevelObjects.AsSpan());

            topLevelObjects.Sort(LayoutObjectCompararer.Default);

            _objects = topLevelObjects.ToImmutableArray();
        }

        internal ImmutableArray<LayoutObject> Objects => _objects;

        private static void ResolveMissingNames(ReadOnlySpan<LayoutObject> objects)
        {
            foreach (LayoutObject @object in objects)
            {
                if (@object.PropertyName == null)
                    @object.PropertyName = @object.Field.Name; // TODO: add a proper name resolver here based on the filed name

                if (@object.Children.Count > 0)
                {
                    ResolveMissingNames(@object.Children.AsSpan());
                }
            }
        }

        private static void SortLayoutObjects(ReadOnlySpan<LayoutObject> objects)
        {
            foreach (LayoutObject @object in objects)
            {
                @object.SortChildren(LayoutObjectCompararer.Default);

                if (@object.Children.Count > 0)
                {
                    SortLayoutObjects(@object.Children.AsSpan());
                }
            }
        }

        private static FrozenSet<Type> s_coreRuntimeTypes = [
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(bool),
            typeof(nint),
            typeof(nuint),
            typeof(char),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(string)
            ];

        private sealed class LayoutObjectCompararer : IComparer<LayoutObject>
        {
            public int Compare(LayoutObject? x, LayoutObject? y)
            {
                return x.PropertyName!.CompareTo(y.PropertyName!);
            }

            public static readonly LayoutObjectCompararer Default = new LayoutObjectCompararer();
        }
    }
}
