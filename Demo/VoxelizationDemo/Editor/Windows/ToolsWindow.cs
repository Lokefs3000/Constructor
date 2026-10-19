using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Threading;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Input;
using VoxelizationDemo.Editor.Tools;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class ToolsWindow : IEditorWindow
    {
        private readonly TextureAtlasAsset _toolIconsAtlas;
        private bool _isPinned;

        private readonly KeybindContext _keybindContext;

        public ToolsWindow()
        {
            _toolIconsAtlas = AssetManager.LoadAsset<TextureAtlasAsset>("Content/Textures/Editor/ToolIcons.atlas");
            _isPinned = true;

            _keybindContext = new KeybindContext();
            _keybindContext.DeclareBinding("Translate tool", OnTranslateToolKeybind, KeyCode.T);
            _keybindContext.DeclareBinding("Rotate tool", OnRotateToolKeybind, KeyCode.E);
            _keybindContext.DeclareBinding("Scale tool", OnScaleToolKeybind, KeyCode.R);

            ThreadHelper.ExecuteOnMainThread(() => EditorManager.Instance.KeybindManager.FocusContext(_keybindContext));
        }

        private void OnTranslateToolKeybind() => EditorManager.Instance.ToolManager.ChangeTool<TranslateTool>();
        private void OnRotateToolKeybind() => EditorManager.Instance.ToolManager.ChangeTool<RotateTool>();
        private void OnScaleToolKeybind() => EditorManager.Instance.ToolManager.ChangeTool<ScaleTool>();

        public bool UpdateAndRender()
        {
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;
            if (_isPinned)
                windowFlags |= ImGuiWindowFlags.NoMove;

            if (ImGui.Begin("TOOLS"u8, windowFlags))
            {
                if (_isPinned)
                    ImGuiP.BringWindowToDisplayFront(ImGuiP.GetCurrentWindow());

                ToolManager tools = EditorManager.Instance.ToolManager;

                if (ImGui.BeginTable("###BTNS"u8, 2))
                {
                    unsafe
                    {
                        ImGui.TableSetupColumn((byte*)null, ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Unpin"u8).X + ImGui.GetStyle().FramePadding.X * 2.0f);
                        ImGui.TableSetupColumn((byte*)null, ImGuiTableColumnFlags.WidthFixed, 24.0f);
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if (ToolButton("TRANSLATE"u8, 'T', _toolIconsAtlas.TryFindSpriteOrNull("Translate"), tools.ActiveTool is TranslateTool))
                        tools.ChangeTool<TranslateTool>();

                    ImGui.TableNextColumn();
                    ToolButton("SPACE"u8, '\0', tools.RelativeMode.HasFlags(ToolRelativeMode.World) ? _toolIconsAtlas.TryFindSpriteOrNull("SpaceWorld") : _toolIconsAtlas.TryFindSpriteOrNull("SpaceLocal"), false);
                    if (ImGui.BeginPopupContextItem(ImGuiPopupFlags.MouseButtonLeft))
                    {
                        if (ToolButton("LOCAL"u8, '\0', _toolIconsAtlas.TryFindSpriteOrNull("SpaceLocal"), tools.RelativeMode.HasFlags(ToolRelativeMode.Local), 32.0f))
                        {
                            tools.RelativeMode = ToolRelativeMode.Local;
                            ImGui.CloseCurrentPopup();
                        }

                        if (ToolButton("WORLD"u8, '\0', _toolIconsAtlas.TryFindSpriteOrNull("SpaceWorld"), tools.RelativeMode.HasFlags(ToolRelativeMode.World), 32.0f))
                        {
                            tools.RelativeMode = ToolRelativeMode.World;
                            ImGui.CloseCurrentPopup();
                        }

                        ImGuiP.BringWindowToDisplayFront(ImGuiP.GetCurrentWindow());
                        ImGui.EndPopup();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if (ToolButton("ROTATE"u8, 'E', _toolIconsAtlas.TryFindSpriteOrNull("Rotate"), tools.ActiveTool is RotateTool))
                        tools.ChangeTool<RotateTool>();

                    ImGui.TableNextColumn();
                    ToolButton("ORIGIN"u8, '\0', tools.OriginMode.HasFlags(ToolOriginMode.First) ? _toolIconsAtlas.TryFindSpriteOrNull("OriginFirst") : (tools.OriginMode.HasFlags(ToolOriginMode.Centered) ? _toolIconsAtlas.TryFindSpriteOrNull("OriginCenter") : _toolIconsAtlas.TryFindSpriteOrNull("OriginLast")), false);
                    if (ImGui.BeginPopupContextItem(ImGuiPopupFlags.MouseButtonLeft))
                    {
                        if (ToolButton("FIRST"u8, '\0', _toolIconsAtlas.TryFindSpriteOrNull("OriginFirst"), tools.OriginMode.HasFlags(ToolOriginMode.First), 32.0f))
                        {
                            tools.OriginMode = ToolOriginMode.First;
                            ImGui.CloseCurrentPopup();
                        }

                        if (ToolButton("CENTER"u8, '\0', _toolIconsAtlas.TryFindSpriteOrNull("OriginCenter"), tools.OriginMode.HasFlags(ToolOriginMode.Centered), 32.0f))
                        {
                            tools.OriginMode = ToolOriginMode.Centered;
                            ImGui.CloseCurrentPopup();
                        }

                        if (ToolButton("LAST"u8, '\0', _toolIconsAtlas.TryFindSpriteOrNull("OriginLast"), tools.OriginMode.HasFlags(ToolOriginMode.Centered), 32.0f))
                        {
                            tools.OriginMode = ToolOriginMode.Last;
                            ImGui.CloseCurrentPopup();
                        }

                        ImGuiP.BringWindowToDisplayFront(ImGuiP.GetCurrentWindow());
                        ImGui.EndPopup();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if (ToolButton("SCALE"u8, 'R', _toolIconsAtlas.TryFindSpriteOrNull("Scale"), tools.ActiveTool is ScaleTool))
                        tools.ChangeTool<ScaleTool>();

                    ImGui.TableNextColumn();
                    if (ToolButton("SNAPPING"u8, '\0', tools.DefaultSnap ? _toolIconsAtlas.TryFindSpriteOrNull("SnapOn") : _toolIconsAtlas.TryFindSpriteOrNull("SnapOff"), tools.IsSnappingActive))
                        tools.DefaultSnap = !tools.DefaultSnap;

                    ImGui.EndTable();
                }

                ImGui.Separator();

                if (ImGui.SmallButton(_isPinned ? "Unpin###PIN_BTN"u8 : " Pin ###PIN_BTN"u8))
                    _isPinned = !_isPinned;

                ImGui.Separator();

                bool defaultSnap = tools.DefaultSnap;
                float snapScale = tools.SnapScale;

                if (ImGui.Checkbox("##DEFSNAP"u8, ref defaultSnap))
                    tools.DefaultSnap = defaultSnap;

                ImGui.SameLine();
                float cursorX = ImGui.GetCursorScreenPos().X;
                
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputFloat("##SNAPVAL"u8, ref snapScale))
                    tools.SnapScale = Math.Max(snapScale, 0.001f);

                snapScale = tools.SnapAngularScale;

                ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos().WithElement(0, cursorX));
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputFloat("##ANGLESNAPVAL"u8, ref snapScale))
                    tools.SnapAngularScale = Math.Max(snapScale, 0.125f);
            }
            ImGui.End();

            return true;
        }

        private static bool ToolButton(ReadOnlySpan<byte> str, char keybind, Sprite? sprite, bool isActive, float forcedWidth = -1.0f)
        {
            EditorManager editorManager = VoxelRuntime.Instance.EditorManager;
            ImGuiContextPtr context = editorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            float currentWidth = forcedWidth > 0.0f ? forcedWidth - context.Style.FramePadding.X * 2.0f : Math.Max(ImGui.GetContentRegionAvail().X - context.Style.FramePadding.X * 2.0f, 24.0f);

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

            if (keybind != '\0')
            {
                ImFontGlyphPtr glyph = ImGui.FindGlyph(context.Font.LastBaked, keybind);
                Vector2 charMin = bb.Max - context.Style.FramePadding - new Vector2(glyph.AdvanceX, context.FontSize);

                window.DrawList.AddRectFilled(charMin - context.Style.FramePadding, bb.Max, 0x40000000);
                context.Font.RenderChar(window.DrawList, context.FontSize, charMin, 0xffffffff, keybind);
            }

            ImGuiP.ItemAdd(bb, id);
            ImGuiP.ItemSize(bb.Max - bb.Min, context.Style.FramePadding.Y);

            return isPressed;
        }
    }
}
