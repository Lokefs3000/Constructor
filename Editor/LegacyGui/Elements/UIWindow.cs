using Editor.Rendering.Gui;
using System.Numerics;

namespace Editor.LegacyGui.Elements
{
    public class UIWindow : PhysicalElement
    {
        private DockSpace _currentDockSpace;

        private string _title;

        private bool _hasFocus;

        public UIWindow(Vector2 position, Vector2 size) : base()
        {
            _currentDockSpace = EditorGuiManager.Instance.DockingManager.DockWindowEmpty(this);

            _title = GetType().Name;

            _hasFocus = false;
        }

        protected override void Destroyed()
        {
            _currentDockSpace.RemoveDockedWindow(this);
        }

        private void RecalculateDockSpace()
        {
            return;
#if false
for (int i = 0; i < _children.Count; i++)
            {
                UIWindow window = (UIWindow)_children[i];
                switch (window.Dock)
                {
                    case UIWindowDock.Left:
                        {
                            float width = MathF.Min(window._size.Value.X, _dockSpace.Maximum.X - _dockSpace.Minimum.X - MinimumAvailSpace);
                            if (width < window._size.Value.X)
                                window.Size = new Vector2(width, window.Size.X);
                            window.Position = _dockSpace.Minimum;

                            _dockSpace.Minimum.X += width;
                            break;
                        }
                    case UIWindowDock.Right:
                        {
                            float width = MathF.Min(window._size.Value.X, _dockSpace.Maximum.X - _dockSpace.Minimum.X - MinimumAvailSpace);
                            if (width < window._size.Value.X)
                                window.Size = new Vector2(width, window.Size.X);
                            window.Position = _dockSpace.Maximum - new Vector2(width, 0.0f);

                            _dockSpace.Maximum.X -= width;
                            break;
                        }
                    case UIWindowDock.Top:
                        {
                            float height = MathF.Min(window._size.Value.Y, _dockSpace.Maximum.Y - _dockSpace.Minimum.Y - MinimumAvailSpace);
                            if (height < window._size.Value.Y)
                                window.Size = new Vector2(window.Size.X, height);
                            window.Position = _dockSpace.Minimum;

                            _dockSpace.Minimum.Y += height;
                            break;
                        }
                    case UIWindowDock.Bottom:
                        {
                            float height = MathF.Min(window._size.Value.Y, _dockSpace.Maximum.Y - _dockSpace.Minimum.Y - MinimumAvailSpace);
                            if (height < window._size.Value.Y)
                                window.Size = new Vector2(window.Size.X, height);
                            window.Position = _dockSpace.Maximum - new Vector2(0.0f, height);

                            _dockSpace.Maximum.Y -= height;
                            break;
                        }
                    case UIWindowDock.Tabbed: //guranteed last
                        {
                            window.Size = _dockSpace.Size;
                            window.Position = _dockSpace.Minimum;
                            break;
                        }
                }
            }
#endif
        }

        public override bool RecalculateLayout()
        {
            RecalculateDockSpace();
            return base.RecalculateLayout();
        }

        public override void DrawVisual(GuiCommandBuffer commandBuffer)
        {
            commandBuffer.DrawSolidRect(Vector2.Zero, _size.Value, EdGuiColors.Background);
        }

        private void RemoveDockedWindow(UIWindow child)
        {
            child._children.Remove(this);
            InvalidateLayout();
        }

        internal void TakeFocus()
        {
            _hasFocus = true;

            _currentDockSpace.Window?.TakeFocus();
        }

        internal void ReleaseFocus()
        {
            _hasFocus = false;
        }

        public new UIWindow? Parent => (UIWindow?)base.Parent;
        internal DockSpace CurrentDockSpace => _currentDockSpace;

        public string Title
        {
            get => _title;
            set
            {
                if (_title == value)
                    return;

                _title = value;
                CurrentDockSpace.InvalidateLayout();
            }
        }

        public bool IsFocused => _hasFocus;

        public const float MinimumAvailSpace = 5.0f;
    }

    public enum UIWindowDock : byte
    {
        Floating,
        Left,
        Right,
        Top,
        Bottom,
        Tabbed
    }
}
