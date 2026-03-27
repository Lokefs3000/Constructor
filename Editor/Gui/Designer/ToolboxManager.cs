using Editor.UI;
using Editor.UI.Datatypes;
using Editor.UI.Designer;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Reflection;
using Primary.Assets;
using Primary.Input.Devices;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace Editor.Gui.Designer
{
    internal sealed class ToolboxManager
    {
        private readonly UIDesigner _designer;

        private UIElement? _currentActiveButton;

        internal ToolboxManager(UIDesigner designer)
        {
            _designer = designer;

            _currentActiveButton = null;

            //generate default
            UISplitPanel panel = designer.FindElementWithId<UISplitPanel>("ToolboxOwner") ?? throw new NullReferenceException("Failed to find toolbox panel");

            string source = AssetFilesystem.ReadString("Editor/Designer/Data/TypeIconData.toml") ?? throw new NullReferenceException("No type icons data");
            TomlTable table = TomlSerializer.Deserialize<TomlTable>(source) ?? throw new NullReferenceException("Failed to deserialize");

            TextureAsset texture = AssetManager.LoadAsset<TextureAsset>("Editor/Designer/Icons/TypeIconSheet.png").WaitIfNotLoaded();
            TextureAsset noPreview = AssetManager.LoadAsset<TextureAsset>("Editor/Textures/NoPreview.png").WaitIfNotLoaded();

            UIManager ui = UIDesignerEngine.Instance.UIManager;

            KeyValuePair<Type, CachedElementData>[] elements = ui.ReflectionManager.ElementCache.Cache.ToArray();
            elements.Sort((a, b) => a.Key.Name.CompareTo(b.Key.Name, StringComparison.Ordinal));

            foreach (var kvp in elements)
            {
                TomlTable? imageData = null;
                {
                    if (table.TryGetValue(kvp.Value.PrettyName, out object? value))
                        imageData = value as TomlTable;
                }

                UIButton button = new UIButton
                {
                    Parent = panel
                };

                {
                    UIFitterLayout fitterLayout = button.AddLayoutModifier<UIFitterLayout>();
                    fitterLayout.Axis = UIFitterAxis.Both;
                    fitterLayout.Margin = new UIValue2(4, 4);
                }
                
                UIImage image = new UIImage
                {
                    Parent = button,

                    IsActive = false,

                    Position = new UIValue2(0, 4),
                    Size = new UIValue2(16, 16),

                    Image = texture
                };

                if (imageData == null)
                {
                    image.Image = noPreview;
                }
                else
                {
                    image.UVMin = new Vector2((long)imageData["x"], (long)imageData["y"]) * 128.0f / new Vector2(texture.Width, texture.Height);
                    image.UVMax = image.UVMin + new Vector2(128.0f) / new Vector2(texture.Width, texture.Height);
                }

                UILabel label = new UILabel
                {
                    Parent = button,

                    IsActive = false,

                    Position = new UIValue2(22, 0),
                    AutoSize = UITextAutoSize.FitBoundsToText,
                    Text = kvp.Value.PrettyName
                };

                button.AddClass("toolbox-item");

                button.OnMouseRelease += (x) =>
                {
                    if (x == MouseButton.Left)
                        ElementButtonReleased(button, kvp.Key);
                };
            }
        }

        private void ElementButtonReleased(UIButton button, Type type)
        {
            if (_currentActiveButton != button)
            {
                _currentActiveButton?.RemoveClass("button-active");
                button.AddClass("button-active");

                _currentActiveButton = button;
            }

            _designer.CanvasManager.SetActiveElementType(type);
        }
    }
}
