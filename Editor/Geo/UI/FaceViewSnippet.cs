using Editor.Geometry;
using Editor.Gui.View;
using Editor.Gui.Windows;
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
    internal class FaceViewSnippet : ViewSnippet
    {
        private readonly GeoHierchyWindow _owner;

        private UIButton? _visibleButton;
        private UIButton? _materialButton;

        public FaceViewSnippet(string? snippetFile, EditorViewWindow window, GeoHierchyWindow owner) : base(snippetFile, window)
        {
            _owner = owner;
        }

        protected override void SetupSelf()
        {
            _visibleButton = RootElement?.FindElementWithId<UIButton>("visible-btn");
            _materialButton = RootElement?.FindElementWithId<UIButton>("material-btn");

            _visibleButton?.OnMouseActivate += OnVisibleButtonPress;
            _materialButton?.OnMouseActivate += OnVisibleButtonPress;
        }

        protected override void CleanupSelf()
        {
            _visibleButton?.OnMouseActivate -= OnVisibleButtonPress;
            _materialButton?.OnMouseActivate -= OnVisibleButtonPress;
        }

        private void OnVisibleButtonPress(MouseButton button)
        {
            int newVisibleState = 0;

            
        }

        private void OnMaterialButtonPress(MouseButton button)
        {

        }
    }
}
