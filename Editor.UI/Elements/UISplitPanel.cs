using Editor.UI.Layout;
using Editor.UI.Visual;
using Editor.UI.Serialization.Custom;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("SplitPanel")]
    [CustomSerilizationRoutine(typeof(SplitPanelRoutine))]
    public class UISplitPanel : UIFrame
    {
        private UISplitPanel? _owningSplit;
        private List<UISplitPanel> _ownedSplits;

        private UISplitDirection _direction;
        private new float _position;

        public UISplitPanel(UISplitDirection direction)
        {
            _owningSplit = null;
            _ownedSplits = new List<UISplitPanel>();

            _direction = direction;
            _position = 0.2f;

            _backgroundColor = Color.TransparentBlack;
        }

        internal UISplitPanel() : this(UISplitDirection.Horizontal)
        {

        }

        internal void AddSplit(UISplitPanel panel) => _ownedSplits.Add(panel);
        internal void RemoveSplit(UISplitPanel panel) => _ownedSplits.Remove(panel);

        internal void SetSplitOwner(UISplitPanel? panel)
        {
            if (_owningSplit == panel)
                return;

            _owningSplit?.RemoveSplit(this);
            panel?.AddSplit(this);

            _owningSplit = panel;
        }

        public override void MeasureSize(UIMeasureContext context)
        {
            
        }

        public override void RecalculateLayout(UILayoutContext context)
        {
            
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            if (_ownedSplits.Count == 0)
                return base.DrawVisual(painter);
            else
                return true;
        }

        public UISplitPanel? SplitOwner { get => _owningSplit; }
        public IReadOnlyList<UISplitPanel> OwnedSplits => _ownedSplits;

        [EditableProperty(nameof(_direction), UIStateFlags.InvalidLayout)] public UISplitDirection Direction { get => _direction; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_position), UIStateFlags.InvalidLayout)] public new float Position { get => _position; set => SetEditableProperty(value); }
    }

    public enum UISplitDirection : byte
    {
        Horizontal = 0,
        Vertical
    }
}
