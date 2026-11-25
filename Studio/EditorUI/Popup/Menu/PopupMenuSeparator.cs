using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Styling;
using EditorUI.Visual;
using Primary.Common;
using Primary.Mathematics;

namespace EditorUI.Popup.Menu
{
    public sealed class PopupMenuSeparator : PopupMenuItem
    {
        private readonly PopupMenu _owningMenu;
        private readonly PopupMenuMenu? _owningItem;

        private float _height;

        private UIColor _foregroundColor;

        internal PopupMenuSeparator(PopupMenu owningMenu, PopupMenuMenu? owningItem)
        {
            _owningMenu = owningMenu;
            _owningItem = owningItem;

            _height = 2.0f;

            _foregroundColor = Color.Black;
        }

        protected internal override void MeasureSelf(float maxAvailableWidth)
        {
            _itemWidth = 16.0f;
            _itemHeight = _height;
        }

        protected internal override void PaintSelf(ref readonly PainterContext painter, Vector2 originPosition, Vector4 safePadding, float availableWidth)
        {
            painter.AddRectangle(new Boundaries(originPosition, originPosition + new Vector2(availableWidth, _itemHeight)), new Paint(_foregroundColor));
        }

        public override bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            switch (inputEvent.EventType)
            {
                case UIInputEventType.MouseEnter:
                    {
                        _owningMenu.BeginItemHover(this);
                        return true;
                    }
                case UIInputEventType.MouseLeave:
                    {
                        _owningMenu.EndItemHover(this);
                        return true;
                    }
            }

            return false;
        }

        protected internal override void ClearSavedData()
        {
        }

        #region StyledObject
        public override void AddStateFlags(StateFlags flags)
        {
            _stateFlags |= StateFlags.SelfInvalidStyle;

            if (_owningItem != null)
                _owningItem.AddStateFlags(flags & ~StateFlags.This);
            else
                _owningMenu.AddStateFlags(flags & ~StateFlags.This);
        }

        public override void RemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        protected internal override void GetUnstyledObjects(ref StyleQueueContext queue)
        {
        }

        protected internal override StyledObject? ParentObject => (StyledObject?)_owningItem ?? _owningMenu;
        #endregion

        #region Styled
       public float Height { get => _height; set => SetStyledField(value); }

       public UIColor ForegroundColor { get => _foregroundColor; set => SetStyledField(value); }
        #endregion

        public override PopupMenu OwningMenu => _owningMenu;
        public override PopupMenuMenu? OwningItem => _owningItem;
    }
}
