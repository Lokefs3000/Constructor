using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Widgets;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Inspector.Views;

namespace VoxelizationDemo.Editor.Windows.Inspector.Widgets
{
    public sealed class InspectorWidgetPreset : InspectorWidget
    {
        private readonly WidgetRelationship _relationships;

        private float _topMargin;
        private float _labelMargin;

        private string _displayName;
        private string[] _presetValues;

        internal InspectorWidgetPreset()
        {
            _relationships = new WidgetRelationship();

            _topMargin = 0.0f;
            _labelMargin = 0.0f;

            _displayName = string.Empty;
            _presetValues = [];
        }

        internal override void DestroySelf()
        {
            _relationships.Destroy();
        }

        internal override void ClearForPooling()
        {
            base.ClearForPooling();

            _presetValues = [];
        }

        internal void SetupForDisplay(ViewPreset? viewData)
        {
            base.SetupForDisplay(viewData);

            if (viewData != null)
            {
                _topMargin = viewData.RelativeOffset.Y;
                _labelMargin = viewData.RelativeOffset.X;

                _displayName = viewData.DisplayName;
                _presetValues = [.. viewData.Presets.Keys];
            }
            else
            {
                _presetValues = [];
            }
        }

        internal WidgetRelationship Relationships => _relationships;
    }
}
