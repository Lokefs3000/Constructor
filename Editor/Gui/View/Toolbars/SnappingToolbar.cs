using Editor.Gui.View.Elements;
using Editor.Gui.View.Snippets;
using Editor.Interaction;
using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Serialization;
using Primary.Assets;
using System.Diagnostics.CodeAnalysis;

namespace Editor.Gui.View.Toolbars
{
    [ToolbarButton(nameof(_snappingItem))]
    internal sealed class SnappingToolbar : IEditorViewToolbar
    {
        private ToolbarDropdownItem? _snappingItem;

        public void Initialize(bool isBeingReloaded)
        {
            if (!isBeingReloaded)
            {
                UIFontAsset fontAsset = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont");

                // snapping item
                if (_snappingItem != null)
                {
                    TextureAtlasAsset texture = AssetManager.LoadAsset<TextureAtlasAsset>("Editor/UI/Atlases/EditorViewToolbar.atlas", true);
                    SnappingSnippet snippet = UIManager.Instance.CreateSnippet<SnappingSnippet>("Editor/UI/EditorView/Dropdown/SnappingDropdown.snippet");

                    _snappingItem.Font = fontAsset;
                    _snappingItem.FontWeight = FontWeight.Lighter;

                    _snappingItem.Appearances = [
                        new ToolbarItemAppearance(null, texture.TryFindSpriteOrNull("SnappingOff")),
                        new ToolbarItemAppearance(null, texture.TryFindSpriteOrNull("SnappingOn")),
                        ];

                    _snappingItem.IsToggleable = true;

                    _snappingItem.DropdownSnippet = snippet;
                }
            }

            if (_snappingItem != null)
            {
                _snappingItem.OnToggled += OnSnappingPressed;
            }
        }

        public void Cleanup()
        {
            if (_snappingItem != null)
            {
                _snappingItem.OnToggled -= OnSnappingPressed;
            }
        }

        private void OnSnappingPressed(bool _)
        {
            bool newValue = !ToolManager.IsSnappingDefault;

            ToolManager.IsSnappingDefault = newValue;

            if (_snappingItem != null)
            {
                _snappingItem.IsToggled = newValue;
                _snappingItem.ChangeAppearance(newValue ? 1 : 0);
            }
        }

        public ToolbarGroup SetupToolbar(ToolbarSetupContext context)
        {
            ToolbarGroup group = context.LoadGroup("Editor/UI/EditorView/Toolbar/SnappingSpace.json");
            if (group.TryGetToolbarItem("snapping", out ToolbarItem? item) && item is ToolbarDropdownItem dropdownItem)
            {
                
            }

            return group;
        }

        public void CleanupToolbar(ToolbarGroup group)
        {

        }
    }
}
