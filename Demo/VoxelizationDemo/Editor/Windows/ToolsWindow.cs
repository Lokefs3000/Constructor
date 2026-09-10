using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Assets;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Tools;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class ToolsWindow : IEditorWindow
    {
        private readonly TextureAtlasAsset _toolIconsAtlas;
        private bool _isPinned;

        public ToolsWindow()
        {
            _toolIconsAtlas = AssetManager.LoadAsset<TextureAtlasAsset>("Content/Textures/Editor/ToolIcons.atlas");
            _isPinned = true;
        }

        public bool UpdateAndRender()
        {
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;

            if (ImGui.Begin("TOOLS"u8, windowFlags))
            {
                if (_isPinned)
                    ImGuiP.BringWindowToDisplayFront(ImGuiP.GetCurrentWindow());

                ToolManager tools = EditorManager.Instance.ToolManager;

                if (ToolButton("TRANSLATE"u8, 'T', _toolIconsAtlas.TryFindSpriteOrNull("Translate"), tools.ActiveTool is TranslateTool))
                    tools.ChangeTool<TranslateTool>();
                ToolButton("ROTATE"u8, 'E', _toolIconsAtlas.TryFindSpriteOrNull("Rotate"), false);
                ToolButton("SCALE"u8, 'R', _toolIconsAtlas.TryFindSpriteOrNull("Scale"), false);

                ImGui.Separator();

                if (ImGui.SmallButton(_isPinned ? "Unpin###PIN_BTN"u8 : " Pin ###PIN_BTN"u8))
                    _isPinned = !_isPinned;
            }
            ImGui.End();

            return true;
        }

        private static bool ToolButton(ReadOnlySpan<byte> str, char keybind, Sprite? sprite, bool isActive)
        {
            EditorManager editorManager = VoxelRuntime.Instance.EditorManager;
            ImGuiContextPtr context = editorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            float currentWidth = Math.Max(ImGui.GetContentRegionAvail().X - context.Style.FramePadding.X * 2.0f, 24.0f);

            uint id = ImGui.GetID(str);
            ImRect bb = new ImRect(window.DC.CursorPos, window.DC.CursorPos + new Vector2(currentWidth, currentWidth * 0.85f) + context.Style.FramePadding * 2.0f);

            bool isHovered = false;
            bool isHeld = false;
            bool isPressed = ImGuiP.ButtonBehavior(bb, id, ref isHovered, ref isHeld);

            ImGuiCol col = isHeld ? ImGuiCol.ButtonActive : (isHovered ? ImGuiCol.ButtonHovered : (isActive ? ImGuiCol.Button : ImGuiCol.TabDimmed));
            window.DrawList.AddRectFilled(bb.Min, bb.Max, ImGui.GetColorU32(col), context.Style.FrameRounding);

            if (context.Style.FrameBorderSize > 0.0f)
                window.DrawList.AddRect(bb.Min, bb.Max, ImGui.GetColorU32(ImGuiCol.Border), context.Style.FrameRounding);

            if (sprite != null && sprite.Texture.IsLoaded)
            {
                float maxHeight = currentWidth * 0.85f;
                float sideOffset = (currentWidth - maxHeight) * 0.5f;

                ImRect imageBb = new ImRect(bb.Min + context.Style.FramePadding, bb.Max - context.Style.FramePadding);
                imageBb.Min.X += sideOffset;
                imageBb.Max.X -= sideOffset;

                window.DrawList.AddImage(
                    editorManager.HandleAllocator.GetHandleAsImTextureRef(sprite.Texture),
                    imageBb.Min,
                    imageBb.Max,
                    sprite.UVMin,
                    sprite.UVMax);
            }

            ImFontGlyphPtr glyph = ImGui.FindGlyph(context.Font.LastBaked, keybind);
            Vector2 charMin = bb.Max - context.Style.FramePadding - new Vector2(glyph.AdvanceX, context.FontSize);

            window.DrawList.AddRectFilled(charMin - context.Style.FramePadding, bb.Max, 0x40000000);
            context.Font.RenderChar(window.DrawList, context.FontSize, charMin, 0xffffffff, keybind);

            ImGuiP.ItemAdd(bb, id);
            ImGuiP.ItemSize(Vector2.Zero.WithElement(1, bb.Max.Y - bb.Min.Y), context.Style.FramePadding.Y);

            return isPressed;
        }
    }
}
