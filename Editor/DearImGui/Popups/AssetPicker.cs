using CommunityToolkit.HighPerformance;
using Editor.Storage;
using Hexa.NET.ImGui;
using Primary.Assets;
using System.Runtime.InteropServices;

namespace Editor.DearImGui.Popups
{
    internal sealed class AssetPicker<T> : IPopup where T : class, IAssetDefinition
    {
        private Action<T> _callback;

        private string _searchQuery;
        private List<Cached> _searched;

        private bool _isFirstFrame;

        internal AssetPicker(Action<T> callback)
        {
            _callback = callback;

            _searchQuery = string.Empty;
            _searched = new List<Cached>();

            _isFirstFrame = true;

            SelectAssetsBasedOnSearch(ReadOnlySpan<char>.Empty);
        }

        public bool Render()
        {
            if (ImGui.BeginPopup("Asset picker", ImGuiWindowFlags.NoMove))
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
                        T asset = AssetManager.LoadAsset<T>(data.Id, true);
                        _callback.Invoke(asset);

                        ImGui.CloseCurrentPopup();
                    }
                }

                if (prev != AssetId.Invalid)
                    ImGui.PopID();

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

            AssetCategoryDatabase? category = Editor.GlobalSingleton.AssetDatabase.GetCategory<T>(false);
            if (category == null)
            {
                EdLog.Gui.Warning("{t} category is not within database", typeof(T));
                return;
            }

            if (search.IsEmpty)
            {
                foreach (AssetDatabaseEntry entry in category.Entries)
                {
                    _searched.Add(new Cached(Path.GetFileNameWithoutExtension(entry.LocalPath), entry.Id));
                }
            }
            else
            {
                foreach (AssetDatabaseEntry entry in category.Entries)
                {
                    if (entry.LocalPath.AsSpan().Contains(search, StringComparison.OrdinalIgnoreCase))
                        _searched.Add(new Cached(Path.GetFileNameWithoutExtension(entry.LocalPath), entry.Id));
                }

            }

            _searched.Sort((x, y) => x.Id.CompareTo(y.Id));
        }

        private readonly record struct Cached(string LocalPath, AssetId Id);
    }
}
