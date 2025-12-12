using Editor.Assets;
using Editor.Storage;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.DearImGui.Debuggers
{
    internal sealed class AssetDatabaseDebugger : IVisualDebugger
    {
        private Type? _activeCategory;

        internal AssetDatabaseDebugger()
        {
            _activeCategory = null;
        }

        public void Render()
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            Vector2 avail = ImGui.GetContentRegionAvail();

            AssetDatabase database = Editor.GlobalSingleton.AssetDatabase;

            if (_activeCategory == null)
            {
                AssetCategoryDatabase? category = database.Categories.FirstOrDefault();
                if (category == null)
                {
                    ImGui.Text("Database empty"u8);
                    return;
                }

                _activeCategory = category.AssetType;
            }

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu(_activeCategory.Name))
                {
                    foreach (AssetCategoryDatabase category in database.Categories)
                    {
                        if (ImGui.MenuItem(category.AssetType.Name, category.AssetType == _activeCategory))
                            _activeCategory = category.AssetType;
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            if (_activeCategory != null)
            {
                AssetCategoryDatabase? category = database.GetCategory(_activeCategory, false);
                if (category == null)
                {
                    _activeCategory = null;
                    return;
                }

                ImGui.BeginTable("ENTRIES"u8, 3, ImGuiTableFlags.Borders);

                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted("Id"u8);

                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted("Local path"u8);

                    ImGui.TableSetColumnIndex(2);
                    ImGui.TextUnformatted("Is imported?"u8);
                }

                foreach (AssetDatabaseEntry entry in category.Entries)
                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextUnformatted(entry.Id.ToString());

                    ImGui.TableSetColumnIndex(1);
                    ImGui.TextUnformatted(entry.LocalPath);

                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.TextUnformatted(entry.LocalPath);
                        ImGui.EndTooltip();
                    }

                    ImGui.TableSetColumnIndex(2);
                    if (entry.IsImported)
                        ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "Yes"u8);
                    else
                        ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), "No"u8);
                }

                ImGui.EndTable();
            }
        }

        public VisualDebuggerType DebuggerType => VisualDebuggerType.Editor;
        public ReadOnlySpan<byte> DebuggerName => "Asset DB"u8;
    }
}
