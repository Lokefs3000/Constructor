using Editor.UI.Assets;
using Editor.UI.Visual;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Menu
{
    public abstract class ContextMenuBase
    {
        protected ContextMenuAsset? _owner;
        protected ContextMenuBase? _parent;

        private float _verticalOffset;

        public ContextMenuBase()
        {
            _owner = null;
            _parent = null;
 
            _verticalOffset = 0.0f;
        }

        internal virtual void ChangeOwner(ContextMenuAsset? newOwner)
        {
            if (_owner != newOwner)
            {
                _owner = newOwner;
            }
        }

        public abstract Vector2 MeasureSize();
        public abstract void DrawVisual(Vector2 basePosition, Vector2 availRegion, UIPainterContext painter);

        public ContextMenuAsset? Owner => _owner;
        public ContextMenuBase? Parent => _parent;

        public float VerticalOffset => _verticalOffset;
    }
}
