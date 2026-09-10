using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Assets.Types;
using Primary.Windowing;
using PrimaryEditor.Assets;
using PrimaryEditor.Search;
using PrimaryEditor.Search.Items;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Popups
{
    internal sealed class ChooseAssetPopup : EditorPopup, IDisposable
    {
        private readonly SearchQuery _query;
        private bool _isNew;

        private SearchList? _searchList;
        private DateTime _lastSearchTime;

        internal ChooseAssetPopup(SearchQuery query)
        {
            _query = query;
            _isNew = true;

            _searchList = null;
            _lastSearchTime = DateTime.MinValue;
        }

        void IDisposable.Dispose()
        {
            _searchList?.Return();
            VoxelRuntime.Instance.EditorManager.SearchManager.ReturnPooledSearchQuery(_query);
        }

        public override bool UpdateAndRender()
        {
            bool isOpen = true;
            if (ImGui.BeginPopupModal("Choose asset"u8, ref isOpen, ImGuiWindowFlags.HorizontalScrollbar))
            {
                string searchText = _query.Text ?? string.Empty;

                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.InputText("##SEARCH_TEXT"u8, ref searchText, 256))
                {
                    _query.Text = searchText;
                    _lastSearchTime = DateTime.Now;
                }

                if (_lastSearchTime < DateTime.Now)
                {
                    _searchList?.Return();
                    _searchList = VoxelRuntime.Instance.EditorManager.SearchManager.Search(_query);

                    _lastSearchTime = DateTime.MaxValue;
                }

                DrawSearchListItems();

                ImGui.EndPopup();
            }

            if (_isNew)
            {
                ImGui.OpenPopup("Choose asset"u8);
                _isNew = false;
                return true;
            }

            return isOpen;
        }

        private void DrawSearchListItems()
        {
            if (_searchList != null && _searchList.Items.Count > 0)
            {
                ImGuiContextPtr context = VoxelRuntime.Instance.EditorManager.ContextManager.ImGuiCtx;
                ImGuiWindowPtr window = context.CurrentWindow;

                float itemHeight = context.FontSize + context.Style.FramePadding.Y;
                Vector2 contentAvail = ImGui.GetContentRegionAvail();

                float scrollAmount = Math.Max(window.Scroll.Y - ImGui.GetCursorPosY() + itemHeight, 0.0f);

                Vector2 cursorPosBackup = ImGui.GetCursorScreenPos();
                ImGui.Dummy(new Vector2(1.0f, _searchList.Items.Count * itemHeight));

                int itemStartIndex = (int)Math.Floor(scrollAmount / itemHeight);
                int itemMaxIndex = Math.Clamp((int)Math.Ceiling((scrollAmount + contentAvail.Y) / itemHeight), itemStartIndex, _searchList.Items.Count - 1);

                ReadOnlySpan<SearchItem> items = _searchList.Items.AsSpan()[itemStartIndex..itemMaxIndex];

                ImGui.SetCursorScreenPos(new Vector2(cursorPosBackup.X, cursorPosBackup.Y + itemStartIndex * itemHeight));
                foreach (SearchItem item in items)
                {
                    if (item is AssetSearchItem assetSearchItem)
                    {
                        if (ImGui.MenuItem(assetSearchItem.FilePath))
                        {
                            FinishAndSetValue(new AssetChosenResponse(assetSearchItem.AssetId, null));
                            ImGui.CloseCurrentPopup();
                        }
                    }
                }
            }
        }

        public record class AssetChosenResponse(AssetId Id, string? Key);
    }
}
