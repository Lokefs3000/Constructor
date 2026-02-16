using CommunityToolkit.HighPerformance;
using Editor.Storage;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Assets.Types;
using System.Runtime.InteropServices;

namespace Editor.DearImGui.Popups
{
    internal sealed class ModelAssetPicker : IPopup
    {
        private Action<RenderMesh> _callback;

        private string _searchQuery;
        private List<Cached> _searched;

        private bool _isFirstFrame;

        internal ModelAssetPicker(Action<RenderMesh> callback)
        {
            _callback = callback;

            _searchQuery = string.Empty;
            _searched = new List<Cached>();

            _isFirstFrame = true;

            SelectAssetsBasedOnSearch(ReadOnlySpan<char>.Empty);
        }

        public bool Render()
        {
            if (ImGui.BeginPopup("Model picker", ImGuiWindowFlags.NoMove))
            {
                if (_isFirstFrame)
                    ImGui.SetKeyboardFocusHere();
                if (ImGui.InputText("##SEARCH", ref _searchQuery, 128))
                    SelectAssetsBasedOnSearch(_searchQuery);

                AssetId prev = AssetId.Invalid;

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

                    if (ImGui.Selectable(data.LocalPath))
                    {
                        ModelAsset model = AssetManager.LoadAsset<ModelAsset>(data.Id, true);
                        _callback.Invoke(model.GetRenderMesh(data.LocalPath));

                        ImGui.CloseCurrentPopup();
                    }
                }

                if (prev != AssetId.Invalid)
                    ImGui.PopID();

                ImGui.EndPopup();
            }

            if (_isFirstFrame)
            {
                ImGui.OpenPopup("Model picker");
                _isFirstFrame = !ImGui.IsPopupOpen("Model picker");
            }

            return _isFirstFrame || ImGui.IsPopupOpen("Model picker");
        }

        private void SelectAssetsBasedOnSearch(ReadOnlySpan<char> search)
        {
            _searched.Clear();

            AssetCategoryDatabase? category = Editor.GlobalSingleton.AssetDatabase.GetCategory<RenderMesh>(false);
            if (category == null)
            {
                EdLog.Gui.Warning("{t} category is not within database", typeof(RenderMesh));
                return;
            }

            if (search.IsEmpty)
            {
                foreach (AssetDatabaseEntry entry in category.Entries)
                {
                    _searched.Add(new Cached(entry.LocalPath, entry.Id));
                }
            }
            else
            {
                foreach (AssetDatabaseEntry entry in category.Entries)
                {
                    if (entry.LocalPath.AsSpan().Contains(search, StringComparison.OrdinalIgnoreCase))
                        _searched.Add(new Cached(entry.LocalPath, entry.Id));
                }

            }

            _searched.Sort((x, y) => x.Id.CompareTo(y.Id));
        }

        private readonly record struct Cached(string LocalPath, AssetId Id);
    }
}
