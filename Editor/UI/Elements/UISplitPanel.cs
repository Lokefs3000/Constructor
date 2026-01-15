using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Elements
{
    public class UISplitPanel : UIElement
    {
        private UISplitPanel? _owningSplit;
        private List<UISplitPanel> _ownedSplits;

        private UISplitDirection _direction;
        private float _position;

        public UISplitPanel(UISplitDirection direction)
        {
            _owningSplit = null;
            _ownedSplits = new List<UISplitPanel>();

            _direction = direction;
            _position = 0.2f;
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

        public UISplitPanel? SplitOwner { get => _owningSplit; }
        public IReadOnlyList<UISplitPanel> OwnedSplits => _ownedSplits;

        public UISplitDirection Direction { get => _direction; set => _direction = value; }
        public float Position { get => _position; set => _position = value; }
    }

    public enum UISplitDirection : byte
    {
        Horizontal = 0,
        Vertical
    }
}
