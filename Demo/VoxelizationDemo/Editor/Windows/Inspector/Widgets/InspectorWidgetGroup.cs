using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Widgets;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Inspector.Views;
using TerraFX.Interop.Windows;

namespace VoxelizationDemo.Editor.Windows.Inspector.Widgets
{
    public sealed class InspectorWidgetGroup : InspectorWidget
    {
        private readonly WidgetRelationship _relationships;

        private float _topMargin;

        internal InspectorWidgetGroup()
        {
            _relationships = new WidgetRelationship();

            _topMargin = 0.0f;
        }

        internal override void DestroySelf()
        {
            _relationships.Destroy();
        }

        internal override void ClearForPooling()
        {
            base.ClearForPooling();
        }

        internal void SetupForDisplay(ViewGroup? viewData)
        {
            base.SetupForDisplay(viewData);

            if (viewData != null)
            {
                _topMargin = viewData.RelativeOffset.Y;
            }
        }

        internal WidgetRelationship Relationships => _relationships;
    }
}
