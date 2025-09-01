using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Gui.Elements
{
    public sealed class DockableElement : Element
    {
        private DockingContainer? _container;

        public DockableElement()
        {
            _container = null;
        }

        protected override bool RecalculateLayout()
        {
            _position = new Vector2(0.0f, 18.0f);
            _size = (_container?.ElementSpace.Size ?? new Vector2(0.0f, 18.0f)) - new Vector2(0.0f, 18.0f);

            return true;
        }

        protected override void SetParent(Element newParent)
        {
            return;
        }

        public DockingContainer? Container { get => _container; internal set => _container = value; }

        internal Vector2 Size { get => _size; set { if (_size != value) InvalidateLayout(); _size = value; } }
    }
}
