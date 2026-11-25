using Editor.Gui.View.Elements;
using Editor.Gui.Windows;
using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Serialization;
using Primary.Assets;
using Primary.Reflection;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Editor.Gui.View
{
    public readonly record struct ToolbarSetupContext(EditorViewWindow Window)
    {
        public ToolbarGroup LoadGroup(string path)
        {
            ToolbarItemSerializable[]? items = null;
            {
                using Stream? stream = AssetFilesystem.OpenStream(path) ?? throw new ToolbarSerilizationException($"Failed to open file stream {path}");
                items = JsonSerializer.Deserialize(stream, ToolbarItemJsonContext.Default.ToolbarItemSerializableArray) ?? throw new ToolbarSerilizationException($"Failed to deserialize source {path}"); ;
            }

            UIElement rootElement = new UIElement();
            {
                UIListLayout listLayout = rootElement.AddLayoutModifier<UIListLayout>();
                listLayout.Direction = UIListLayoutDirection.Horizontal;
                listLayout.Padding = new UIValue(4);

                UIFitterLayout fitterLayout = rootElement.AddLayoutModifier<UIFitterLayout>();
                fitterLayout.Axis = UIFitterAxis.Horizontal;

                rootElement.Size = new UIValue2(0.0f, 1.0f);
            }

            UIFontAsset fontAsset = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont");

            Dictionary<string, ToolbarItem> itemDict = new Dictionary<string, ToolbarItem>();
            foreach (ToolbarItemSerializable item in items)
            {
                ToolbarItem element;

                if (item.Logic is EnumLogicSerializable @enum)
                {
                    Type enumType = TypeKeyReflector.KeyToType(@enum.Type) ?? throw new ToolbarSerilizationException($"Failed to find type for enum {@enum.Type}");
                    if (!enumType.IsEnum)
                        throw new ToolbarSerilizationException($"Specified type is not an enum {enumType}");

                    element = new ToolbarEnumItem(rootElement) { EnumType = enumType };
                }
                else if (item.Logic is DropdownLogicSerializable dropdown)
                {
                    LayoutSnippet layoutSnippet = UIManager.Instance.CreateSnippet<LayoutSnippet>(dropdown.Path);
                    element = new ToolbarDropdownItem(rootElement) { DropdownSnippet = layoutSnippet, IsToggleable = dropdown.IsToggleable };
                }
                else
                    throw new ToolbarSerilizationException($"Unknown item logic {item.Logic}");

                element.Font = fontAsset;
                element.Size = new UIValue2(0.0f, 1.0f);

                if (item.Appearances.Length == 0)
                {
                    element.InitializeAppearanceArray(1);
                    element.SetAppearanceData(0, "_", default);
                }
                else
                {
                    element.InitializeAppearanceArray(item.Appearances.Length);

                    for (int i = 0; i < item.Appearances.Length; i++)
                    {
                        AppearanceSerializable appearance = item.Appearances[i];
                        element.SetAppearanceData(i, appearance.Key ?? "_", new ToolbarItemAppearance(appearance.Text, null));
                    }
                }

                itemDict[item.Id] = element;
            }

            return new ToolbarGroup(rootElement, itemDict.ToFrozenDictionary());
        }
    }

    public sealed class ToolbarSerilizationException : Exception
    {
        public ToolbarSerilizationException()
        {
        }

        public ToolbarSerilizationException(string? message) : base(message)
        {
        }
    }
}
