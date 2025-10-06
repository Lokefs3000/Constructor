using Editor.Gui;
using Hexa.NET.ImGui;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Editor.DearImGui.Debuggers
{
    internal class DynAtlasDebugger : IVisualDebugger
    {
        private int _activeAtlasIndex;

        public void Render()
        {
            DynamicAtlasManager atlasManager = Editor.GlobalSingleton.GuiAtlasManager;

            _activeAtlasIndex = Math.Clamp(_activeAtlasIndex, 0, atlasManager.SubAtlasses.Count - 1);

            if (ImGui.BeginMenuBar())
            {
                ImGui.SliderInt("##ATLASIDX", ref _activeAtlasIndex, 0, atlasManager.SubAtlasses.Count - 1);
                ImGui.EndMenuBar();
            }

            DynamicSubAtlas subAtlas = atlasManager.SubAtlasses[_activeAtlasIndex];

            DynamicIconSet? hoveredSet = null;
            string? hoveredIcon = null;

            if (ImGui.BeginChild("##ICS", new Vector2(ImGui.GetContentRegionAvail().X * 0.25f, 0.0f), ImGuiChildFlags.Borders, ImGuiWindowFlags.HorizontalScrollbar))
            {
                foreach (DynamicIconSet iconSet in atlasManager.IconSets)
                {
                    if (iconSet.SubAtlas == subAtlas)
                    {
                        bool node = ImGui.TreeNode(iconSet.UniqueHash.ToString());
                        if (ImGui.IsItemHovered())
                            hoveredSet = iconSet;

                        if (node)
                        {

                            foreach (string icon in iconSet.Icons)
                            {
                                ImGui.Selectable(icon.Substring(icon.LastIndexOf('/') + 1));

                                if (ImGui.IsItemHovered())
                                {
                                    hoveredSet = iconSet;
                                    hoveredIcon = icon;
                                }

                                if (ImGui.BeginItemTooltip())
                                {
                                    iconSet.TryGetAtlasIcon(icon, out DynAtlasIcon atlasIcon);

                                    ImGui.EndTooltip();
                                }
                            }

                            ImGui.TreePop();
                        }
                    }
                }

                ImGui.EndChild();
            }

            ImGui.SameLine();

            if (ImGui.BeginChild("##VIEW", ImGuiChildFlags.Borders))
            {
                ImGuiContextPtr context = ImGui.GetCurrentContext();
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                Vector2 cursor = ImGui.GetCursorScreenPos();
                Vector2 avail = ImGui.GetContentRegionAvail();

                Vector2 imageSize = new Vector2(MathF.Min(avail.X, avail.Y));
                if (avail.X < avail.Y)
                    cursor.Y += (avail.Y - imageSize.Y) * 0.5f;
                else
                    cursor.X += (avail.X - imageSize.X) * 0.5f;

                const int individualTileSize = 32;
                int tileXYCount = (int)MathF.Ceiling(imageSize.X / (float)individualTileSize);

                drawList.PushClipRect(cursor, cursor + imageSize);

                for (int y = 0; y < tileXYCount; y++)
                {
                    for (int x = 0; x < tileXYCount; x++)
                    {
                        Vector2 baseCursor = cursor + new Vector2(x, y) * individualTileSize;
                        int offset = (x + y) % 2;

                        drawList.AddRectFilled(baseCursor, baseCursor + new Vector2(individualTileSize), offset == 0 ? 0xff202020 : 0xff242424);
                    }
                }

                drawList.PopClipRect();

                drawList.AddImage(ImGuiUtility.GetTextureRef(subAtlas.AtlasTexture.Handle), cursor, cursor + imageSize);

                if (hoveredSet == null)
                {
                    foreach (DynamicIconSet iconSet in atlasManager.IconSets)
                    {
                        if (iconSet.SubAtlas == subAtlas)
                        {
                            uint color = (uint)((uint)iconSet.UniqueHash | 0xff000000u);
                            foreach (string icon in iconSet.Icons)
                            {
                                iconSet.TryGetAtlasIcon(icon, out DynAtlasIcon atlasIcon);
                                drawList.AddRect(atlasIcon.UVs.Minimum * imageSize + cursor, atlasIcon.UVs.Maximum * imageSize + cursor, color);
                            }
                        }
                    }
                }
                else
                {
                    uint color = (uint)(hoveredSet.UniqueHash | 0xff000000u);

                    if (hoveredIcon == null)
                    {
                        foreach (string icon in hoveredSet.Icons)
                        {
                            hoveredSet.TryGetAtlasIcon(icon, out DynAtlasIcon atlasIcon);
                            drawList.AddRect(atlasIcon.UVs.Minimum * imageSize + cursor, atlasIcon.UVs.Maximum * imageSize + cursor, color);
                        }
                    }
                    else
                    {
                        hoveredSet.TryGetAtlasIcon(hoveredIcon, out DynAtlasIcon atlasIcon);
                        drawList.AddRect(atlasIcon.UVs.Minimum * imageSize + cursor, atlasIcon.UVs.Maximum * imageSize + cursor, color);
                    }
                }

                ImGui.EndChild();
            }
        }

        public VisualDebuggerType DebuggerType => VisualDebuggerType.Editor;
        public ReadOnlySpan<byte> DebuggerName => "Dynamic atlas"u8;
    }
}
