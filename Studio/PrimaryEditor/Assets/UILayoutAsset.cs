using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using EditorUI;
using EditorUI.Reflection;
using EditorUI.Reflection.Cache;
using EditorUI.Styling;
using EditorUI.Widgets;
using Primary.Assets.Types;
using PrimaryEditor.Exceptions;

namespace PrimaryEditor.Assets
{
    public sealed class UILayoutAsset : BaseAssetDefinition<UILayoutAsset, UILayoutData>
    {
        public UILayoutAsset(UILayoutData assetData) : base(assetData)
        {
        }

        public Widget? Instantiate()
        {
            WaitIfNotLoaded();

            if (Status == ResourceStatus.Success)
            {
                UIManager manager = UIManager.Instance;

                return InstantiateTree(AssetData.RootNode!, null);

                Widget? InstantiateTree(UILayoutNode layoutNode, Widget? parentWidget)
                {
                    if (!manager.ReflectionManager.WidgetDatabase.TryGetWidgetTypeFromName(layoutNode.WidgetTypeName, out Type? type))
                    {
                        throw new LayoutInstantiateException($"Failed to find type for widget '{layoutNode.WidgetTypeName}' ({AssetData.Name}:{layoutNode.SourcePosition.Line}:{layoutNode.SourcePosition.Position})");
                    }

                    bool isWidgetStylist = layoutNode.WidgetTypeName.EndsWith("Stylist");

                    WidgetCachedData cachedData = manager.ReflectionManager.WidgetPropertyCache.GetCachedData(type);
                    if (!isWidgetStylist && cachedData.Constructor == null)
                    {
                        throw new LayoutInstantiateException($"Failed to instatiate widget '{layoutNode.WidgetTypeName}' ({AssetData.Name}:{layoutNode.SourcePosition.Line}:{layoutNode.SourcePosition.Position})");
                    }

                    StyledObject obj = isWidgetStylist ?
                        parentWidget!.CreateStylist(type) :
                        (Widget)cachedData.Constructor!.Invoke(null);

                    foreach (string className in layoutNode.ClassList)
                    {
                        obj.TryAddClass(className);
                    }

                    foreach (UILayoutPropertyValue propertyValue in layoutNode.PropertyValues)
                    {
                        if (cachedData.TryGetPropertyData(propertyValue.Name, out PropertyData? propertyData))
                        {
                            if (manager.ValueSerializer.TryDeserialize(propertyData, propertyValue.Value, out object? value, out Exception? exception))
                            {
                                propertyData.Methods.SetIndirect!(obj, value);
                            }
                            else
                            {
                                throw new LayoutInstantiateException($"Failed to deserialize value '{propertyValue.Value}' on property '{propertyValue.Name}' ({AssetData.Name}:{layoutNode.SourcePosition.Line}:{layoutNode.SourcePosition.Position})", exception);
                            }
                        }
                        else
                        {
                            throw new LayoutInstantiateException($"Property '{propertyValue.Name}' does not exist ({AssetData.Name}:{layoutNode.SourcePosition.Line}:{layoutNode.SourcePosition.Position})");
                        }
                    }

                    if (!isWidgetStylist)
                    {
                        Widget widget = (Widget)obj;
                        foreach (UILayoutNode childNode in layoutNode.ChildNodes)
                        {
                            Widget? childWidget = InstantiateTree(childNode, widget);
                            if (childWidget != null)
                                widget.AddChild(childWidget);
                        }
                    }

                    return obj as Widget;
                }
            }

            return null;
        }

        public UILayoutNode? RootNode => Status == ResourceStatus.Success ? AssetData.RootNode : null;
        public ImmutableArray<StylesheetAsset> Stylesheets => Status == ResourceStatus.Success ? AssetData.Stylesheets : ImmutableArray<StylesheetAsset>.Empty;
    }

    public sealed class UILayoutData : BaseInternalAssetData<UILayoutAsset>
    {
        private UILayoutNode? _rootNode;
        private ImmutableArray<StylesheetAsset> _stylesheets;

        public UILayoutData(AssetId id) : base(id)
        {
            _rootNode = null;
            _stylesheets = default;
        }

        public override void Dispose()
        {
            base.Dispose();

            _rootNode = null;
            _stylesheets = default;
        }

        public void UpdateAssetData(UILayoutAsset asset, UILayoutNode layoutNode, ImmutableArray<StylesheetAsset> stylesheets)
        {
            base.UpdateAssetData(asset);

            _rootNode = layoutNode;
            _stylesheets = stylesheets;
        }

        public override void UpdateAssetFailed(UILayoutAsset asset)
        {
            base.UpdateAssetFailed(asset);

            _rootNode = null;
            _stylesheets = default;
        }

        public UILayoutNode? RootNode => _rootNode;
        public ImmutableArray<StylesheetAsset> Stylesheets => _stylesheets;
    }

    public record class UILayoutNode(string WidgetTypeName, ImmutableArray<string> ClassList, ImmutableArray<UILayoutPropertyValue> PropertyValues, ImmutableArray<UILayoutNode> ChildNodes, UILayoutSourcePosition SourcePosition);
    public readonly record struct UILayoutPropertyValue(string Name, string Value);
    public readonly record struct UILayoutSourcePosition(int Line, int Position);
}
