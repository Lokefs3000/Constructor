using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Widgets;
using Hexa.NET.ImGui;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Inspector.Views;

namespace VoxelizationDemo.Editor.Windows.Inspector.Widgets
{
    public sealed class InspectorWidgetField : InspectorWidget
    {
        private float _topMargin;
        private float _labelMargin;

        private string _displayName;

        internal InspectorWidgetField()
        {
            _topMargin = 0.0f;
            _labelMargin = 0.0f;

            _displayName = string.Empty;
        }

        internal override void DestroySelf()
        {
        }

        internal override void ClearForPooling()
        {
            base.ClearForPooling();
        }

        internal void SetupForDisplay(ViewField? viewData)
        {
            base.SetupForDisplay(viewData);

            if (viewData != null)
            {
                _topMargin = viewData.RelativeOffset.Y;
                _labelMargin = viewData.RelativeOffset.X;

                _displayName = viewData.DisplayName;
            }
        }

        internal unsafe void DrawSelf(ImDrawListPtr drawList, Vector2 cursorPos, Vector2 cursorPosMax)
        {
            ImGuiP.RenderTextEllipsis(drawList, cursorPos, cursorPosMax, cursorPosMax.X, _displayName, (byte*)null, null);
        }

        public float TopMargin => _topMargin;
        public float LabelMargin => _labelMargin;

        public string DisplayName => _displayName;
    }
}
