using CommunityToolkit.HighPerformance;
using Editor.Gui;
using Editor.Storage;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Assets.Types;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Editor.DearImGui.Popups
{
    internal sealed class AssetPicker : IPopup
    {
        private static DynamicIconSet? _iconSet;

        private AssetId _refId;
        private Type _type;

        private Action<IAssetDefinition>? _callback;
        private Action<AssetId>? _callback2;

        private string _searchQuery;
        private List<Cached> _searched;

        private List<(Vector2 pos, float sz, uint color)> _iconBoundaries;

        private bool _isFirstFrame;

        internal AssetPicker(Type type, AssetId current, Action<IAssetDefinition>? callback, Action<AssetId>? callback2)
        {
            _refId = current;
            _type = type;

            _callback = callback;
            _callback2 = callback2;

            _searchQuery = string.Empty;
            _searched = new List<Cached>();

            _iconBoundaries = new List<(Vector2 pos, float sz, uint color)>();

            _isFirstFrame = true;

            SelectAssetsBasedOnSearch(ReadOnlySpan<char>.Empty);
        }

        public bool Render()
        {
            if (_iconSet == null)
            {
                DynamicAtlasManager atlasManager = Editor.GlobalSingleton.GuiAtlasManager;
                _iconSet = atlasManager.CreateIconSet(
                    "Editor/Textures/Icons/ImportIcon.png");

                atlasManager.TriggerRebuild();
            }

            if (ImGui.BeginPopup("Asset picker", ImGuiWindowFlags.NoMove))
            {
                if (_isFirstFrame)
                    ImGui.SetKeyboardFocusHere();
                if (ImGui.InputText("##SEARCH", ref _searchQuery, 128))
                    SelectAssetsBasedOnSearch(_searchQuery);

                AssetId prev = AssetId.Invalid;

                _iconBoundaries.Clear();

                Span<Cached> cached = _searched.AsSpan();
                for (int i = 0; i < cached.Length; i++)
                {
                    ref Cached data = ref cached[i];

                    if (prev != data.Id)
                    {
                        if (prev != AssetId.Invalid)
                            ImGui.PopID();

                        ulong id = data.Id.Value;
                        ImGui.PushID(MemoryMarshal.Cast<ulong, byte>(new ReadOnlySpan<ulong>(ref id)));

                        prev = data.Id;
                    }

                    if (IconSelectable(ref data, data.Id == _refId))
                    {
                        if (_callback != null)
                        {
                            IAssetDefinition asset = (AssetManager.LoadAsset(_type, data.Id, true) as IAssetDefinition)!;
                            _callback.Invoke(asset);
                        }
                        else if (_callback2 != null)
                        {
                            _callback2.Invoke(data.Id);
                        }

                        ImGui.CloseCurrentPopup();
                    }
                }

                if (prev != AssetId.Invalid)
                    ImGui.PopID();

                if (_iconSet.TryGetAtlasIcon("Editor/Textures/Icons/ImportIcon.png", out DynAtlasIcon atlasIcon))
                {
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                    //ImTextureRef @ref = ImGuiUtility.GetTextureRef(_iconSet.AtlasTexture.Handle);
                    //for (int i = 0; i < _iconBoundaries.Count; i++)
                    //{
                    //    (Vector2 pos, float sz, uint color) = _iconBoundaries[i];
                    //    drawList.AddImage(@ref, pos, pos + new Vector2(sz), atlasIcon.UVs.Minimum, atlasIcon.UVs.Maximum, color);
                    //}
                }

                ImGui.EndPopup();
            }

            if (_isFirstFrame)
            {
                ImGui.OpenPopup("Asset picker");
                _isFirstFrame = !ImGui.IsPopupOpen("Asset picker");
            }

            return _isFirstFrame || ImGui.IsPopupOpen("Asset picker");
        }

        private void SelectAssetsBasedOnSearch(ReadOnlySpan<char> search)
        {
            _searched.Clear();

            AssetCategoryDatabase? category = Editor.GlobalSingleton.AssetDatabase.GetCategory(_type, false);
            if (category == null)
            {
                EdLog.Gui.Warning("{t} category is not within database", _type);
                return;
            }

            bool searchOnlyImported = _callback != null;

            if (search.IsEmpty)
            {
                foreach (AssetDatabaseEntry entry in category.Entries)
                {
                    if (searchOnlyImported && !entry.IsImported)
                        continue;
                    _searched.Add(new Cached(Path.GetFileNameWithoutExtension(entry.LocalPath), entry.Id, entry.IsImported));
                }
            }
            else
            {
                foreach (AssetDatabaseEntry entry in category.Entries)
                {
                    if (searchOnlyImported && !entry.IsImported)
                        continue;
                    if (entry.LocalPath.AsSpan().Contains(search, StringComparison.OrdinalIgnoreCase))
                        _searched.Add(new Cached(Path.GetFileNameWithoutExtension(entry.LocalPath), entry.Id, entry.IsImported));
                }

            }

            _searched.Sort((x, y) => x.LocalPath.CompareTo(y.LocalPath));
        }

        private unsafe bool IconSelectable(ref Cached cached, bool selected)
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ref ImGuiWindowPtr currentWindow = ref context.CurrentWindow;

            uint id = ImGui.GetID(cached.LocalPath);
            Vector2 labelSize = ImGui.CalcTextSize(cached.LocalPath);
            Vector2 size = new Vector2(currentWindow.ContentSize.X, labelSize.Y);
            Vector2 pos = currentWindow.DC.CursorPos;

            pos.Y += currentWindow.DC.CurrLineTextBaseOffset;

            ImRect bb = new ImRect(pos, pos + size);

            {
                float spacingX = context.Style.ItemSpacing.X;
                float spacingY = context.Style.ItemSpacing.Y;
                float spacingL = float.Truncate(spacingX * 0.5f);
                float spacingU = float.Truncate(spacingY * 0.5f);

                bb.Min -= new Vector2(spacingL, spacingU);
                bb.Max += new Vector2(spacingX, spacingY) - new Vector2(spacingL, spacingU);
            }

            bool hovered = false, held = false;
            bool pressed = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref held);

            if (hovered || selected)
            {
                uint col = ImGui.GetColorU32((held && hovered) ? ImGuiCol.HeaderActive : hovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header);
                ImGuiP.RenderFrame(bb.Min, bb.Max, col, false);
            }
            if (context.NavId == id)
            {
                ImGuiNavRenderCursorFlags navRenderCursorFlags = ImGuiNavRenderCursorFlags.Compact | ImGuiNavRenderCursorFlags.NoRounding;
                ImGuiP.RenderNavCursor(bb, id, navRenderCursorFlags);
            }

            _iconBoundaries.Add((pos, labelSize.Y, cached.IsImported ? 0xff00ff00 : 0xff0000ff));
            pos.X += labelSize.Y + context.Style.ItemInnerSpacing.X;

            ImGuiP.RenderTextClipped(pos, new Vector2(MathF.Min(pos.X + size.X, currentWindow.WorkRect.Max.X), pos.Y + size.Y), cached.LocalPath, (byte*)null, &labelSize, context.Style.SelectableTextAlign, new ImRectPtr(&bb));

            ImGuiP.ItemAdd(bb, id);
            ImGuiP.ItemSize(size, 0.0f);

            return pressed;
        }

        private readonly record struct Cached(string LocalPath, AssetId Id, bool IsImported);
    }
}
