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
        protected ContextMenuHost? _host;
        protected ContextMenuBase? _parent;

        protected string? _id;

        private Vector2 _totalSize;
        private float _verticalOffset;

        public ContextMenuBase()
        {
            _host = null;
            _parent = null;

            _id = null;

            _totalSize = Vector2.Zero;
            _verticalOffset = 0.0f;
        }

        public virtual void SetOwner(ContextMenuHost? newHost)
        {
            if (_host == newHost)
                return;

            // can't set the host owner because we are not the tree root
            if (_parent != null)
                return;

            _host = newHost;
        }

        public virtual void SetParent(ContextMenuBase? newParent)
        {
            if (_parent == newParent)
                return;

            _parent?.RemoveChild(this);

            if (newParent == this)
                newParent = null;

            if (newParent != null && !newParent.AddChild(this))
            {
                _parent = null;
                SetOwner(null);
                return;
            }

            _parent = newParent;
            SetOwner(newParent?._host);
        }

        protected virtual void RemoveChild(ContextMenuBase item)
        {
        }

        protected virtual bool AddChild(ContextMenuBase item) => false;

        public abstract Vector2 MeasureSize();
        public abstract void DrawVisual(Vector2 basePosition, Vector2 availRegion, UIPainterContext painter);

        public ContextMenuHost? Host => _host;
        public ContextMenuBase? Parent { get => _parent; set => SetParent(value); }

        public string? Id { get => _id; set => _id = value; }

        internal Vector2 TotalSize { get => _totalSize; set => _totalSize = value; }
        internal float VerticalOffset { get => _verticalOffset; set => _verticalOffset = value; }
    }
}
