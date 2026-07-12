using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Binding;
using EditorUI.Input;
using EditorUI.Layout;
using EditorUI.Styling;
using EditorUI.Visual;
using EditorUI.Visual.Built;
using Primary.Common;
using Primary.Mathematics;

namespace EditorUI.Widgets
{
    [UIWidget]
    public class GridView : ScrollView
    {
        protected ICollectionBinding<GridViewItem>? _items;
        protected GridViewItemStyle? _itemStyle;

        protected Int2 _itemSize;

        public GridView()
        {
            _items = null;
            _itemStyle = null;

            _itemSize = new Int2(32);
        }

        protected internal override LayoutReturnData LayoutSelf(ref readonly LayoutContext context)
        {
            if (_items != null)
            {
                Vector2 itemSizeVector = _itemSize.AsVector2();

                int startIndex = 0;
                int endIndex = _items.Count - startIndex;

                float activeRows = MathF.Ceiling(endIndex / MathF.Floor(_idealSize.X / itemSizeVector.X));
                _viewSize = new Vector2(_idealSize.X, activeRows * itemSizeVector.Y);
            }
            else
            {
                _viewSize = Vector2.Zero;
            }

            return base.LayoutSelf(in context);
        }

        protected internal override void PaintSelf(ref PainterContext painter)
        {
            base.PaintSelf(ref painter);

            if (_items != null)
            {
                Vector2 itemSizeVector = _itemSize.AsVector2();

                Int2 maxRowsAndColumns = Int2.Max(new Vector2(_insetIdealSize.X / itemSizeVector.X, _insetIdealSize.Y / itemSizeVector.Y + 0.5f).AsInt2(), Int2.One);
                int maxItemsVisibleAtOnce = maxRowsAndColumns.X * maxRowsAndColumns.Y;

                int startIndex = 0;
                int endIndex = Math.Min(_items.Count, maxItemsVisibleAtOnce);

                painter.PushTranslate(_computedRect.Minimum);

                for (int i = startIndex; i < endIndex; ++i)
                {
                    int x = i % maxRowsAndColumns.X;
                    int y = i / maxRowsAndColumns.X;

                    painter.PushTranslate(new Vector2(x, y) * itemSizeVector);

                    GridViewItem item = _items.GetItemAt(i);
                    item.OnPaint(this, in painter, itemSizeVector);

                    painter.PopTranslate();
                }

                painter.PopTranslate();
            }
        }

        public override IInteractable GetInteractable(Vector2 point)
        {
            IInteractable thisInteractable = base.GetInteractable(point);
            if (thisInteractable != this)
                return thisInteractable;

            if (_items != null)
            {
                Vector2 itemSizeVector = _itemSize.AsVector2();

                Int2 maxRowsAndColumns = Int2.Max(new Vector2(_insetIdealSize.X / itemSizeVector.X, _insetIdealSize.Y / itemSizeVector.Y + 0.5f).AsInt2(), Int2.One);
                int maxItemsVisibleAtOnce = maxRowsAndColumns.X * maxRowsAndColumns.Y;

                point -= _computedRect.Minimum;

                int positionX = (int)(point.X / itemSizeVector.X);
                int positionY = (int)(point.Y / itemSizeVector.Y);

                int startIndex = 0;
                int index = positionX + positionY * maxRowsAndColumns.X + startIndex;

                if ((uint)index < _items.Count)
                {
                    return _items.GetItemAt(index);
                }
            }

            return thisInteractable;
        }

        protected internal override void GetUnstyledObjects(ref StyleQueueContext queue)
        {
            if (_itemStyle != null)
                queue.TryEnqueue(_itemStyle);

            base.GetUnstyledObjects(ref queue);
        }

        public ICollectionBinding<GridViewItem>? Items { get => _items; set { _items = value; AddStateFlags(StateFlags.SelfInvalidLayout); } }
        public GridViewItemStyle? ItemStyle
        {
            get => _itemStyle;
            set
            {
                _itemStyle = value;
                if (value != null)
                {
                    value.AddStateFlags(StateFlags.SelfInvalidStyle);
                    AddStateFlags(value.StateFlags & ~StateFlags.This);
                }
            }
        }

        #region Serializable
        [Styled(nameof(_itemSize), StateFlags.SelfInvalidLayout)] public Int2 ItemSize { get => _itemSize; set => _itemSize = value; }
        #endregion
    }

    public abstract class GridViewItem : IInteractable
    {
        public IInteractable GetInteractable(Vector2 point) => this;

        public abstract void OnPaint(GridView gridView, ref readonly PainterContext painter, Vector2 itemSize);
        public abstract void HandleEventSelf(ref readonly UIInputEvent inputEvent);

        public IInteractionShape? Shape => null;
        public WidgetInputState InputState => WidgetInputState.Sink;
    }

    public abstract class GridViewItemStyle : StyledObject
    {
        protected readonly GridView _gridView;
        protected StateFlags _stateFlags;

        public GridViewItemStyle(GridView gridView)
        {
            _gridView = gridView;
            _stateFlags = StateFlags.SelfInvalidStyle;
        }

        public override void AddStateFlags(StateFlags flags)
        {
            _stateFlags |= flags;
            _gridView.AddStateFlags(flags & ~StateFlags.This);
        }

        public override void RemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        protected internal override void GetUnstyledObjects(ref StyleQueueContext context)
        {
        }

        protected internal override StyledObject? ParentObject => _gridView;

        public override StateFlags StateFlags => _stateFlags;
    }
}
