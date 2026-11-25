using Editor.Geo.Selection;
using Editor.Geo.Tools;
using Editor.Geometry;
using Editor.Gui.View;
using Editor.Gui.Windows;
using Editor.Interaction;
using Editor.Interaction.Controls;
using Editor.Interaction.Tools;
using Editor.UI;
using Editor.UI.Elements;
using Editor.UI.Serialization;
using Primary.Common;
using Primary.Input.Devices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo.UI
{
    internal class ToolsViewSnippet : ViewSnippet
    {
        private UIToggleButton? _createButton;
        private UIToggleButton? _editBrushButton;
        private UIToggleButton? _editFaceButton;
        private UIToggleButton? _editVertexButton;

        public ToolsViewSnippet(string? snippetFile, EditorViewWindow window) : base(snippetFile, window)
        {
            ToolManager.SetTypeState<Brush>(false);
            ToolManager.SetTypeState<SelectedFace>(false);
        }

        protected override void SetupSelf()
        {
            _createButton = RootElement?.FindElementWithId<UIToggleButton>("create-btn");
            _editBrushButton = RootElement?.FindElementWithId<UIToggleButton>("edit-brush-btn");
            _editFaceButton = RootElement?.FindElementWithId<UIToggleButton>("edit-face-btn");
            _editVertexButton = RootElement?.FindElementWithId<UIToggleButton>("edit-vertex-btn");

            //_createButton?.OnPressed += static () => ToolManager.SwitchTool<CreateBrushTool>();
            _editBrushButton?.OnPressed += OnButtonPressed;
            _editFaceButton?.OnPressed += OnButtonPressed;
            _editVertexButton?.OnPressed += OnButtonPressed;

            ToolManager.OnTypeStateChanged += OnTypeStateChanged;
        }

        protected override void CleanupSelf()
        {
            ToolManager.OnTypeStateChanged -= OnTypeStateChanged;
        }

        private void OnButtonPressed()
        {
            AllowedSelections allowed = AllowedSelections.None;
            if (IsEditBrushActive)
                allowed |= AllowedSelections.Brush;
            if (IsEditFaceActive)
                allowed |= AllowedSelections.Face;
            if (IsEditVertexActive)
                allowed |= AllowedSelections.Vertex;

            EditorRuntime.GlobalSingleton.GeoSceneManager.SelectionGroup.UpdateAllowed(allowed);
            ToolManager.SetTypeState<Brush>(IsEditBrushActive || IsEditFaceActive || IsEditVertexActive);
        }

        private void OnTypeStateChanged(Type type, bool state)
        {
            
        }

        public bool IsEditBrushActive => _editBrushButton?.IsToggled ?? false;
        public bool IsEditFaceActive => _editFaceButton?.IsToggled ?? false;
        public bool IsEditVertexActive => _editVertexButton?.IsToggled ?? false;

        public bool IsAnyEditActive => IsEditBrushActive || IsEditFaceActive || IsEditVertexActive;
    }
}
