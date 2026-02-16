using Editor.DearImGui.Popups;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.DearImGui.LayoutDbg
{
    internal sealed class ImageInspector : IElementInspector
    {
        public void Inspect(UIElement element)
        {
            UIImage image = Unsafe.As<UIImage>(element);

            TextureAsset? imageAsset = image.Image;
            UIColor tintColor = image.TintColor;

            if (ImGui.Button(imageAsset?.Name ?? "null"))
            {
                Editor.GlobalSingleton.DearImGuiWindowManager.OpenPopup(new AssetPicker(typeof(TextureAsset), imageAsset?.Id ?? AssetId.Invalid, null, (id) =>
                {
                    image.Image = AssetManager.LoadAsset<TextureAsset>(id);
                }));
            }
            ImGui.SameLine();
            ImGui.Text("Image"u8);

            if (IElementInspector.InputUIColor("Tint color"u8, ref tintColor))
                image.TintColor = tintColor;
        }
    }
}
