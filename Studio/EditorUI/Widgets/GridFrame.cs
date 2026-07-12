using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Layout;
using Primary.Common;

namespace EditorUI.Widgets
{
    [UIWidgetAttribute]
    public class GridFrame : Widget
    {
        protected LayoutDirection _layoutDirection;

        protected LayoutAlignment _verticalAlignment;
        protected LayoutAlignment _horizontalAlignment;

        protected LayoutBalance _verticalBalance;
        protected LayoutBalance _horizontalBalance;

        protected Vector2 _itemPadding;

        protected int _maxRowsOrColumns;

        public GridFrame()
        {
            _layoutDirection = LayoutDirection.Horizontal;

            _verticalAlignment = LayoutAlignment.Start;
            _horizontalAlignment = LayoutAlignment.Start;

            _verticalBalance = LayoutBalance.None;
            _horizontalBalance = LayoutBalance.None;

            _itemPadding = Vector2.Zero;

            _maxRowsOrColumns = int.MaxValue;
        }

        protected internal override MeasureReturnData MeasureSelf(ref readonly LayoutContext context)
        {
            base.MeasureSelf(in context);

            LayoutLockAxis lockAxis = LayoutLockAxis.None;
            if (_children != null && _children.Count > 0)
            {
                switch (_layoutDirection)
                {
                    case LayoutDirection.Vertical:
                        {


                            break;
                        }
                    case LayoutDirection.Horizontal:
                        {
                            if (_horizontalBalance == LayoutBalance.Fit)
                            {
                                int maxColumnsPerRow = Math.Min(_maxRowsOrColumns, _children.Count);

                                float itemPadding = _itemPadding.X == 0.0f ? 0.0f : _itemPadding.X * 0.5f * (maxColumnsPerRow - 1);
                                float childSize = (_idealSize.X - _padding.X - _padding.Z) / maxColumnsPerRow - itemPadding;

                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    child.IdealSize = new Vector2(childSize, 0.0f);
                                }

                                lockAxis = LayoutLockAxis.AxisX;
                            }

                            break;
                        }
                }
            }

            return new MeasureReturnData(MeasureStatus.Success, lockAxis);
        }

        protected internal override LayoutReturnData LayoutSelf(ref readonly LayoutContext context)
        {
            base.LayoutSelf(in context);
            return new LayoutReturnData(LayoutLockAxis.AxisXY, true);
        }

        protected internal override void PostLayoutSelf(ref readonly LayoutContext context)
        {
            if (_children != null && _children.Count > 0)
            {
                switch (_layoutDirection)
                {
                    case LayoutDirection.Vertical:
                        {
                            break;
                        }
                    case LayoutDirection.Horizontal:
                        {
                            if (_horizontalBalance == LayoutBalance.Fit)
                            {
                                int maxColumnsPerRow = Math.Min(_maxRowsOrColumns, _children.Count);
                                int totalRowCount = (_children.Count + maxColumnsPerRow - 1) / maxColumnsPerRow;

                                float itemPadding = _itemPadding.X == 0.0f ? 0.0f : _itemPadding.X * 0.5f * maxColumnsPerRow;
                                float columnWidth = (_idealSize.X - _padding.X - _padding.Z + itemPadding) / maxColumnsPerRow;

                                float rowOffset = 0.0f;

                                for (int i = 0; i < totalRowCount; ++i)
                                {
                                    int startIndex = i * maxColumnsPerRow;
                                    int endIndex = Math.Min(startIndex + maxColumnsPerRow, _children.Count);

                                    float maxWidgetHeight = 0.0f;
    
                                    for (int j = startIndex; j < endIndex; ++j)
                                    {
                                        Widget widget = _children[j];
                                        widget.IdealPosition = new Vector2((j - startIndex) * columnWidth, rowOffset);

                                        maxWidgetHeight = Math.Max(maxWidgetHeight, widget.IdealSize.Y);
                                    }

                                    rowOffset += maxWidgetHeight + _itemPadding.Y;
                                }
                            }
                            else
                            {
                                Vector2 startPosition = Vector2.Zero;
                                float maxHeightInColumn = 0.0f;

                                int currentColumnCount = 0;
                                for (int i = 0; i < _children.Count; ++i)
                                {
                                    Widget child = _children[i];
                                    float positionAfter = startPosition.X + child.IdealSize.X;

                                    // if there is not space for one just place it and don't try to move it
                                    if ((positionAfter > _idealSize.X && currentColumnCount > 0) || ++currentColumnCount > _maxRowsOrColumns)
                                    {
                                        startPosition.X = 0.0f;
                                        startPosition.Y += maxHeightInColumn + _itemPadding.Y;

                                        maxHeightInColumn = 0.0f;
                                        positionAfter = 0.0f;

                                        currentColumnCount = 1;
                                    }

                                    child.IdealPosition = startPosition;

                                    maxHeightInColumn = Math.Max(maxHeightInColumn, child.IdealSize.Y);
                                    startPosition.X = positionAfter + _itemPadding.X;
                                }
                            }

                            break;
                        }
                }
            }
        }

        public override void AddStateFlags(StateFlags flags)
        {
            if (flags.HasFlags(StateFlags.InvalidLayout) && _children != null)
            {
                for (int i = 0; i < _children.Count; ++i)
                {
                    if (_children[i].StateFlags.HasFlags(StateFlags.ThisLayout))
                    {
                        flags |= StateFlags.ThisLayout;
                        break;
                    }
                }
            }

            base.AddStateFlags(flags);
        }

        #region Serializable
        [Styled(nameof(_layoutDirection), StateFlags.SelfInvalidLayout)] public LayoutDirection LayoutDirection { get => _layoutDirection; set => SetStyledField(value); }

        [Styled(nameof(_verticalAlignment), StateFlags.SelfInvalidLayout)] public LayoutAlignment VerticalAlignment { get => _verticalAlignment; set => SetStyledField(value); }
        [Styled(nameof(_horizontalAlignment), StateFlags.SelfInvalidLayout)] public LayoutAlignment HorizontalAlignment { get => _horizontalAlignment; set => SetStyledField(value); }

        [Styled(nameof(_verticalBalance), StateFlags.SelfInvalidLayout)] public LayoutBalance VerticalBalance { get => _verticalBalance; set => SetStyledField(value); }
        [Styled(nameof(_horizontalBalance), StateFlags.SelfInvalidLayout)] public LayoutBalance HorizontalBalance { get => _horizontalBalance; set => SetStyledField(value); }

        [Styled(nameof(_itemPadding), StateFlags.SelfInvalidLayout)] public Vector2 ItemPadding { get => _itemPadding; set => SetStyledField(value); }

        [Styled(nameof(_maxRowsOrColumns), StateFlags.SelfInvalidLayout)] public int MaxRowsOrColumns { get => _maxRowsOrColumns; set => SetStyledField(value); }
        #endregion
    }
}
